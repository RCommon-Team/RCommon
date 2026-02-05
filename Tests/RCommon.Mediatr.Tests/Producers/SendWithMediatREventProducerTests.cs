using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Mediator;
using RCommon.MediatR.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Mediatr.Tests.Producers;

public class SendWithMediatREventProducerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider.Object,
            new EventSubscriptionManager());

        // Assert
        producer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ImplementsIEventProducer()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider.Object,
            new EventSubscriptionManager());

        // Assert
        producer.Should().BeAssignableTo<IEventProducer>();
    }

    [Fact]
    public void Constructor_WithNullMediatorService_ThrowsArgumentNullException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        Action act = () => new SendWithMediatREventProducer(
            null!,
            mockLogger.Object,
            mockServiceProvider.Object,
            new EventSubscriptionManager());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        Action act = () => new SendWithMediatREventProducer(
            mockMediatorService.Object,
            null!,
            mockServiceProvider.Object,
            new EventSubscriptionManager());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();

        // Act
        Action act = () => new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            null!,
            new EventSubscriptionManager());

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullSubscriptionManager_ThrowsArgumentNullException()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = new Mock<IServiceProvider>();

        // Act
        Action act = () => new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ProduceEventAsync Tests

    [Fact]
    public async Task ProduceEventAsync_CallsMediatorServiceSend()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };

        // Act
        await producer.ProduceEventAsync(@event);

        // Assert
        mockMediatorService.Verify(
            x => x.Send(@event, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_PassesEventToMediatorService()
    {
        // Arrange
        TestSerializableEvent? capturedEvent = null;
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestSerializableEvent, CancellationToken>((e, ct) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var eventId = Guid.NewGuid();
        var @event = new TestSerializableEvent { EventId = eventId, Message = "TestMessage" };

        // Act
        await producer.ProduceEventAsync(@event);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EventId.Should().Be(eventId);
        capturedEvent.Message.Should().Be("TestMessage");
    }

    [Fact]
    public async Task ProduceEventAsync_PassesCancellationTokenToMediatorService()
    {
        // Arrange
        CancellationToken capturedToken = default;
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestSerializableEvent, CancellationToken>((e, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };
        using var cts = new CancellationTokenSource();

        // Act
        await producer.ProduceEventAsync(@event, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task ProduceEventAsync_CreatesServiceScope()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceScope = new Mock<IServiceScope>();
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        mockServiceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(mockServiceScope.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider.Object,
            new EventSubscriptionManager());

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };

        // Act
        await producer.ProduceEventAsync(@event);

        // Assert
        mockServiceScopeFactory.Verify(x => x.CreateScope(), Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_LogsInformation()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        mockLogger.Setup(x => x.IsEnabled(LogLevel.Information)).Returns(true);

        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };

        // Act
        await producer.ProduceEventAsync(@event);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("sending event")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProduceEventAsync_WithNullEvent_ThrowsException()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        // Act
        Func<Task> act = async () => await producer.ProduceEventAsync<TestSerializableEvent>(null!);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ProduceEventAsync_WhenMediatorServiceThrows_ThrowsEventProductionException()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Mediator error"));

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };

        // Act
        Func<Task> act = async () => await producer.ProduceEventAsync(@event);

        // Assert
        await act.Should().ThrowAsync<EventProductionException>();
    }

    [Fact]
    public async Task ProduceEventAsync_WithComplexEvent_PassesCorrectly()
    {
        // Arrange
        ComplexSerializableEvent? capturedEvent = null;
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<ComplexSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ComplexSerializableEvent, CancellationToken>((e, ct) => capturedEvent = e)
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var eventId = Guid.NewGuid();
        var @event = new ComplexSerializableEvent
        {
            EventId = eventId,
            Name = "ComplexTest",
            Timestamp = new DateTime(2024, 1, 15),
            Items = new List<string> { "Item1", "Item2" }
        };

        // Act
        await producer.ProduceEventAsync(@event);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EventId.Should().Be(eventId);
        capturedEvent.Name.Should().Be("ComplexTest");
        capturedEvent.Timestamp.Should().Be(new DateTime(2024, 1, 15));
        capturedEvent.Items.Should().BeEquivalentTo(new[] { "Item1", "Item2" });
    }

    [Fact]
    public async Task ProduceEventAsync_WithDefaultCancellationToken_PassesDefaultToMediatorService()
    {
        // Arrange
        CancellationToken capturedToken = new CancellationToken(true);
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Callback<TestSerializableEvent, CancellationToken>((e, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };

        // Act
        await producer.ProduceEventAsync(@event);

        // Assert
        capturedToken.Should().Be(default(CancellationToken));
    }

    #endregion

    #region Comparison Tests - Send vs Publish

    [Fact]
    public async Task ProduceEventAsync_UsesSendNotPublish()
    {
        // Arrange
        var mockMediatorService = new Mock<IMediatorService>();
        mockMediatorService
            .Setup(x => x.Send(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockMediatorService
            .Setup(x => x.Publish(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockLogger = new Mock<ILogger<SendWithMediatREventProducer>>();
        var mockServiceProvider = CreateMockServiceProviderWithScope();

        var producer = new SendWithMediatREventProducer(
            mockMediatorService.Object,
            mockLogger.Object,
            mockServiceProvider,
            new EventSubscriptionManager());

        var @event = new TestSerializableEvent { EventId = Guid.NewGuid(), Message = "Test" };

        // Act
        await producer.ProduceEventAsync(@event);

        // Assert
        mockMediatorService.Verify(
            x => x.Send(@event, It.IsAny<CancellationToken>()),
            Times.Once);
        mockMediatorService.Verify(
            x => x.Publish(It.IsAny<TestSerializableEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Helper Methods

    private static IServiceProvider CreateMockServiceProviderWithScope()
    {
        var mockServiceScope = new Mock<IServiceScope>();
        var mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        mockServiceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(mockServiceScope.Object);

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(mockServiceScopeFactory.Object);
        return mockServiceProvider.Object;
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
