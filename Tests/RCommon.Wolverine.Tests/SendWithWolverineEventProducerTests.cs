using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Wolverine.Producers;
using Wolverine;
using Xunit;

namespace RCommon.Wolverine.Tests;

public class SendWithWolverineEventProducerTests
{
    private readonly Mock<IMessageBus> _mockMessageBus;
    private readonly Mock<ILogger<SendWithWolverineEventProducer>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;

    public SendWithWolverineEventProducerTests()
    {
        _mockMessageBus = new Mock<IMessageBus>();
        _mockLogger = new Mock<ILogger<SendWithWolverineEventProducer>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
    }

    private SendWithWolverineEventProducer CreateProducer()
    {
        return new SendWithWolverineEventProducer(
            _mockMessageBus.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            new EventSubscriptionManager());
    }

    [Fact]
    public void Constructor_WithNullMessageBus_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SendWithWolverineEventProducer(
            null!,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            new EventSubscriptionManager());

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("messageBus");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SendWithWolverineEventProducer(
            _mockMessageBus.Object,
            null!,
            _mockServiceProvider.Object,
            new EventSubscriptionManager());

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SendWithWolverineEventProducer(
            _mockMessageBus.Object,
            _mockLogger.Object,
            null!,
            new EventSubscriptionManager());

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullSubscriptionManager_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SendWithWolverineEventProducer(
            _mockMessageBus.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("subscriptionManager");
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
    public async Task ProduceEventAsync_WithValidEvent_CallsMessageBusSendAsync()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent { Message = "Test" };

        _mockMessageBus
            .Setup(x => x.SendAsync(It.IsAny<TestEvent>(), It.IsAny<DeliveryOptions?>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await producer.ProduceEventAsync(testEvent);

        // Assert
        _mockMessageBus.Verify(
            x => x.SendAsync(It.Is<TestEvent>(e => e.Message == "Test"), It.IsAny<DeliveryOptions?>()),
            Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_WhenMessageBusThrows_ThrowsEventProductionException()
    {
        // Arrange
        var producer = CreateProducer();
        var testEvent = new TestEvent();

        _mockMessageBus
            .Setup(x => x.SendAsync(It.IsAny<TestEvent>(), It.IsAny<DeliveryOptions?>()))
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
    public async Task ProduceEventAsync_DisposesScope_AfterSending()
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
    public async Task ProduceEventAsync_CreatesScope_BeforeSending()
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
