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

public record TrackerTestEvent(string Data) : ISerializableEvent;
public record TrackerTestEventA(string Data) : ISerializableEvent;
public record TrackerTestEventB(string Data) : ISerializableEvent;

public class TrackerTestEntity : BusinessEntity<int>
{
    public TrackerTestEntity(ISerializableEvent localEvent)
    {
        AddLocalEvent(localEvent);
    }
}

public class OutboxEntityEventTrackerTests
{
    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IGuidGenerator> _guidGenMock = new();
    private readonly OutboxEventRouter _outboxRouter;
    private readonly InMemoryEntityEventTracker _innerTracker;
    private readonly InMemoryTransactionalEventRouter _inProcessRouter;
    private readonly IEventRoutingRegistry _routingRegistry;

    public OutboxEntityEventTrackerTests()
    {
        _guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid());
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        _outboxRouter = new OutboxEventRouter(
            _storeMock.Object,
            new JsonOutboxSerializer(),
            _guidGenMock.Object,
            tenantMock.Object,
            serviceProviderMock.Object,
            new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()),
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }));

        _innerTracker = new InMemoryEntityEventTracker(_outboxRouter);

        // The in-process router resolves IEnumerable<IEventProducer> from the provider on every drain, so it
        // needs a real (empty) provider rather than a bare mock that throws "no service registered".
        var emptyProducerProvider = new ServiceCollection().BuildServiceProvider();
        _inProcessRouter = new InMemoryTransactionalEventRouter(
            emptyProducerProvider,
            NullLogger<InMemoryTransactionalEventRouter>.Instance,
            new EventSubscriptionManager(),
            Options.Create(new EventHandlingOptions()));
        _routingRegistry = new EventRoutingRegistry();
    }

    [Fact]
    public void AddEntity_DelegatesToInnerTracker()
    {
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter, _inProcessRouter, _routingRegistry);
        var entityMock = new Mock<IBusinessEntity>();
        entityMock.Setup(e => e.AllowEventTracking).Returns(true);

        tracker.AddEntity(entityMock.Object);

        tracker.TrackedEntities.Should().Contain(entityMock.Object);
    }

    [Fact]
    public async Task PersistEventsAsync_WithNoEntities_CompletesWithoutStoreCalls()
    {
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter, _inProcessRouter, _routingRegistry);

        await tracker.PersistEventsAsync();

        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_ReturnsTrue()
    {
        // The router no longer reads from the store in RouteEventsAsync — it dispatches from
        // the in-memory retained list. Since no events were buffered, the retained list is empty
        // and RouteEventsAsync returns immediately without any store calls.
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter, _inProcessRouter, _routingRegistry);

        var result = await tracker.EmitTransactionalEventsAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchDomainEventsAsync_DoesNotTouchTheStore_DurableEventsAreOnlyBuffered()
    {
        // Route-driven contract (Phase 3a): DispatchDomainEventsAsync partitions events but must NOT write to
        // the outbox store — durable events are only BUFFERED during dispatch and are flushed later by
        // PersistEventsAsync. Use a strict store mock to assert zero store interaction during dispatch. A
        // durable event is used so the transient in-process FIFO drain is not exercised (no producers wired).
        var strictStore = new Mock<IOutboxStore>(MockBehavior.Strict);
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var strictRouter = new OutboxEventRouter(
            strictStore.Object,
            new JsonOutboxSerializer(),
            _guidGenMock.Object,
            tenantMock.Object,
            serviceProviderMock.Object,
            new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()),
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }));
        var innerTracker = new InMemoryEntityEventTracker(strictRouter);
        _routingRegistry.MarkDurable(typeof(TrackerTestEventA), "A");
        var tracker = new OutboxEntityEventTracker(innerTracker, strictRouter, _inProcessRouter, _routingRegistry);
        tracker.AddEntity(new TrackerTestEntity(new TrackerTestEventA("a")), "A");

        await tracker.DispatchDomainEventsAsync();

        strictStore.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PersistEventsAsync_PersistsEachDurableEntityEventToItsOwnDataStore()
    {
        // Route-driven contract (Phase 3a): only DURABLE events are persisted. Each event type is marked
        // durable to the datastore its aggregate is tracked under (co-location). Durable events are buffered
        // during DispatchDomainEventsAsync and flushed in PersistEventsAsync.
        _routingRegistry.MarkDurable(typeof(TrackerTestEventA), "A");
        _routingRegistry.MarkDurable(typeof(TrackerTestEventB), "B");

        var perStore = new Dictionary<string, int>();
        _storeMock.Setup(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<IOutboxMessage, string, CancellationToken>((_, name, _) =>
            {
                perStore.TryGetValue(name, out var count);
                perStore[name] = count + 1;
            });

        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter, _inProcessRouter, _routingRegistry);
        tracker.AddEntity(new TrackerTestEntity(new TrackerTestEventA("a")), "A");
        tracker.AddEntity(new TrackerTestEntity(new TrackerTestEventB("b")), "B");

        await tracker.DispatchDomainEventsAsync();
        await tracker.PersistEventsAsync();

        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), "A", It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), "B", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersistEventsAsync_DoesNotPersistTransientEntityEvents()
    {
        // A plain entity event with NO durable route is transient under the route-driven model and must not
        // be written to the outbox store. It is dispatched in-process during DispatchDomainEventsAsync instead.
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter, _inProcessRouter, _routingRegistry);
        tracker.AddEntity(new TrackerTestEntity(new TrackerTestEvent("a")), "A");

        await tracker.DispatchDomainEventsAsync();
        await tracker.PersistEventsAsync();

        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
