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
    public async Task RouteEventsAsync_DispatchesRetainedEvents()
    {
        var producerMock = new Mock<IEventProducer>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventProducer>)))
            .Returns(new[] { producerMock.Object });

        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("x"));
        await router.PersistBufferedEventsAsync();

        await router.RouteEventsAsync();

        producerMock.Verify(
            p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _storeMock.Verify(
            s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RouteEventsAsync_LogsWarningOnException_DoesNotMarkFailed()
    {
        var producerMock = new Mock<IEventProducer>();
        producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("broker down"));
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventProducer>)))
            .Returns(new[] { producerMock.Object });

        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("x"));
        await router.PersistBufferedEventsAsync();

        await router.RouteEventsAsync();

        _storeMock.Verify(
            s => s.MarkFailedAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storeMock.Verify(
            s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RouteEventsAsync_ClearsRetainedEventsAfterDispatch()
    {
        var producerMock = new Mock<IEventProducer>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IEnumerable<IEventProducer>)))
            .Returns(new[] { producerMock.Object });

        var router = CreateRouter();
        router.AddTransactionalEvent(new RouterTestEvent("x"));
        await router.PersistBufferedEventsAsync();

        await router.RouteEventsAsync();

        // Second call should be a no-op: no retained events left
        _storeMock.Invocations.Clear();
        producerMock.Invocations.Clear();
        await router.RouteEventsAsync();

        producerMock.Verify(
            p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storeMock.Verify(
            s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RouteEventsAsync_NoRetainedEvents_ReturnsImmediately()
    {
        var router = CreateRouter();

        // No PersistBufferedEventsAsync called - no retained events
        await router.RouteEventsAsync();

        _storeMock.Verify(
            s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _storeMock.Verify(
            s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
