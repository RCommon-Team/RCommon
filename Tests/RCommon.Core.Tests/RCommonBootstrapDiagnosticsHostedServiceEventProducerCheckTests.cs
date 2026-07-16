using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using Xunit;

namespace RCommon.Core.Tests;

/// <summary>
/// Covers the cross-builder startup safety net from
/// docs/specs/event-handling/producer-auto-registration.md: any IEventHandlingBuilder
/// implementation (not just InMemoryEventBusBuilder) with a recorded subscription but zero
/// recorded producers gets a single LogWarning naming that builder type.
/// </summary>
public class RCommonBootstrapDiagnosticsHostedServiceEventProducerCheckTests
{
    private static (ServiceCollection services, RCommonBuilder builder, Mock<ILoggerFactory> loggerFactory, Mock<ILogger> logger)
        CreateHarness()
    {
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        var mockLogger = new Mock<ILogger>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(mockLogger.Object);

        return (services, builder, mockLoggerFactory, mockLogger);
    }

    public class FakeBuilderWithNoProducer : IEventHandlingBuilder
    {
        public FakeBuilderWithNoProducer(IRCommonBuilder builder) => Services = builder.Services;
        public IServiceCollection Services { get; }
    }

    public class FakeProducer : IEventProducer
    {
        public System.Threading.Tasks.Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : RCommon.Models.Events.ISerializableEvent => System.Threading.Tasks.Task.CompletedTask;
    }

    public class FakeEvent : RCommon.Models.Events.ISyncEvent
    {
    }

    public class FakeEventHandler : RCommon.EventHandling.Subscribers.ISubscriber<FakeEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(FakeEvent @event, CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }

    [Fact]
    public async System.Threading.Tasks.Task StartAsync_BuilderWithSubscriptionAndNoProducer_LogsWarningNamingBuilderType()
    {
        // Arrange -- a custom IEventHandlingBuilder that records a subscription via AddSubscriber-style
        // manual calls but never registers a producer (the general, cross-builder version of the bug).
        var (services, builder, mockLoggerFactory, mockLogger) = CreateHarness();
        builder.WithEventHandling<FakeBuilderWithNoProducer>(eh =>
        {
            eh.Services.AddScoped<RCommon.EventHandling.Subscribers.ISubscriber<FakeEvent>, FakeEventHandler>();
            var subscriptionManager = eh.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(eh.GetType(), typeof(FakeEvent));
        });

        var hostedService = new RCommonBootstrapDiagnosticsHostedService(services, builder, mockLoggerFactory.Object);

        // Act
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((state, t) => state.ToString()!.Contains(nameof(FakeBuilderWithNoProducer))),
            null,
            It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async System.Threading.Tasks.Task StartAsync_BuilderWithSubscriptionAndProducer_LogsNoWarning()
    {
        // Arrange -- regression guard: a builder with both a subscription and a matching producer
        // must not be warned about.
        var (services, builder, mockLoggerFactory, mockLogger) = CreateHarness();
        builder.WithEventHandling<FakeBuilderWithNoProducer>(eh =>
        {
            eh.AddProducer<FakeProducer>();
            eh.Services.AddScoped<RCommon.EventHandling.Subscribers.ISubscriber<FakeEvent>, FakeEventHandler>();
            var subscriptionManager = eh.Services.GetSubscriptionManager();
            subscriptionManager?.AddSubscription(eh.GetType(), typeof(FakeEvent));
        });

        var hostedService = new RCommonBootstrapDiagnosticsHostedService(services, builder, mockLoggerFactory.Object);

        // Act
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task StartAsync_InMemoryEventBusBuilder_AddSubscriberAlone_LogsNoWarning()
    {
        // Arrange -- InMemoryEventBusBuilder.AddSubscriber now auto-registers its own producer, so
        // the cross-builder check must not flag it even with no explicit AddProducer call.
        var (services, builder, mockLoggerFactory, mockLogger) = CreateHarness();
        builder.WithEventHandling<InMemoryEventBusBuilder>(eh =>
        {
            eh.AddSubscriber<FakeEvent, FakeEventHandler>();
        });

        var hostedService = new RCommonBootstrapDiagnosticsHostedService(services, builder, mockLoggerFactory.Object);

        // Act
        await hostedService.StartAsync(CancellationToken.None);

        // Assert
        mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<System.Func<It.IsAnyType, System.Exception?, string>>()),
            Times.Never);
    }
}
