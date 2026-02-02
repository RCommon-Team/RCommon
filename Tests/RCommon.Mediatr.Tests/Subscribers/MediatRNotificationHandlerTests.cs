using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator.Subscribers;
using RCommon.MediatR.Subscribers;
using Xunit;

namespace RCommon.Mediatr.Tests.Subscribers;

public class MediatRNotificationHandlerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServiceProvider_CreatesInstance()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatRNotificationHandler<TestAppNotification, MediatRNotification<TestAppNotification>>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().NotBeNull();
    }

    [Fact]
    public void MediatRNotificationHandler_ImplementsINotificationHandler()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var handler = new MediatRNotificationHandler<TestAppNotification, MediatRNotification<TestAppNotification>>(
            mockServiceProvider.Object);

        // Assert
        handler.Should().BeAssignableTo<INotificationHandler<MediatRNotification<TestAppNotification>>>();
    }

    #endregion

    #region Handle Tests

    [Fact]
    public async Task Handle_ResolvesSubscriberFromServiceProvider()
    {
        // Arrange
        var mockSubscriber = new Mock<ISubscriber<TestAppNotification>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestAppNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestAppNotification>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatRNotificationHandler<TestAppNotification, MediatRNotification<TestAppNotification>>(
            mockServiceProvider.Object);

        var notification = new MediatRNotification<TestAppNotification>(
            new TestAppNotification { Message = "Test" });

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockServiceProvider.Verify(x => x.GetService(typeof(ISubscriber<TestAppNotification>)), Times.Once);
    }

    [Fact]
    public async Task Handle_CallsSubscriberHandleAsync()
    {
        // Arrange
        var mockSubscriber = new Mock<ISubscriber<TestAppNotification>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestAppNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestAppNotification>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatRNotificationHandler<TestAppNotification, MediatRNotification<TestAppNotification>>(
            mockServiceProvider.Object);

        var appNotification = new TestAppNotification { Message = "Test" };
        var notification = new MediatRNotification<TestAppNotification>(appNotification);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockSubscriber.Verify(x => x.HandleAsync(appNotification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PassesNotificationToSubscriber()
    {
        // Arrange
        TestAppNotification? capturedNotification = null;
        var mockSubscriber = new Mock<ISubscriber<TestAppNotification>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestAppNotification>(), It.IsAny<CancellationToken>()))
            .Callback<TestAppNotification, CancellationToken>((n, ct) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestAppNotification>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatRNotificationHandler<TestAppNotification, MediatRNotification<TestAppNotification>>(
            mockServiceProvider.Object);

        var appNotification = new TestAppNotification { Message = "TestMessage" };
        var notification = new MediatRNotification<TestAppNotification>(appNotification);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Message.Should().Be("TestMessage");
    }

    [Fact]
    public async Task Handle_WithNullSubscriber_ThrowsNullReferenceException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<TestAppNotification>)))
            .Returns(null!);

        var handler = new MediatRNotificationHandler<TestAppNotification, MediatRNotification<TestAppNotification>>(
            mockServiceProvider.Object);

        var notification = new MediatRNotification<TestAppNotification>(
            new TestAppNotification { Message = "Test" });

        // Act
        Func<Task> act = async () => await handler.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public async Task Handle_WithComplexNotification_PassesCorrectly()
    {
        // Arrange
        ComplexAppNotification? capturedNotification = null;
        var mockSubscriber = new Mock<ISubscriber<ComplexAppNotification>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<ComplexAppNotification>(), It.IsAny<CancellationToken>()))
            .Callback<ComplexAppNotification, CancellationToken>((n, ct) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(ISubscriber<ComplexAppNotification>)))
            .Returns(mockSubscriber.Object);

        var handler = new MediatRNotificationHandler<ComplexAppNotification, MediatRNotification<ComplexAppNotification>>(
            mockServiceProvider.Object);

        var appNotification = new ComplexAppNotification
        {
            Id = 42,
            Name = "ComplexTest",
            Items = new List<string> { "Item1", "Item2" }
        };
        var notification = new MediatRNotification<ComplexAppNotification>(appNotification);

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        capturedNotification.Should().NotBeNull();
        capturedNotification!.Id.Should().Be(42);
        capturedNotification.Name.Should().Be("ComplexTest");
        capturedNotification.Items.Should().BeEquivalentTo(new[] { "Item1", "Item2" });
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Handle_WithRealServiceProvider_ResolvesSubscriber()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockSubscriber = new Mock<ISubscriber<TestAppNotification>>();
        mockSubscriber
            .Setup(x => x.HandleAsync(It.IsAny<TestAppNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(mockSubscriber.Object);

        var serviceProvider = services.BuildServiceProvider();

        var handler = new MediatRNotificationHandler<TestAppNotification, MediatRNotification<TestAppNotification>>(
            serviceProvider);

        var notification = new MediatRNotification<TestAppNotification>(
            new TestAppNotification { Message = "Integration Test" });

        // Act
        await handler.Handle(notification, CancellationToken.None);

        // Assert
        mockSubscriber.Verify(x => x.HandleAsync(It.IsAny<TestAppNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Test Helper Classes

    public class TestAppNotification : IAppNotification
    {
        public string Message { get; set; } = string.Empty;
    }

    public class ComplexAppNotification : IAppNotification
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }

    #endregion
}
