using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Subscribers;
using RCommon.MassTransit.Subscribers;
using Xunit;

namespace RCommon.MassTransit.Tests;

public class MassTransitEventHandlerTests
{
    private readonly Mock<ILogger<MassTransitEventHandler<TestEvent>>> _mockLogger;
    private readonly Mock<ISubscriber<TestEvent>> _mockSubscriber;
    private readonly Mock<ConsumeContext<TestEvent>> _mockConsumeContext;

    public MassTransitEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<MassTransitEventHandler<TestEvent>>>();
        _mockSubscriber = new Mock<ISubscriber<TestEvent>>();
        _mockConsumeContext = new Mock<ConsumeContext<TestEvent>>();
    }

    private MassTransitEventHandler<TestEvent> CreateHandler()
    {
        return new MassTransitEventHandler<TestEvent>(
            _mockLogger.Object,
            _mockSubscriber.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new MassTransitEventHandler<TestEvent>(
            null!,
            _mockSubscriber.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullSubscriber_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new MassTransitEventHandler<TestEvent>(
            _mockLogger.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("subscriber");
    }

    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        // Act
        var action = () => CreateHandler();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Consume_WithValidContext_CallsSubscriberHandleAsync()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent { Message = "Test" };
        _mockConsumeContext.Setup(x => x.Message).Returns(testEvent);
        _mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.Consume(_mockConsumeContext.Object);

        // Assert
        _mockSubscriber.Verify(
            x => x.HandleAsync(It.Is<TestEvent>(e => e.Message == "Test"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Consume_LogsDebugMessage()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent();
        _mockConsumeContext.Setup(x => x.Message).Returns(testEvent);

        // Act
        await handler.Consume(_mockConsumeContext.Object);

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
    public void Handler_ImplementsIMassTransitEventHandler()
    {
        // Arrange & Act
        var handler = CreateHandler();

        // Assert
        handler.Should().BeAssignableTo<IMassTransitEventHandler<TestEvent>>();
    }

    [Fact]
    public void Handler_ImplementsIConsumer()
    {
        // Arrange & Act
        var handler = CreateHandler();

        // Assert
        handler.Should().BeAssignableTo<IConsumer<TestEvent>>();
    }

    [Fact]
    public async Task Consume_WithMultipleCalls_HandlesEachEvent()
    {
        // Arrange
        var handler = CreateHandler();
        var event1 = new TestEvent { Message = "Event1" };
        var event2 = new TestEvent { Message = "Event2" };

        var context1 = new Mock<ConsumeContext<TestEvent>>();
        var context2 = new Mock<ConsumeContext<TestEvent>>();
        context1.Setup(x => x.Message).Returns(event1);
        context2.Setup(x => x.Message).Returns(event2);

        // Act
        await handler.Consume(context1.Object);
        await handler.Consume(context2.Object);

        // Assert
        _mockSubscriber.Verify(
            x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Consume_WhenSubscriberThrows_PropagatesException()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent();
        _mockConsumeContext.Setup(x => x.Message).Returns(testEvent);
        _mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Subscriber error"));

        // Act
        var action = () => handler.Consume(_mockConsumeContext.Object);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subscriber error");
    }

    [Fact]
    public async Task Consume_PassesEventFromContext()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent
        {
            Id = Guid.NewGuid(),
            Message = "UniqueMessage",
            Timestamp = DateTime.UtcNow
        };
        _mockConsumeContext.Setup(x => x.Message).Returns(testEvent);

        TestEvent? capturedEvent = null;
        _mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, ct) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        // Act
        await handler.Consume(_mockConsumeContext.Object);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Id.Should().Be(testEvent.Id);
        capturedEvent.Message.Should().Be(testEvent.Message);
        capturedEvent.Timestamp.Should().Be(testEvent.Timestamp);
    }
}
