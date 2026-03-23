using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Inbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public record PollerTestEvent(string Data) : ISerializableEvent;

public class OutboxProcessingServiceTests
{
    private readonly Mock<IOutboxStore> _storeMock = new();
    private readonly Mock<IEventProducer> _producerMock = new();
    private readonly Mock<IBackoffStrategy> _backoffMock = new();
    private readonly IOutboxSerializer _serializer = new JsonOutboxSerializer();
    private readonly EventSubscriptionManager _subscriptionManager = new();

    private (OutboxProcessingService service, IServiceProvider provider) CreateService(
        OutboxOptions? options = null,
        Mock<IInboxStore>? inboxStoreMock = null)
    {
        var opts = options ?? new OutboxOptions { PollingInterval = TimeSpan.FromMilliseconds(50) };

        var services = new ServiceCollection();
        services.AddSingleton(_storeMock.Object);
        services.AddSingleton<IOutboxSerializer>(_serializer);
        services.AddSingleton<IEventProducer>(_producerMock.Object);
        services.AddSingleton(_subscriptionManager);
        services.AddSingleton(_backoffMock.Object);

        if (inboxStoreMock != null)
        {
            services.AddSingleton<IInboxStore>(inboxStoreMock.Object);
        }

        var provider = services.BuildServiceProvider();

        var service = new OutboxProcessingService(
            provider,
            Options.Create(opts),
            NullLogger<OutboxProcessingService>.Instance,
            _backoffMock.Object);

        return (service, provider);
    }

    [Fact]
    public async Task ProcessBatchAsync_DispatchesAndMarksProcessed()
    {
        var @event = new PollerTestEvent("hello");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_MarksFailedOnException()
    {
        var @event = new PollerTestEvent("fail");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0
        };
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });
        _producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("transport error"));
        _backoffMock.Setup(b => b.ComputeDelay(1)).Returns(TimeSpan.FromSeconds(10));

        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _storeMock.Verify(s => s.MarkFailedAsync(msg.Id, It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_DeadLettersWhenMaxRetriesExceeded()
    {
        var @event = new PollerTestEvent("dead");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 5
        };
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });
        _producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("still down"));

        var opts = new OutboxOptions { MaxRetries = 5, PollingInterval = TimeSpan.FromMilliseconds(50) };
        var (service, _) = CreateService(opts);
        await service.ProcessBatchAsync(CancellationToken.None);

        _storeMock.Verify(s => s.MarkDeadLetteredAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_InboxRegistered_SkipsDuplicateMessage()
    {
        var @event = new PollerTestEvent("duplicate");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0
        };
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var inboxMock = new Mock<IInboxStore>();
        inboxMock.Setup(i => i.ExistsAsync(msg.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (service, _) = CreateService(inboxStoreMock: inboxMock);
        await service.ProcessBatchAsync(CancellationToken.None);

        // Should mark processed (as duplicate), but NOT dispatch
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatchAsync_InboxNotRegistered_DispatchesNormally()
    {
        var @event = new PollerTestEvent("normal");
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = _serializer.GetEventTypeName(@event),
            EventPayload = _serializer.Serialize(@event),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0
        };
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        // No inboxStoreMock — inbox not registered
        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
