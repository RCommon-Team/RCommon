using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

public record RouterTestEvent(string Data) : ISerializableEvent;

public class OutboxEventRouterTests
{
    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IGuidGenerator> _guidGenMock = new();
    private readonly Mock<ITenantIdAccessor> _tenantMock = new();
    private readonly IOutboxSerializer _serializer = new JsonOutboxSerializer();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly EventSubscriptionManager _subscriptionManager = new();

    private OutboxEventRouter CreateRouter()
    {
        _guidGenMock.Setup(g => g.Create()).Returns(Guid.NewGuid());
        // _tenantMock is not setup here; Moq returns null by default for reference types.
        // Individual tests that need a specific tenant can set it up before calling CreateRouter().
        return new OutboxEventRouter(
            _storeMock.Object,
            _serializer,
            _guidGenMock.Object,
            _tenantMock.Object,
            _serviceProviderMock.Object,
            _subscriptionManager,
            NullLogger<OutboxEventRouter>.Instance,
            Options.Create(new OutboxOptions()));
    }

    [Fact]
    public void AddTransactionalEvent_BuffersWithoutCallingStore()
    {
        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("test"));
        _storeMock.Verify(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PersistBufferedEventsAsync_WritesBufferedEventsToStore()
    {
        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("event1"));
        router.AddTransactionalEvent(new RouterTestEvent("event2"));

        await router.PersistBufferedEventsAsync();

        _storeMock.Verify(
            s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task PersistBufferedEventsAsync_ClearsBufferAfterPersistence()
    {
        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("event1"));
        await router.PersistBufferedEventsAsync();

        // Second call should have nothing to persist
        _storeMock.Invocations.Clear();
        await router.PersistBufferedEventsAsync();

        _storeMock.Verify(
            s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PersistBufferedEventsAsync_SetsCorrectMessageFields()
    {
        IOutboxMessage? captured = null;
        _storeMock.Setup(s => s.SaveAsync(It.IsAny<IOutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback<IOutboxMessage, CancellationToken>((msg, _) => captured = msg);
        _tenantMock.Setup(t => t.GetTenantId()).Returns("tenant-1");

        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("data"));
        await router.PersistBufferedEventsAsync();

        captured.Should().NotBeNull();
        captured!.EventType.Should().Contain("RouterTestEvent");
        captured.EventPayload.Should().Contain("data");
        captured.TenantId.Should().Be("tenant-1");
        captured.RetryCount.Should().Be(0);
        captured.ProcessedAtUtc.Should().BeNull();
        captured.DeadLetteredAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RouteEventsAsync_DispatchesPendingFromStore()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(new RouterTestEvent("x")),
            EventPayload = _serializer.Serialize(new RouterTestEvent("x")),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var producerMock = new Mock<IEventProducer>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventProducer>)))
            .Returns(new[] { producerMock.Object });

        var router = CreateRouter();
        await router.RouteEventsAsync();

        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteEventsAsync_MarksFailedOnException()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(new RouterTestEvent("x")),
            EventPayload = _serializer.Serialize(new RouterTestEvent("x")),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _storeMock.Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var producerMock = new Mock<IEventProducer>();
        producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("broker down"));
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventProducer>)))
            .Returns(new[] { producerMock.Object });

        var router = CreateRouter();
        await router.RouteEventsAsync();

        _storeMock.Verify(s => s.MarkFailedAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
