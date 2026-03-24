using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public record UoWTestEvent(string Data) : ISerializableEvent;

public class UnitOfWorkOutboxTests
{
    [Fact]
    public async Task PersistEventsAsync_IsCalledBeforeCommit_ViaOutboxEntityEventTracker()
    {
        var storeMock = new Mock<IOutboxStore>();
        var serializer = new JsonOutboxSerializer();
        var guidGenMock = new Mock<IGuidGenerator>();
        guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid());
        var tenantMock = new Mock<RCommon.Security.Claims.ITenantIdAccessor>();

        var serviceProviderMock = new Mock<IServiceProvider>();
        var subscriptionManager = new EventSubscriptionManager();

        var outboxRouter = new OutboxEventRouter(
            storeMock.Object,
            serializer,
            guidGenMock.Object,
            tenantMock.Object,
            serviceProviderMock.Object,
            subscriptionManager,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OutboxEventRouter>.Instance,
            Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));

        var innerTracker = new InMemoryEntityEventTracker(outboxRouter);
        var tracker = new OutboxEntityEventTracker(innerTracker, outboxRouter);

        // Simulate: PersistEventsAsync is called (Phase 1, pre-commit)
        await tracker.PersistEventsAsync();

        // With no entities tracked, no store calls expected — but should complete without error
        storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
