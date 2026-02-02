using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Subscribers;
using RCommon.MassTransit.Subscribers;
using Wolverine;
using Xunit;

namespace RCommon.Wolverine.Tests;

public class WolverineEventHandlerTests
{
    private readonly Mock<ILogger<WolverineEventHandler<TestEvent>>> _mockLogger;
    private readonly Mock<ISubscriber<TestEvent>> _mockSubscriber;

    public WolverineEventHandlerTests()
    {
        _mockLogger = new Mock<ILogger<WolverineEventHandler<TestEvent>>>();
        _mockSubscriber = new Mock<ISubscriber<TestEvent>>();
    }

    private WolverineEventHandler<TestEvent> CreateHandler()
    {
        return new WolverineEventHandler<TestEvent>(
            _mockSubscriber.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullSubscriber_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new WolverineEventHandler<TestEvent>(
            null!,
            _mockLogger.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("subscriber");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new WolverineEventHandler<TestEvent>(
            _mockSubscriber.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
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
    public async Task HandleAsync_WithValidEvent_CallsSubscriberHandleAsync()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent { Message = "Test" };
        _mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await handler.HandleAsync(testEvent);

        // Assert
        _mockSubscriber.Verify(
            x => x.HandleAsync(It.Is<TestEvent>(e => e.Message == "Test"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LogsDebugMessage()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent();

        // Act
        await handler.HandleAsync(testEvent);

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
    public void Handler_ImplementsIWolverineEventHandler()
    {
        // Arrange & Act
        var handler = CreateHandler();

        // Assert
        handler.Should().BeAssignableTo<IWolverineEventHandler<TestEvent>>();
    }

    [Fact]
    public void Handler_ImplementsIWolverineHandler()
    {
        // Arrange & Act
        var handler = CreateHandler();

        // Assert
        handler.Should().BeAssignableTo<IWolverineHandler>();
    }

    [Fact]
    public async Task HandleAsync_WithMultipleCalls_HandlesEachEvent()
    {
        // Arrange
        var handler = CreateHandler();
        var event1 = new TestEvent { Message = "Event1" };
        var event2 = new TestEvent { Message = "Event2" };

        // Act
        await handler.HandleAsync(event1);
        await handler.HandleAsync(event2);

        // Assert
        _mockSubscriber.Verify(
            x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task HandleAsync_WhenSubscriberThrows_PropagatesException()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent();
        _mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Subscriber error"));

        // Act
        var action = () => handler.HandleAsync(testEvent);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subscriber error");
    }

    [Fact]
    public async Task HandleAsync_PassesEventToSubscriber()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent
        {
            Id = Guid.NewGuid(),
            Message = "UniqueMessage",
            Timestamp = DateTime.UtcNow
        };

        TestEvent? capturedEvent = null;
        _mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestEvent, CancellationToken>((e, ct) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        // Act
        await handler.HandleAsync(testEvent);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Id.Should().Be(testEvent.Id);
        capturedEvent.Message.Should().Be(testEvent.Message);
        capturedEvent.Timestamp.Should().Be(testEvent.Timestamp);
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_CallsSubscriber()
    {
        // Arrange
        var handler = CreateHandler();
        var testEvent = new TestEvent();
        var cts = new CancellationTokenSource();

        // Act
        await handler.HandleAsync(testEvent, cts.Token);

        // Assert
        // Note: The current implementation does not pass the cancellation token to the subscriber
        _mockSubscriber.Verify(
            x => x.HandleAsync(It.IsAny<TestEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
