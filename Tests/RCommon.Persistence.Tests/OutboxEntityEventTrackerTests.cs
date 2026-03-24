using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

public record TrackerTestEvent(string Data) : ISerializableEvent;

public class OutboxEntityEventTrackerTests
{
    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IGuidGenerator> _guidGenMock = new();
    private readonly OutboxEventRouter _outboxRouter;
    private readonly InMemoryEntityEventTracker _innerTracker;

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
            Options.Create(new OutboxOptions()));

        _innerTracker = new InMemoryEntityEventTracker(_outboxRouter);
    }

    [Fact]
    public void AddEntity_DelegatesToInnerTracker()
    {
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter);
        var entityMock = new Mock<IBusinessEntity>();
        entityMock.Setup(e => e.AllowEventTracking).Returns(true);

        tracker.AddEntity(entityMock.Object);

        tracker.TrackedEntities.Should().Contain(entityMock.Object);
    }

    [Fact]
    public async Task PersistEventsAsync_WithNoEntities_CompletesWithoutStoreCalls()
    {
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter);

        await tracker.PersistEventsAsync();

        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_ReturnsTrue()
    {
        // The router no longer reads from the store in RouteEventsAsync — it dispatches from
        // the in-memory retained list. Since no events were buffered, the retained list is empty
        // and RouteEventsAsync returns immediately without any store calls.
        var tracker = new OutboxEntityEventTracker(_innerTracker, _outboxRouter);

        var result = await tracker.EmitTransactionalEventsAsync();

        result.Should().BeTrue();
    }
}
