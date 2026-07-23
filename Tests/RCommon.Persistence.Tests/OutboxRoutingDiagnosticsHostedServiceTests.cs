using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Covers the outbox-routing footgun on the LOAD-BEARING service. Durable-event persistence rides on
/// <see cref="IEntityEventTracker"/> resolving to <see cref="OutboxEntityEventTracker"/> — NOT on the
/// <see cref="IEventRouter"/> binding (the tracker composes the concrete <see cref="OutboxEventRouter"/>
/// directly). If a later registration binds the in-memory tracker after the outbox producer configured
/// its tracker, durable events fire in-process and are never persisted, silently. The diagnostic
/// surfaces that at startup instead of leaving it silent.
/// </summary>
public class OutboxRoutingDiagnosticsHostedServiceTests
{
    private static (Mock<ILoggerFactory> loggerFactory, Mock<ILogger> logger) CreateLogger()
    {
        var logger = new Mock<ILogger>();
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(logger.Object);
        return (loggerFactory, logger);
    }

    private static void VerifyWarning(Mock<ILogger> logger, Times times)
    {
        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public async Task StartAsync_Warns_When_Outbox_Tracker_Is_Clobbered_By_InMemory_Tracker()
    {
        var services = new ServiceCollection();
        // Outbox binds IEntityEventTracker -> OutboxEntityEventTracker...
        services.AddScoped<IEntityEventTracker, OutboxEntityEventTracker>();
        // ...then a later registration binds the in-memory tracker last (the footgun): durable events
        // would fire in-process and never be persisted.
        services.AddScoped<IEntityEventTracker, InMemoryEntityEventTracker>();

        var (loggerFactory, logger) = CreateLogger();
        var diagnostic = new OutboxRoutingDiagnosticsHostedService(services, loggerFactory.Object);

        await diagnostic.StartAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Once());
    }

    [Fact]
    public async Task StartAsync_Does_Not_Warn_When_Outbox_Tracker_Is_Intact()
    {
        var services = new ServiceCollection();
        // Intact: the last IEntityEventTracker registration is the outbox tracker.
        services.AddScoped<IEntityEventTracker, InMemoryEntityEventTracker>();
        services.AddScoped<IEntityEventTracker, OutboxEntityEventTracker>();

        var (loggerFactory, logger) = CreateLogger();
        var diagnostic = new OutboxRoutingDiagnosticsHostedService(services, loggerFactory.Object);

        await diagnostic.StartAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Never());
    }
}
