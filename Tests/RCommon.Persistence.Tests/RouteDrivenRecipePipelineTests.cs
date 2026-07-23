using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Holistic proof of the recipe-1 wiring end-to-end (AC-2, AC-12).
/// Models an Order aggregate that raises TWO events on a single command:
///   - <see cref="OrderPlaced"/>  (transient) — has an AddSubscriber handler, NOT durable.
///   - <see cref="OrderConfirmed"/> (durable) — Publish&lt;OrderConfirmed&gt;().UseOutbox("Orders").
/// Drives <see cref="OutboxEntityEventTracker"/> directly (tracker-level, not via UoW+TransactionScope,
/// matching the RouteDrivenOutboxPipelineTests pattern) and exercises the full dispatch-persist-relay
/// pipeline including rollback-on-handler-throw semantics.
/// </summary>
public class RouteDrivenRecipePipelineTests
{
    // --------------------------------------------------------------------------
    // Domain events
    // --------------------------------------------------------------------------

    /// <summary>Domain / transient event — handled in-process pre-commit.</summary>
    public record OrderPlaced(Guid OrderId, string CustomerName) : ISerializableEvent;

    /// <summary>Integration / durable event — buffered to outbox, relayed post-commit.</summary>
    public record OrderConfirmed(Guid OrderId) : ISerializableEvent;

    // --------------------------------------------------------------------------
    // Aggregate
    // --------------------------------------------------------------------------

    /// <summary>
    /// Minimal aggregate (derives from AggregateRoot&lt;Guid&gt; to match the task spec).
    /// Uses <c>AddLocalEvent</c> (from <see cref="BusinessEntity"/>) rather than
    /// <c>AddDomainEvent</c> because the test events implement <see cref="ISerializableEvent"/>
    /// (not <see cref="IDomainEvent"/>) — sufficient for the event-tracking pipeline under test.
    /// </summary>
    private sealed class Order : AggregateRoot<Guid>
    {
        public Order(Guid id) : base(id) { }

        /// <summary>Raises OrderPlaced (transient) + OrderConfirmed (durable) in that order.</summary>
        public void PlaceOrder(string customerName)
        {
            AddLocalEvent(new OrderPlaced(Id, customerName));
            AddLocalEvent(new OrderConfirmed(Id));
        }
    }

    // --------------------------------------------------------------------------
    // Test subscribers
    // --------------------------------------------------------------------------

    /// <summary>Records every OrderPlaced event handled in-process.</summary>
    private sealed class OrderPlacedRecordingHandler : ISubscriber<OrderPlaced>
    {
        private readonly List<OrderPlaced> _received;

        public OrderPlacedRecordingHandler(List<OrderPlaced> received) => _received = received;

        public Task HandleAsync(OrderPlaced @event, CancellationToken cancellationToken = default)
        {
            _received.Add(@event);
            return Task.CompletedTask;
        }
    }

    /// <summary>OrderPlaced handler that always throws (for rollback-on-throw test, test d).</summary>
    private sealed class ThrowingOrderPlacedHandler : ISubscriber<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced @event, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("order-placed-handler-boom");
    }

    /// <summary>Records every OrderConfirmed event handled during post-commit relay.</summary>
    private sealed class OrderConfirmedRecordingHandler : ISubscriber<OrderConfirmed>
    {
        private readonly List<OrderConfirmed> _received;

        public OrderConfirmedRecordingHandler(List<OrderConfirmed> received) => _received = received;

        public Task HandleAsync(OrderConfirmed @event, CancellationToken cancellationToken = default)
        {
            _received.Add(@event);
            return Task.CompletedTask;
        }
    }

    // --------------------------------------------------------------------------
    // Infrastructure helpers (mirrors RouteDrivenOutboxPipelineTests)
    // --------------------------------------------------------------------------

    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly List<(IOutboxMessage Message, string DataStore)> _saved = new();
    private readonly Mock<IGuidGenerator> _guidGenMock = new();

    public RouteDrivenRecipePipelineTests()
    {
        _guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid);
        _storeMock
            .Setup(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<IOutboxMessage, string, CancellationToken>((m, name, _) => _saved.Add((m, name)))
            .Returns(Task.CompletedTask);
    }

    private OutboxEventRouter CreateOutboxRouter(IServiceProvider serviceProvider, EventSubscriptionManager subscriptionManager)
        => new OutboxEventRouter(
            _storeMock.Object,
            new JsonOutboxSerializer(),
            _guidGenMock.Object,
            new Mock<ITenantIdAccessor>().Object,
            serviceProvider,
            subscriptionManager,
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()),
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "Orders" }));

    private InMemoryTransactionalEventRouter CreateInProcessRouter(IServiceProvider serviceProvider, EventSubscriptionManager subscriptionManager)
        => new InMemoryTransactionalEventRouter(
            serviceProvider,
            NullLogger<InMemoryTransactionalEventRouter>.Instance,
            subscriptionManager,
            Options.Create(new EventHandlingOptions()));

    /// <summary>
    /// Builds a real provider using WithEventHandling&lt;InMemoryEventBusBuilder&gt;, wiring:
    ///  - AddSubscriber&lt;OrderPlaced, OrderPlacedRecordingHandler&gt;
    ///  - AddSubscriber&lt;OrderConfirmed, OrderConfirmedRecordingHandler&gt;
    ///  - Publish&lt;OrderConfirmed&gt;().UseOutbox("Orders")
    /// The recording lists are registered as singletons so DI can inject them into the scoped
    /// handler instances resolved by InMemoryEventBus.PublishAsync at dispatch time.
    /// Returns the provider, the shared recording lists, and the routing registry.
    /// </summary>
    private static (ServiceProvider Provider, List<OrderPlaced> PlacedReceived, List<OrderConfirmed> ConfirmedReceived, EventRoutingRegistry Registry, EventSubscriptionManager SubscriptionManager)
        BuildRecipeProvider()
    {
        var placedReceived = new List<OrderPlaced>();
        var confirmedReceived = new List<OrderConfirmed>();

        var services = new ServiceCollection();
        services.AddLogging();

        // Register the recording lists as singletons so DI can inject them into the scoped handlers.
        // InMemoryEventBus.PublishAsync creates a new scope and resolves ISubscriber<T> — which in turn
        // asks DI to inject the ctor arg List<TEvent> — finding the singleton instance that the test
        // body holds a reference to, making captured results observable after dispatch.
        services.AddSingleton(placedReceived);
        services.AddSingleton(confirmedReceived);

        var rcommonBuilder = new RCommonBuilder(services);
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(events =>
        {
            // Standard AddSubscriber wiring: registers ISubscriber<T> as scoped, adds the
            // PublishWithEventBusEventProducer, and records the event->builder subscription.
            events.AddSubscriber<OrderPlaced, OrderPlacedRecordingHandler>();
            events.AddSubscriber<OrderConfirmed, OrderConfirmedRecordingHandler>();
            // Mark OrderConfirmed as durable to "Orders" outbox — recipe-1 wiring under test.
            events.Publish<OrderConfirmed>().UseOutbox("Orders");
        });

        var provider = services.BuildServiceProvider();

        // Extract the singleton registry and subscription manager that were wired during config
        var registry = (EventRoutingRegistry)provider.GetRequiredService<IEventRoutingRegistry>();
        var subscriptionManager = provider.GetRequiredService<EventSubscriptionManager>();

        return (provider, placedReceived, confirmedReceived, registry, subscriptionManager);
    }

    // --------------------------------------------------------------------------
    // (a) After DispatchDomainEventsAsync: OrderPlaced handler fired; no outbox row for OrderPlaced
    // --------------------------------------------------------------------------

    [Fact]
    public async Task After_dispatch_OrderPlaced_handler_fired_and_no_row_persisted_for_OrderPlaced()
    {
        // Arrange
        var (provider, placedReceived, _, registry, subscriptionManager) = BuildRecipeProvider();
        using (provider)
        {
            var outboxRouter = CreateOutboxRouter(provider, subscriptionManager);
            var inProcessRouter = CreateInProcessRouter(provider, subscriptionManager);
            var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
            var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, registry);

            var orderId = Guid.NewGuid();
            var order = new Order(orderId);
            order.PlaceOrder("Alice");
            tracker.AddEntity(order, "Orders");

            // Act: pre-commit dispatch only
            await tracker.DispatchDomainEventsAsync();

            // Assert (a): the transient handler for OrderPlaced fired
            placedReceived.Should().ContainSingle(
                "the OrderPlaced transient handler must fire pre-commit during DispatchDomainEventsAsync");
            placedReceived[0].OrderId.Should().Be(orderId);
            placedReceived[0].CustomerName.Should().Be("Alice");

            // Assert (a): no outbox row was persisted yet (PersistEventsAsync not called)
            _saved.Should().BeEmpty(
                "no rows should be persisted after DispatchDomainEventsAsync alone — PersistEventsAsync flushes the buffer");
        }
    }

    // --------------------------------------------------------------------------
    // (b) After PersistEventsAsync: exactly ONE row, for OrderConfirmed, in "Orders"
    // --------------------------------------------------------------------------

    [Fact]
    public async Task After_persist_exactly_one_outbox_row_exists_for_OrderConfirmed_in_Orders()
    {
        // Arrange
        var (provider, _, _, registry, subscriptionManager) = BuildRecipeProvider();
        using (provider)
        {
            var outboxRouter = CreateOutboxRouter(provider, subscriptionManager);
            var inProcessRouter = CreateInProcessRouter(provider, subscriptionManager);
            var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
            var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, registry);

            var order = new Order(Guid.NewGuid());
            order.PlaceOrder("Bob");
            tracker.AddEntity(order, "Orders");

            // Act: dispatch (pre-commit) then persist
            await tracker.DispatchDomainEventsAsync();
            await tracker.PersistEventsAsync();

            // Assert (b): exactly one row in the outbox
            _saved.Should().HaveCount(1,
                "only the durable OrderConfirmed event must be persisted to the outbox");

            // Assert (b): the row is for OrderConfirmed, written to "Orders"
            _saved[0].DataStore.Should().Be("Orders",
                "the outbox row must target the 'Orders' datastore matching UseOutbox(\"Orders\")");

            // The saved message must correspond to OrderConfirmed (verify type name is preserved in payload)
            _saved[0].Message.EventType.Should().Contain(nameof(OrderConfirmed),
                "the outbox message type must identify the durable OrderConfirmed event");
        }
    }

    // --------------------------------------------------------------------------
    // (c) OrderConfirmed fires during EmitTransactionalEventsAsync, NOT pre-commit
    // --------------------------------------------------------------------------

    [Fact]
    public async Task OrderConfirmed_subscriber_fires_post_commit_relay_not_pre_commit()
    {
        // Arrange
        var (provider, _, confirmedReceived, registry, subscriptionManager) = BuildRecipeProvider();
        using (provider)
        {
            var outboxRouter = CreateOutboxRouter(provider, subscriptionManager);
            var inProcessRouter = CreateInProcessRouter(provider, subscriptionManager);
            var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
            var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, registry);

            var order = new Order(Guid.NewGuid());
            order.PlaceOrder("Charlie");
            tracker.AddEntity(order, "Orders");

            // Act: pre-commit dispatch
            await tracker.DispatchDomainEventsAsync();

            // Assert (c) — NOT dispatched pre-commit
            confirmedReceived.Should().BeEmpty(
                "the durable OrderConfirmed must NOT be dispatched pre-commit during DispatchDomainEventsAsync");

            // Act: persist (within transaction)
            await tracker.PersistEventsAsync();

            // Post-persist: still not dispatched
            confirmedReceived.Should().BeEmpty(
                "the durable OrderConfirmed must NOT be dispatched during PersistEventsAsync either");

            // Act: post-commit relay
            var relayed = await tracker.EmitTransactionalEventsAsync();

            // Assert (c): relay returns true and OrderConfirmed fires exactly once
            relayed.Should().BeTrue();
            confirmedReceived.Should().ContainSingle(
                "OrderConfirmed must fire exactly once during the post-commit relay");
        }
    }

    // --------------------------------------------------------------------------
    // (d) If OrderPlaced handler throws, DispatchDomainEventsAsync throws and no row is persisted
    // --------------------------------------------------------------------------

    [Fact]
    public async Task When_OrderPlaced_handler_throws_DispatchDomainEventsAsync_throws_and_persist_is_never_reached()
    {
        // Arrange: wire a throwing handler for OrderPlaced (replaces the recording handler)
        var services = new ServiceCollection();
        services.AddLogging();

        var rcommonBuilder = new RCommonBuilder(services);
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(events =>
        {
            // Throwing handler wired for OrderPlaced
            events.AddSubscriber<OrderPlaced, ThrowingOrderPlacedHandler>();
            // OrderConfirmed is still durable — to confirm no row is saved when dispatch throws
            events.Publish<OrderConfirmed>().UseOutbox("Orders");
        });

        using var provider = services.BuildServiceProvider();
        var registry = (EventRoutingRegistry)provider.GetRequiredService<IEventRoutingRegistry>();
        var subscriptionManager = provider.GetRequiredService<EventSubscriptionManager>();

        var outboxRouter = CreateOutboxRouter(provider, subscriptionManager);
        var inProcessRouter = CreateInProcessRouter(provider, subscriptionManager);
        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, registry);

        var order = new Order(Guid.NewGuid());
        order.PlaceOrder("Dave");
        tracker.AddEntity(order, "Orders");

        // Act: dispatch should throw because the OrderPlaced handler throws
        var act = async () => await tracker.DispatchDomainEventsAsync();

        // Assert (d): DispatchDomainEventsAsync throws.
        // OBSERVED REAL BEHAVIOR (per Phase-2 pre-commit holistic test, PreCommitDispatchPipelineTests):
        // the in-memory bus invokes subscribers via reflection, wrapping the handler exception in
        // TargetInvocationException, which the router's catch block further wraps in EventProductionException.
        // What matters for AC-6 / AC-12: dispatch THROWS (so CommitAsync would roll back before persist)
        // and the original handler exception is preserved at the root.
        var thrown = await act.Should().ThrowAsync<EventProductionException>(
            "a throwing pre-commit handler must surface as EventProductionException so CommitAsync rolls back");

        var rootCause = UnwrapToRoot(thrown.Which);
        rootCause.Should().BeOfType<InvalidOperationException>()
            .Which.Message.Should().Be("order-placed-handler-boom",
                "the original handler exception must be preserved at the exception root");

        // Assert (d): PersistEventsAsync was never reached => no outbox rows
        _saved.Should().BeEmpty(
            "when DispatchDomainEventsAsync throws, PersistEventsAsync is never called and no outbox row is written");
    }

    // --------------------------------------------------------------------------
    // Helper
    // --------------------------------------------------------------------------

    private static Exception UnwrapToRoot(Exception ex)
    {
        var current = ex;
        while (current.InnerException is not null)
            current = current.InnerException;
        return current;
    }
}
