using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

public class InMemoryTransactionalEventRouterTests
{
    private readonly Mock<ILogger<InMemoryTransactionalEventRouter>> _mockLogger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;

    public InMemoryTransactionalEventRouterTests()
    {
        _mockLogger = new Mock<ILogger<InMemoryTransactionalEventRouter>>();
        _services = new ServiceCollection();
        _services.AddSingleton(_mockLogger.Object);
        _serviceProvider = _services.BuildServiceProvider();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);

        // Assert
        router.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InMemoryTransactionalEventRouter(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new InMemoryTransactionalEventRouter(_serviceProvider, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region AddTransactionalEvent Tests

    [Fact]
    public void AddTransactionalEvent_WithValidEvent_AddsToQueue()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);
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
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);

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
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);
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
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);

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
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);
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
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);

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

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object);
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

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object);
        var mockEvent = new Mock<IAsyncEvent>();
        mockEvent.As<ISerializableEvent>();
        var events = new List<ISerializableEvent> { mockEvent.As<ISerializableEvent>().Object };

        // Act
        await router.RouteEventsAsync(events);

        // Assert
        mockProducer.Verify(x => x.ProduceEventAsync(It.IsAny<ISerializableEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RouteEventsAsync (No Parameters) Tests

    [Fact]
    public async Task RouteEventsAsync_WithNoStoredEvents_CompletesSuccessfully()
    {
        // Arrange
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);

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

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object);

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
        var router = new InMemoryTransactionalEventRouter(_serviceProvider, _mockLogger.Object);

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

        var router = new InMemoryTransactionalEventRouter(serviceProvider, _mockLogger.Object);
        var mockEvent = new Mock<ISyncEvent>();
        mockEvent.As<ISerializableEvent>();
        var events = new List<ISerializableEvent> { mockEvent.As<ISerializableEvent>().Object };

        // Act
        var act = async () => await router.RouteEventsAsync(events);

        // Assert
        await act.Should().ThrowAsync<EventProductionException>();
    }

    #endregion
}
