using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

public record ConcurrencyTestEvent(string Data) : ISerializableEvent;

public class OutboxConcurrencyTests
{
    [Fact]
    public async Task EmptyBuffer_PersistBufferedEventsAsync_NoStoreCalls()
    {
        var storeMock = new Mock<IOutboxStore>();
        var guidGenMock = new Mock<IGuidGenerator>();
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        var router = new OutboxEventRouter(
            storeMock.Object, new JsonOutboxSerializer(),
            guidGenMock.Object, tenantMock.Object,
            serviceProviderMock.Object, new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));

        await router.PersistBufferedEventsAsync();
        storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RouteEventsAsync_NoRetainedEvents_CompletesQuickly()
    {
        var storeMock = new Mock<IOutboxStore>();
        var guidGenMock = new Mock<IGuidGenerator>();
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        var router = new OutboxEventRouter(
            storeMock.Object, new JsonOutboxSerializer(),
            guidGenMock.Object, tenantMock.Object,
            serviceProviderMock.Object, new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));

        // No events buffered or persisted, so retained list is empty
        await router.RouteEventsAsync();
        storeMock.Verify(s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
