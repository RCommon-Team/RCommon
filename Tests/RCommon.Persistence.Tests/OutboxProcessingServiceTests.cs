using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

// Concrete producer types so their GetType().FullName is stable and matchable against
// OutboxMessage.TargetProducers (which stores comma-delimited producer FullName(s)).
public sealed class TargetProducerX : IEventProducer
{
    private readonly List<ISerializableEvent> _received = new();
    public IReadOnlyList<ISerializableEvent> Received => _received;
    public Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
    {
        _received.Add(@event);
        return Task.CompletedTask;
    }
}

public sealed class TargetProducerY : IEventProducer
{
    private readonly List<ISerializableEvent> _received = new();
    public IReadOnlyList<ISerializableEvent> Received => _received;
    public Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
    {
        _received.Add(@event);
        return Task.CompletedTask;
    }
}

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
            _backoffMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }));

        return (service, provider);
    }

    // Builds a service whose scope has NO IEventProducer registered, so every claimed event resolves
    // to an empty producer set — the "drained event type has zero subscribers on the poller" scenario.
    private (OutboxProcessingService service, Mock<ILogger<OutboxProcessingService>> logger) CreateServiceWithNoProducer()
    {
        var opts = new OutboxOptions { PollingInterval = TimeSpan.FromMilliseconds(50) };
        var services = new ServiceCollection();
        services.AddSingleton(_storeMock.Object);
        services.AddSingleton<IOutboxSerializer>(_serializer);
        services.AddSingleton(_subscriptionManager);
        services.AddSingleton(_backoffMock.Object);
        var provider = services.BuildServiceProvider();

        var logger = new Mock<ILogger<OutboxProcessingService>>();
        var service = new OutboxProcessingService(
            provider, Options.Create(opts), logger.Object, _backoffMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }));
        return (service, logger);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Throws_ClearMessage_When_DefaultDataStoreName_Is_Unresolved(string? unresolvedName)
    {
        var provider = new ServiceCollection().BuildServiceProvider();

        Action act = () => new OutboxProcessingService(
            provider,
            Options.Create(new OutboxOptions()),
            NullLogger<OutboxProcessingService>.Instance,
            _backoffMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = unresolvedName! }));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*no default datastore*")
            .WithMessage("*SetDefaultDataStore*")
            .WithMessage("*AddOutbox*");
    }

    private static void VerifyWarning(Mock<ILogger<OutboxProcessingService>> logger, Times times)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    private OutboxMessage MakeMessage(ISerializableEvent @event) => new()
    {
        Id = Guid.NewGuid(),
        EventType = _serializer.GetEventTypeName(@event),
        EventPayload = _serializer.Serialize(@event),
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task ProcessBatchAsync_Warns_When_No_Producer_Matches()
    {
        var msg = MakeMessage(new PollerTestEvent("orphan"));
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var (service, logger) = CreateServiceWithNoProducer();
        await service.ProcessBatchAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Once());
        // The row is still marked processed so the poller does not re-claim it forever.
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatchAsync_Warns_Once_Per_EventType()
    {
        var msg1 = MakeMessage(new PollerTestEvent("orphan-1"));
        var msg2 = MakeMessage(new PollerTestEvent("orphan-2"));
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg1, msg2 });

        var (service, logger) = CreateServiceWithNoProducer();
        await service.ProcessBatchAsync(CancellationToken.None);

        // Two orphaned messages of the same event type => a single warning, not one per row.
        VerifyWarning(logger, Times.Once());
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
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });
        _producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("transport error"));
        _backoffMock.Setup(b => b.ComputeDelay(1)).Returns(TimeSpan.FromSeconds(10));

        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _storeMock.Verify(s => s.MarkFailedAsync(msg.Id, It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });
        _producerMock.Setup(p => p.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("still down"));

        var opts = new OutboxOptions { MaxRetries = 5, PollingInterval = TimeSpan.FromMilliseconds(50) };
        var (service, _) = CreateService(opts);
        await service.ProcessBatchAsync(CancellationToken.None);

        _storeMock.Verify(s => s.MarkDeadLetteredAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var inboxMock = new Mock<IInboxStore>();
        inboxMock.Setup(i => i.ExistsAsync(msg.Id, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var (service, _) = CreateService(inboxStoreMock: inboxMock);
        await service.ProcessBatchAsync(CancellationToken.None);

        // Should mark processed (as duplicate), but NOT dispatch
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Builds a service whose scope registers the given registry + producers, allowing multi-datastore
    // and target-producer scenarios to be exercised through the public ProcessBatchAsync seam.
    private OutboxProcessingService CreateServiceWith(
        IOutboxDataStoreRegistry registry,
        IEnumerable<IEventProducer> producers,
        OutboxOptions? options = null)
    {
        var opts = options ?? new OutboxOptions { PollingInterval = TimeSpan.FromMilliseconds(50) };
        var services = new ServiceCollection();
        services.AddSingleton(_storeMock.Object);
        services.AddSingleton<IOutboxSerializer>(_serializer);
        foreach (var p in producers)
        {
            services.AddSingleton(p);
        }
        services.AddSingleton(_subscriptionManager);
        services.AddSingleton(_backoffMock.Object);
        services.AddSingleton(registry);
        var provider = services.BuildServiceProvider();

        return new OutboxProcessingService(
            provider,
            Options.Create(opts),
            NullLogger<OutboxProcessingService>.Instance,
            _backoffMock.Object,
            Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" }));
    }

    private static IOutboxDataStoreRegistry Registry(params string[] names)
    {
        var mock = new Mock<IOutboxDataStoreRegistry>();
        mock.Setup(r => r.Registrations).Returns(names);
        return mock.Object;
    }

    [Fact]
    public async Task ProcessBatchAsync_DrainsEveryRegisteredDataStore()
    {
        var eventA = new PollerTestEvent("from-A");
        var eventB = new PollerTestEvent("from-B");
        var msgA = MakeMessage(eventA);
        var msgB = MakeMessage(eventB);

        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), "A", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msgA });
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), "B", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msgB });

        var service = CreateServiceWith(Registry("A", "B"), new[] { _producerMock.Object });
        await service.ProcessBatchAsync(CancellationToken.None);

        // Both datastores were claimed by name...
        _storeMock.Verify(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), "A", It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), "B", It.IsAny<CancellationToken>()), Times.Once);
        // ...and each datastore's row was marked processed against its own datastore name.
        _storeMock.Verify(s => s.MarkProcessedAsync(msgA.Id, "A", It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.MarkProcessedAsync(msgB.Id, "B", It.IsAny<CancellationToken>()), Times.Once);
        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ProcessBatchAsync_TargetProducers_DispatchesOnlyToListedProducer()
    {
        var producerX = new TargetProducerX();
        var producerY = new TargetProducerY();

        var msg = MakeMessage(new PollerTestEvent("targeted"));
        msg.TargetProducers = typeof(TargetProducerX).FullName;

        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var service = CreateServiceWith(Registry("test"), new IEventProducer[] { producerX, producerY });
        await service.ProcessBatchAsync(CancellationToken.None);

        producerX.Received.Should().HaveCount(1);
        producerY.Received.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessBatchAsync_TargetProducers_MultipleTargets_DispatchesToAllListed()
    {
        var producerX = new TargetProducerX();
        var producerY = new TargetProducerY();

        var msg = MakeMessage(new PollerTestEvent("both"));
        msg.TargetProducers = $"{typeof(TargetProducerX).FullName},{typeof(TargetProducerY).FullName}";

        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        var service = CreateServiceWith(Registry("test"), new IEventProducer[] { producerX, producerY });
        await service.ProcessBatchAsync(CancellationToken.None);

        producerX.Received.Should().HaveCount(1);
        producerY.Received.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessBatchAsync_NullTargetProducers_FallsBackToResolveAll()
    {
        var producerX = new TargetProducerX();
        var producerY = new TargetProducerY();

        var msg = MakeMessage(new PollerTestEvent("legacy"));
        msg.TargetProducers = null; // legacy row

        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        // No subscriptions => resolve-all fallback: both producers receive the event.
        var service = CreateServiceWith(Registry("test"), new IEventProducer[] { producerX, producerY });
        await service.ProcessBatchAsync(CancellationToken.None);

        producerX.Received.Should().HaveCount(1);
        producerY.Received.Should().HaveCount(1);
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
        _storeMock.Setup(s => s.ClaimAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IOutboxMessage> { msg });

        // No inboxStoreMock — inbox not registered
        var (service, _) = CreateService();
        await service.ProcessBatchAsync(CancellationToken.None);

        _producerMock.Verify(p => p.ProduceEventAsync(It.IsAny<PollerTestEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.MarkProcessedAsync(msg.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
