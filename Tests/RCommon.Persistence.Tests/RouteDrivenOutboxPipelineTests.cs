using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Verifies the Phase-3a route-driven split in <see cref="OutboxEntityEventTracker"/>:
/// transient events are dispatched pre-commit through the in-process FIFO, durable events are
/// buffered to the outbox and persisted pre-commit (never dispatched pre-commit) and relayed post-commit.
/// </summary>
public class RouteDrivenOutboxPipelineTests
{
    // Distinct event types so the routing registry can mark each independently.
    public record TransientPipelineEvent(string Data) : ISerializableEvent;
    public record DurablePipelineEvent(string Data) : ISerializableEvent;
    public record MidDispatchDurableEvent(string Data) : ISerializableEvent;
    public record DualRoutedEvent(string Data) : ISerializableEvent;

    /// <summary>Entity that seeds a single local event at construction.</summary>
    private class PipelineEntity : BusinessEntity<int>
    {
        public PipelineEntity(ISerializableEvent localEvent) => AddLocalEvent(localEvent);
    }

    /// <summary>
    /// A domain-handling producer that, upon receiving a <see cref="TransientPipelineEvent"/>, has the
    /// tracked entity raise a durable integration event mid-dispatch (simulates AC-5 cascade).
    /// </summary>
    private sealed class RaisingProducer : IEventProducer
    {
        private readonly IBusinessEntity _entity;
        private readonly ISerializableEvent _toRaise;

        public RaisingProducer(IBusinessEntity entity, ISerializableEvent toRaise)
        {
            _entity = entity;
            _toRaise = toRaise;
        }

        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            if (@event is TransientPipelineEvent)
            {
                ((BusinessEntity<int>)_entity).AddLocalEvent(_toRaise);
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>Recording producer that captures every event it produces (in-process dispatch observer).</summary>
    private sealed class RecordingProducer : IEventProducer
    {
        public List<ISerializableEvent> Produced { get; } = new();

        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            Produced.Add(@event);
            return Task.CompletedTask;
        }
    }

    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly List<(IOutboxMessage Message, string DataStore)> _saved = new();
    private readonly Mock<IGuidGenerator> _guidGenMock = new();
    private readonly EventRoutingRegistry _routingRegistry = new();

    public RouteDrivenOutboxPipelineTests()
    {
        _guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid);
        _storeMock.Setup(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }));

    private InMemoryTransactionalEventRouter CreateInProcessRouter(IServiceProvider serviceProvider, EventSubscriptionManager subscriptionManager)
        => new InMemoryTransactionalEventRouter(
            serviceProvider,
            NullLogger<InMemoryTransactionalEventRouter>.Instance,
            subscriptionManager,
            Options.Create(new EventHandlingOptions()));

    [Fact]
    public async Task Transient_entity_event_is_dispatched_pre_commit_and_not_persisted()
    {
        var recorder = new RecordingProducer();
        var services = new ServiceCollection();
        services.AddSingleton<IEventProducer>(recorder);
        var sp = services.BuildServiceProvider();
        var subscriptionManager = new EventSubscriptionManager();

        var outboxRouter = CreateOutboxRouter(sp, subscriptionManager);
        var inProcessRouter = CreateInProcessRouter(sp, subscriptionManager);
        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, _routingRegistry);

        // NOT marked durable => transient.
        var evt = new TransientPipelineEvent("t");
        tracker.AddEntity(new PipelineEntity(evt), "Orders");

        await tracker.DispatchDomainEventsAsync();
        await tracker.PersistEventsAsync();

        recorder.Produced.Should().Contain(evt, "a transient event is dispatched in-process pre-commit");
        _saved.Should().BeEmpty("a transient event must not be persisted to the outbox");
    }

    [Fact]
    public async Task Durable_entity_event_is_persisted_and_not_dispatched_pre_commit_then_relayed()
    {
        var recorder = new RecordingProducer();
        var services = new ServiceCollection();
        services.AddSingleton<IEventProducer>(recorder);
        var sp = services.BuildServiceProvider();
        var subscriptionManager = new EventSubscriptionManager();

        var outboxRouter = CreateOutboxRouter(sp, subscriptionManager);
        var inProcessRouter = CreateInProcessRouter(sp, subscriptionManager);
        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, _routingRegistry);

        _routingRegistry.MarkDurable(typeof(DurablePipelineEvent), "Orders");
        var evt = new DurablePipelineEvent("d");
        tracker.AddEntity(new PipelineEntity(evt), "Orders");

        await tracker.DispatchDomainEventsAsync();
        recorder.Produced.Should().NotContain(evt, "a durable event is not dispatched pre-commit");

        await tracker.PersistEventsAsync();
        _saved.Should().HaveCount(1, "the durable event is persisted to the outbox exactly once");
        _saved[0].DataStore.Should().Be("Orders");

        var relayed = await tracker.EmitTransactionalEventsAsync();
        relayed.Should().BeTrue();
        recorder.Produced.Should().Contain(evt, "the durable event is relayed post-commit");
    }

    [Fact]
    public async Task Durable_event_raised_by_a_handler_mid_dispatch_is_persisted_in_the_same_transaction()
    {
        _routingRegistry.MarkDurable(typeof(MidDispatchDurableEvent), "Orders");

        var transientSeed = new TransientPipelineEvent("seed");
        var midEvent = new MidDispatchDurableEvent("mid");
        var entity = new PipelineEntity(transientSeed);

        // Producer raises the durable event on the entity while the transient seed is being dispatched.
        var raising = new RaisingProducer(entity, midEvent);
        var services = new ServiceCollection();
        services.AddSingleton<IEventProducer>(raising);
        var sp = services.BuildServiceProvider();
        var subscriptionManager = new EventSubscriptionManager();

        var outboxRouter = CreateOutboxRouter(sp, subscriptionManager);
        var inProcessRouter = CreateInProcessRouter(sp, subscriptionManager);
        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, _routingRegistry);

        tracker.AddEntity(entity, "Orders");

        await tracker.DispatchDomainEventsAsync();
        await tracker.PersistEventsAsync();

        _saved.Should().HaveCount(1, "the mid-dispatch durable event was buffered and persisted in the same transaction (AC-5)");
        _saved[0].DataStore.Should().Be("Orders");
    }

    [Fact]
    public async Task Event_with_both_durable_route_and_subscriber_is_persisted_once_and_relayed_not_double_delivered()
    {
        var recorder = new RecordingProducer();
        var services = new ServiceCollection();
        services.AddSingleton<IEventProducer>(recorder);
        var sp = services.BuildServiceProvider();
        var subscriptionManager = new EventSubscriptionManager();

        // Register a subscription so the producer is a genuine subscriber for the event.
        subscriptionManager.AddProducerForBuilder(typeof(RouteDrivenOutboxPipelineTests), typeof(RecordingProducer));
        subscriptionManager.AddSubscription(typeof(RouteDrivenOutboxPipelineTests), typeof(DualRoutedEvent));

        var outboxRouter = CreateOutboxRouter(sp, subscriptionManager);
        var inProcessRouter = CreateInProcessRouter(sp, subscriptionManager);
        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, _routingRegistry);

        _routingRegistry.MarkDurable(typeof(DualRoutedEvent), "Orders");
        var evt = new DualRoutedEvent("dual");
        tracker.AddEntity(new PipelineEntity(evt), "Orders");

        await tracker.DispatchDomainEventsAsync();
        recorder.Produced.Should().BeEmpty("the subscriber must NOT fire pre-commit for a durable event");

        await tracker.PersistEventsAsync();
        _saved.Should().HaveCount(1, "exactly one outbox row");

        await tracker.EmitTransactionalEventsAsync();
        recorder.Produced.Should().ContainSingle().Which.Should().Be(evt, "the subscriber fires exactly once during the relay");
    }

    [Fact]
    public async Task Durable_event_store_mismatch_with_entity_datastore_throws_co_location_error()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var subscriptionManager = new EventSubscriptionManager();

        var outboxRouter = CreateOutboxRouter(sp, subscriptionManager);
        var inProcessRouter = CreateInProcessRouter(sp, subscriptionManager);
        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter, inProcessRouter, _routingRegistry);

        // Event routed to "Warehouse" but the aggregate is tracked under "Orders" -> co-location violation.
        _routingRegistry.MarkDurable(typeof(DurablePipelineEvent), "Warehouse");
        tracker.AddEntity(new PipelineEntity(new DurablePipelineEvent("x")), "Orders");

        var act = async () => await tracker.DispatchDomainEventsAsync();

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .Which.Message.Should().ContainAll("Warehouse", "Orders");
    }
}
