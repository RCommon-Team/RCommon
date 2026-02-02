using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling.Subscribers;
using RCommon.MediatR.Subscribers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Mediatr.Tests.Subscribers;

public class MediatREventHandlerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServiceProvider_CreatesInstance()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void MediatREventHandler_ImplementsINotificationHandler()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().BeAssignableTo<INotificationHandler<MediatRNotification<TestSerializableEvent>>>();
    }

    #endregion

    #region Handle Tests

    [Fact]
    public async Task Handle_ResolvesSubscriberFromServiceProvider()
    {
        // Arrange
        var mockSubscriber = new Mock<ISubscriber<TestSerializableEvent>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestSerializableEvent>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            mockServiceProvider.Object);

        var notification = new MediatRNotification<TestSerializableEvent>(
            new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" });

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockServiceProvider.Verify(x => x.GetService(typeof(ISubscriber<TestSerializableEvent>)), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsSubscriberHandleAsync()
    {
        // Arrange
        var mockSubscriber = new Mock<ISubscriber<TestSerializableEvent>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestSerializableEvent>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            mockServiceProvider.Object);

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };
        var notification = new MediatRNotification<TestSerializableEvent>(@event);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockSubscriber.Verify(x => x.HandleAsync(@event, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesEventToSubscriber()
    {
        // Arrange
        TestSerializableEvent? capturedEvent = null;
        var mockSubscriber = new Mock<ISubscriber<TestSerializableEvent>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestSerializableEvent, CancellationToken>((e, ct) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestSerializableEvent>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            mockServiceProvider.Object);

        var eventId = Guid.NewGuid();
        var @event = new TestSerializableEvent { EventId = eventId, Message = "TestMessage" };
        var notification = new MediatRNotification<TestSerializableEvent>(@event);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EventId.Should().Be(eventId);
        capturedEvent.Message.Should().Be("TestMessage");
    }

    [Fact]
    public async Task Handle_WithNullSubscriber_ThrowsNullReferenceException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestSerializableEvent>)))
            .Returns(null!);

        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            mockServiceProvider.Object);

        var notification = new MediatRNotification<TestSerializableEvent>(
            new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" });

        // Act
        Func<Task> act = async () => await handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task Handle_WithComplexEvent_PassesCorrectly()
    {
        // Arrange
        ComplexSerializableEvent? capturedEvent = null;
        var mockSubscriber = new Mock<ISubscriber<ComplexSerializableEvent>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<ComplexSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ComplexSerializableEvent, CancellationToken>((e, ct) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<ComplexSerializableEvent>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatREventHandler<ComplexSerializableEvent, MediatRNotification<ComplexSerializableEvent>>(
            mockServiceProvider.Object);

        var eventId = Guid.NewGuid();
        var @event = new ComplexSerializableEvent
        {
            EventId = eventId,
            Name = "ComplexTest",
            Timestamp = new DateTime(2024, 1, 15),
            Items = new List<string> { "Item1", "Item2" }
        };
        var notification = new MediatRNotification<ComplexSerializableEvent>(@event);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EventId.Should().Be(eventId);
        capturedEvent.Name.Should().Be("ComplexTest");
        capturedEvent.Timestamp.Should().Be(new DateTime(2024, 1, 15));
        capturedEvent.Items.Should().BeEquivalentTo(new[] { "Item1", "Item2" });
    }

    [Fact]
    public async Task Handle_PropagatesExceptionFromSubscriber()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Subscriber error");
        var mockSubscriber = new Mock<ISubscriber<TestSerializableEvent>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestSerializableEvent>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            mockServiceProvider.Object);

        var notification = new MediatRNotification<TestSerializableEvent>(
            new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" });

        // Act
        Func<Task> act = async () => await handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subscriber error");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Handle_WithRealServiceProvider_ResolvesSubscriber()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockSubscriber = new Mock<ISubscriber<TestSerializableEvent>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(mockSubscriber.Object);

        var serviceProvider = services.BuildServiceProvider();

        var handler = new MediatREventHandler<TestSerializableEvent, MediatRNotification<TestSerializableEvent>>(
            serviceProvider);

        var notification = new MediatRNotification<TestSerializableEvent>(
            new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Integration Test" });

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockSubscriber.Verify(x => x.HandleAsync(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Test Helper Classes

    public class TestSerializableEvent : ISerializableEvent
    {
        public Guid EventId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ComplexSerializableEvent : ISerializableEvent
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public List<string> Items { get; set; } = new();
    }

    #endregion
}
