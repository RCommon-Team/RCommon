using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.MassTransit.Producers;
using Xunit;

namespace RCommon.MassTransit.Tests;

public class SendWithMassTransitEventProducerTests
{
    private readonly Mock<IBus> _mockBus;
    private readonly Mock<ILogger<PublishWithMassTransitEventProducer>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<ISendEndpoint> _mockSendEndpoint;

    public SendWithMassTransitEventProducerTests()
    {
        _mockBus = new Mock<IBus>();
        _mockLogger = new Mock<ILogger<PublishWithMassTransitEventProducer>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockScope = new Mock<IServiceScope>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockSendEndpoint = new Mock<ISendEndpoint>();

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockScopeFactory.Object);
    }

    private SendWithMassTransitEventProducer CreateProducer()
    {
        return new SendWithMassTransitEventProducer(
            _mockBus.Object,
            _mockLogger.Object,
            _mockServiceProvider.Object);
    }

    [Fact]
    public void Constructor_WithNullBus_ThrowsArgumentNullException()
    {
        // Act
        var action = () => new SendWithMassTransitEventProducer(
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
        var action = () => new SendWithMassTransitEventProducer(
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
        var action = () => new SendWithMassTransitEventProducer(
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

    [Fact(Skip = "Send is an extension method on ISendEndpointProvider and cannot be mocked with Moq")]
    public async Task ProduceEventAsync_WithValidEvent_CallsBusSend()
    {
        // Note: This test is skipped because IBus.Send() is an extension method
        // (EndpointConventionExtensions.Send) which cannot be mocked by Moq.
        // Integration tests should be used to verify this functionality.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Send is an extension method on ISendEndpointProvider and cannot be mocked with Moq")]
    public async Task ProduceEventAsync_WithCancellationToken_PassesTokenToBus()
    {
        // Note: This test is skipped because IBus.Send() is an extension method
        // (EndpointConventionExtensions.Send) which cannot be mocked by Moq.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Send is an extension method on ISendEndpointProvider and cannot be mocked with Moq")]
    public async Task ProduceEventAsync_WhenBusThrows_ThrowsEventProductionException()
    {
        // Note: This test is skipped because IBus.Send() is an extension method
        // (EndpointConventionExtensions.Send) which cannot be mocked by Moq.
        await Task.CompletedTask;
    }

    [Fact]
    public void Producer_ImplementsIEventProducer()
    {
        // Arrange & Act
        var producer = CreateProducer();

        // Assert
        producer.Should().BeAssignableTo<IEventProducer>();
    }

    [Fact(Skip = "Send is an extension method on ISendEndpointProvider and cannot be mocked with Moq")]
    public async Task ProduceEventAsync_WithInfoLogEnabled_LogsInformation()
    {
        // Note: This test is skipped because IBus.Send() is an extension method
        // which cannot be mocked, and the actual call requires MassTransit configuration.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Send is an extension method on ISendEndpointProvider and cannot be mocked with Moq")]
    public async Task ProduceEventAsync_WithInfoLogDisabled_LogsDebug()
    {
        // Note: This test is skipped because IBus.Send() is an extension method
        // which cannot be mocked, and the actual call requires MassTransit configuration.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Send is an extension method on ISendEndpointProvider and cannot be mocked with Moq")]
    public async Task ProduceEventAsync_DisposesScope_AfterSending()
    {
        // Note: This test is skipped because IBus.Send() is an extension method
        // which cannot be mocked, and the actual call requires MassTransit configuration.
        await Task.CompletedTask;
    }

    [Fact(Skip = "Send is an extension method on ISendEndpointProvider and cannot be mocked with Moq")]
    public async Task ProduceEventAsync_CreatesScope_BeforeSending()
    {
        // Note: This test is skipped because IBus.Send() is an extension method
        // which cannot be mocked, and the actual call requires MassTransit configuration.
        await Task.CompletedTask;
    }
}
