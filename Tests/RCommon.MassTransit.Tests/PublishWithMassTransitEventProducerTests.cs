using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.MassTransit.Producers;
using Xunit;

namespace RCommon.MassTransit.Tests;

public class PublishWithMassTransitEventProducerTests
{
    private readonly Mock<IBus> _mockBus;
    private readonly Mock<ILogger<PublishWithMassTransitEventProducer>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;

    public PublishWithMassTransitEventProducerTests()
    {
        _mockBus = new Mock<IBus>();
        _mockLogger = new Mock<ILogger<PublishWithMassTransitEventProducer>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
    }

    private PublishWithMassTransitEventProducer CreateProducer()
    {
        return new PublishWithMassTransitEventProducer(
            _mockBus.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object);
    }

    [Fact]
    public void Constructor_WithNullBus_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new PublishWithMassTransitEventProducer(
            null!,
            _mockLogger.Object,
            _mockServiceProvider.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("bus");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new PublishWithMassTransitEventProducer(
            _mockBus.Object,
            null!,
            _mockServiceProvider.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new PublishWithMassTransitEventProducer(
            _mockBus.Object,
            _mockLogger.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        // Act
        var action = () => CreateProducer();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task ProduceEventAsync_WithNullEvent_ThrowsNullReferenceException()
    {
        // Arrange
        var producer = CreateProducer();

        // Act
        var action = () => producer.ProduceEventAsync<TestEvent>(null!);

        // Assert
        // The producer does not validate null input, so it throws NullReferenceException
        // when trying to get the type name of the null event
        await action.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task ProduceEventAsync_WithValidEvent_CallsBusPublish()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent { Message = "Test" };

        _mockBus
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        // Verify using non-generic Publish(object, CancellationToken) signature
        _mockBus.Verify(
            x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_WithValidEvent_CreatesScope()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        _mockServiceProvider.Verify(
            x => x.GetService(typeof(IServiceScopeFactory)),
            Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_WithCancellationToken_PassesTokenToBus()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent();
        var cts = new CancellationTokenSource();

        // Act
        await producer.ProduceEventAsync(testEvent, cts.Token);

        // Assert
        _mockBus.Verify(
            x => x.Publish(It.IsAny<object>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_WhenBusThrows_ThrowsEventProductionException()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent();

        _mockBus
            .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Bus error"));

        // Act
        var action = () => producer.ProduceEventAsync(testEvent);

        // Assert
        await action.Should().ThrowAsync<EventProductionException>();
    }

    [Fact]
    public void Producer_ImplementsIEventProducer()
    {
        // Arrange & Act
        var producer = CreateProducer();

        // Assert
        producer.Should().BeAssignableTo<IEventProducer>();
    }

    [Fact]
    public async Task ProduceEventAsync_WithInfoLogEnabled_LogsInformation()
    {
        // Arrange
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);
        var producer = CreateProducer();
        var testEvent = new TestEvent { Message = "Test" };

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProduceEventAsync_WithInfoLogDisabled_LogsDebug()
    {
        // Arrange
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(false);
        var producer = CreateProducer();
        var testEvent = new TestEvent();

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProduceEventAsync_DisposesScope_AfterPublishing()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent();

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        _mockScope.Verify(x => x.Dispose(), Times.Once);
    }
}
