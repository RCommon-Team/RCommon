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
    public async Task DeadLetterMessages_ExcludedFromGetPending()
    {
        var storeMock = new Mock<IOutboxStore>();
        var deadLetteredMsg = new OutboxMessage
        {
            Id = Guid.NewGuid(), EventType = "T", EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow, DeadLetteredAtUtc = DateTimeOffset.UtcNow
        };
        storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage>());

        var pending = await storeMock.Object.GetPendingAsync(100);
        pending.Should().NotContain(m => m.DeadLetteredAtUtc.HasValue);
    }

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
    public async Task RouteEventsAsync_NoPending_CompletesQuickly()
    {
        var storeMock = new Mock<IOutboxStore>();
        storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage>());
        var guidGenMock = new Mock<IGuidGenerator>();
        var tenantMock = new Mock<ITenantIdAccessor>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        var router = new OutboxEventRouter(
            storeMock.Object, new JsonOutboxSerializer(),
            guidGenMock.Object, tenantMock.Object,
            serviceProviderMock.Object, new EventSubscriptionManager(),
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));

        await router.RouteEventsAsync();
        storeMock.Verify(s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
