using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

public class InMemoryTransactionalEventRouterTests
{
    private readonly Mock<ILogger<InMemoryTransactionalEventRouter>> _mockLogger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;
    private readonly IOptions<EventHandlingOptions> _options;

    public InMemoryTransactionalEventRouterTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryTransactionalEventRouter>>();
        _services = new ServiceCollection();
        _services.AddSingleton(_mockLogger.Object);
        _serviceProvider = _services.BuildServiceProvider();
        _options = Options.Create(new EventHandlingOptions());
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        // Assert
        router.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InMemoryTransactionalEventRouter(null!, _mockLogger.Object, new EventSubscriptionManager(), _options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InMemoryTransactionalEventRouter(_serviceProvider, null!, new EventSubscriptionManager(), _options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullSubscriptionManager_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, null!, _options);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("subscriptionManager");
    }

    [Fact]
    public void Constructor_WithNullEventHandlingOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventHandlingOptions");
    }

    #endregion

    #region AddTransactionalEvent Tests

    [Fact]
    public void AddTransactionalEvent_WithValidEvent_AddsToQueue()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var mockEvent = new Mock<ISerializableEvent>();

        // Act
        router.AddTransactionalEvent(mockEvent.Object);

        // Assert - no exception thrown means success
        // The event will be processed on RouteEventsAsync
    }

    [Fact]
    public void AddTransactionalEvent_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        // Act
        var act = () => router.AddTransactionalEvent(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region AddTransactionalEvents Tests

    [Fact]
    public void AddTransactionalEvents_WithValidEvents_AddsAllToQueue()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var events = new List<ISerializableEvent>
        {
            new Mock<ISerializableEvent>().Object,
            new Mock<ISerializableEvent>().Object,
            new Mock<ISerializableEvent>().Object
        };

        // Act
        router.AddTransactionalEvents(events);

        // Assert - no exception thrown means success
    }

    [Fact]
    public void AddTransactionalEvents_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        // Act
        var act = () => router.AddTransactionalEvents(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region RouteEventsAsync (IEnumerable) Tests

    [Fact]
    public async Task RouteEventsAsync_WithEmptyCollection_CompletesSuccessfully()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var events = Enumerable.Empty<ISerializableEvent>();

        // Act
        var act = async () => await router.RouteEventsAsync(events);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RouteEventsAsync_WithNullCollection_ThrowsEventProductionException()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        // Act
        var act = async () => await router.RouteEventsAsync(null!);

        // Assert - The method wraps exceptions in EventProductionException
        await act.Should().ThrowAsync<EventProductionException>();
    }

    [Fact]
    public async Task RouteEventsAsync_WithSyncEvents_CallsProducers()
    {
        // Arrange
        var mockProducer = new Mock<IEventProducer>();
        mockProducer.Setup(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(mockProducer.Object);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var mockEvent = new Mock<ISyncEvent>();
        mockEvent.As<ISerializableEvent>();
        var events = new List<ISerializableEvent> { mockEvent.As<ISerializableEvent>().Object };

        // Act
        await router.RouteEventsAsync(events);

        // Assert
        mockProducer.Verify(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteEventsAsync_WithAsyncEvents_CallsProducers()
    {
        // Arrange
        var mockProducer = new Mock<IEventProducer>();
        mockProducer.Setup(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(mockProducer.Object);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var mockEvent = new Mock<IAsyncEvent>();
        mockEvent.As<ISerializableEvent>();
        var events = new List<ISerializableEvent> { mockEvent.As<ISerializableEvent>().Object };

        // Act
        await router.RouteEventsAsync(events);

        // Assert
        mockProducer.Verify(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Log-Ordering: Zero Producers vs. Nonzero Producers

    [Fact]
    public async Task RouteEventsAsync_EventsWithZeroProducers_LogsWarningNotInformation()
    {
        // Arrange -- no IEventProducer registered at all; this is the silent-failure scenario
        // from docs/specs/event-handling/producer-auto-registration.md.
        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var mockEvent = new Mock<ISyncEvent>();
        var events = new List<ISerializableEvent> { mockEvent.As<ISerializableEvent>().Object };

        // Act
        await router.RouteEventsAsync(events);

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("no IEventProducer is")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("transactional events to event producers")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task RouteEventsAsync_EventsWithAtLeastOneProducer_LogsInformationNotWarning()
    {
        // Arrange -- regression guard: the pre-existing, always-fires LogInformation still fires
        // when there is at least one producer to route to.
        var mockProducer = new Mock<IEventProducer>();
        mockProducer.Setup(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(mockProducer.Object);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var mockEvent = new Mock<ISyncEvent>();
        var events = new List<ISerializableEvent> { mockEvent.As<ISerializableEvent>().Object };

        // Act
        await router.RouteEventsAsync(events);

        // Assert
        _mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains("transactional events to event producers")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region RouteEventsAsync (No Parameters) Tests

    [Fact]
    public async Task RouteEventsAsync_WithNoStoredEvents_CompletesSuccessfully()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        // Act
        var act = async () => await router.RouteEventsAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RouteEventsAsync_WithStoredEvents_ProcessesAllEvents()
    {
        // Arrange
        var mockProducer = new Mock<IEventProducer>();
        mockProducer.Setup(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(mockProducer.Object);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        var mockEvent1 = new Mock<ISyncEvent>();
        mockEvent1.As<ISerializableEvent>();
        var mockEvent2 = new Mock<ISyncEvent>();
        mockEvent2.As<ISerializableEvent>();

        router.AddTransactionalEvent(mockEvent1.As<ISerializableEvent>().Object);
        router.AddTransactionalEvent(mockEvent2.As<ISerializableEvent>().Object);

        // Act
        await router.RouteEventsAsync();

        // Assert
        mockProducer.Verify(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region IEventRouter Interface Tests

    [Fact]
    public void InMemoryTransactionalEventRouter_ImplementsIEventRouter()
    {
        // Arrange & Act
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        // Assert
        router.Should().BeAssignableTo<IEventRouter>();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task RouteEventsAsync_WhenProducerThrows_ThrowsEventProductionException()
    {
        // Arrange
        var mockProducer = new Mock<IEventProducer>();
        mockProducer.Setup(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Producer failed"));

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton(mockProducer.Object);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        var mockEvent = new Mock<ISyncEvent>();
        mockEvent.As<ISerializableEvent>();
        var events = new List<ISerializableEvent> { mockEvent.As<ISerializableEvent>().Object };

        // Act
        var act = async () => await router.RouteEventsAsync(events);

        // Assert
        await act.Should().ThrowAsync<EventProductionException>();
    }

    #endregion

    #region FIFO Drain: Generation-Tracked Ordering & Cascade Cycle-Breaker (AC-3, AC-4)

    private sealed class SyncTestEvent : ISyncEvent
    {
        public string Name { get; }
        public SyncTestEvent(string name) => Name = name;
        public override string ToString() => Name;
    }

    private sealed class AsyncTestEvent : IAsyncEvent
    {
        public string Name { get; }
        public AsyncTestEvent(string name) => Name = name;
        public override string ToString() => Name;
    }

    /// <summary>
    /// A recording producer that invokes a supplied delegate for each event it handles.
    /// The delegate receives the event and the router so a handler may raise further events.
    /// </summary>
    private sealed class RecordingProducer : IEventProducer
    {
        private readonly Func<ISerializableEvent, Task> _onEvent;
        public RecordingProducer(Func<ISerializableEvent, Task> onEvent) => _onEvent = onEvent;

        public Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : ISerializableEvent
            => _onEvent(@event);
    }

    [Fact]
    public async Task Drain_Dispatches_Sync_Events_In_Raise_Order()
    {
        // Arrange
        var recorded = new List<ISerializableEvent>();
        var producer = new RecordingProducer(e => { recorded.Add(e); return Task.CompletedTask; });

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton<IEventProducer>(producer);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);

        var e1 = new SyncTestEvent("E1");
        var e2 = new SyncTestEvent("E2");
        var e3 = new SyncTestEvent("E3");
        router.AddTransactionalEvent(e1);
        router.AddTransactionalEvent(e2);
        router.AddTransactionalEvent(e3);

        // Act
        await router.RouteEventsAsync();

        // Assert
        recorded.Should().Equal(new ISerializableEvent[] { e1, e2, e3 });
    }

    [Fact]
    public async Task Drain_Processes_Events_Raised_By_Handlers_In_Same_Pass()
    {
        // Arrange
        var e1 = new SyncTestEvent("E1");
        var e2 = new SyncTestEvent("E2");
        var recorded = new List<ISerializableEvent>();

        InMemoryTransactionalEventRouter? routerRef = null;
        var producer = new RecordingProducer(e =>
        {
            recorded.Add(e);
            if (ReferenceEquals(e, e1))
            {
                routerRef!.AddTransactionalEvent(e2);
            }
            return Task.CompletedTask;
        });

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton<IEventProducer>(producer);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        routerRef = router;

        router.AddTransactionalEvent(e1); // gen 0

        // Act
        await router.RouteEventsAsync();

        // Assert - both dispatched, in order, and queue drained
        recorded.Should().Equal(new ISerializableEvent[] { e1, e2 });

        // Second drain proves the queue is now empty (no re-dispatch)
        recorded.Clear();
        await router.RouteEventsAsync();
        recorded.Should().BeEmpty();
    }

    [Fact]
    public async Task Drain_Throws_DispatchGenerationLimitException_On_Runaway_Sync_Cascade()
    {
        // Arrange
        var options = Options.Create(new EventHandlingOptions { MaxDispatchGenerations = 3 });

        InMemoryTransactionalEventRouter? routerRef = null;
        var producer = new RecordingProducer(e =>
        {
            // Always raise another sync event -> unbounded cascade
            routerRef!.AddTransactionalEvent(new SyncTestEvent("cascade"));
            return Task.CompletedTask;
        });

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton<IEventProducer>(producer);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), options);
        routerRef = router;

        router.AddTransactionalEvent(new SyncTestEvent("seed")); // gen 0

        // Act
        var act = async () => await router.RouteEventsAsync();

        // Assert
        (await act.Should().ThrowAsync<DispatchGenerationLimitException>())
            .Which.MaxDispatchGenerations.Should().Be(3);
    }

    [Fact]
    public async Task Drain_Throws_DispatchGenerationLimitException_On_Runaway_Async_Cascade()
    {
        // Arrange
        var options = Options.Create(new EventHandlingOptions { MaxDispatchGenerations = 3 });

        InMemoryTransactionalEventRouter? routerRef = null;
        var producer = new RecordingProducer(e =>
        {
            // Always raise another async event -> unbounded cascade, surfacing via Task.WhenAll
            routerRef!.AddTransactionalEvent(new AsyncTestEvent("cascade"));
            return Task.CompletedTask;
        });

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton<IEventProducer>(producer);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), options);
        routerRef = router;

        router.AddTransactionalEvent(new AsyncTestEvent("seed")); // gen 0

        // Act
        var act = async () => await router.RouteEventsAsync();

        // Assert - propagated UNWRAPPED (not an EventProductionException)
        (await act.Should().ThrowAsync<DispatchGenerationLimitException>())
            .Which.MaxDispatchGenerations.Should().Be(3);
    }

    [Fact]
    public async Task Drain_Awaits_A_Run_Of_Async_Events_Concurrently()
    {
        // Arrange - two async handlers that each rendezvous with the other. If dispatched
        // sequentially this deadlocks; concurrent dispatch lets both start and release.
        var bothStarted = new SemaphoreSlim(0, 2);
        var releaseA = new TaskCompletionSource();
        var releaseB = new TaskCompletionSource();

        var e1 = new AsyncTestEvent("A1");
        var e2 = new AsyncTestEvent("A2");

        var producer = new RecordingProducer(async e =>
        {
            if (ReferenceEquals(e, e1))
            {
                bothStarted.Release();
                await releaseA.Task;
            }
            else
            {
                bothStarted.Release();
                await releaseB.Task;
            }
        });

        // Coordinator: once both handlers have started, release both.
        _ = Task.Run(async () =>
        {
            await bothStarted.WaitAsync();
            await bothStarted.WaitAsync();
            releaseA.SetResult();
            releaseB.SetResult();
        });

        var services = new ServiceCollection();
        services.AddSingleton(_mockLogger.Object);
        services.AddSingleton<IEventProducer>(producer);
        var serviceProvider = services.BuildServiceProvider();

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object, new EventSubscriptionManager(), _options);
        router.AddTransactionalEvent(e1);
        router.AddTransactionalEvent(e2);

        // Act - a regression to sequential dispatch would hang; surface it as a timeout instead.
        var act = async () => await router.RouteEventsAsync().WaitAsync(TimeSpan.FromSeconds(5));

        // Assert - completing without timeout proves concurrency.
        await act.Should().NotThrowAsync();
    }

    #endregion
}
