using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Wolverine.Producers;
using Wolverine;
using Xunit;

namespace RCommon.Wolverine.Tests;

public class PublishWithWolverineEventProducerTests
{
    private readonly Mock<IMessageBus> _mockMessageBus;
    private readonly Mock<ILogger<PublishWithWolverineEventProducer>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;

    public PublishWithWolverineEventProducerTests()
    {
        _mockMessageBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<PublishWithWolverineEventProducer>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
    }

    private PublishWithWolverineEventProducer CreateProducer()
    {
        return new PublishWithWolverineEventProducer(
            _mockMessageBus.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object);
    }

    [Fact]
    public void Constructor_WithNullMessageBus_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new PublishWithWolverineEventProducer(
            null!,
            _mockLogger.Object,
            _mockServiceProvider.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("messageBus");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new PublishWithWolverineEventProducer(
            _mockMessageBus.Object,
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
        var action = () => new PublishWithWolverineEventProducer(
            _mockMessageBus.Object,
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
    public async Task ProduceEventAsync_WithValidEvent_CallsMessageBusPublishAsync()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent { Message = "Test" };

        _mockMessageBus
            .Setup(x => x.PublishAsync(It.IsAny<TestEvent>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        _mockMessageBus.Verify(
            x => x.PublishAsync(It.Is<TestEvent>(e => e.Message == "Test"), It.IsAny<DeliveryOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_WhenMessageBusThrows_ThrowsEventProductionException()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent();

        _mockMessageBus
            .Setup(x => x.PublishAsync(It.IsAny<TestEvent>(), It.IsAny<DeliveryOptions?>()))
            .ThrowsAsync(new Exception("Message bus error"));

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
        var testEvent = new TestEvent();

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

    [Fact]
    public async Task ProduceEventAsync_CreatesScope_BeforePublishing()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent();

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        _mockServiceProvider.Verify(
            x => x.GetService(typeof(IServiceScopeFactory)),
            Times.Once);
    }
}
