using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling.Producers;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Covers the outbox-routing footgun: because DI is last-registration-wins, an event-handling
/// registration that binds the in-memory <see cref="InMemoryTransactionalEventRouter"/> AFTER
/// <see cref="OutboxPersistenceBuilderExtensions.AddOutbox{TOutboxStore}"/> silently overrides the
/// outbox router. When that happens, domain events fire in-memory and are never persisted to the
/// outbox. The diagnostic surfaces the misconfiguration at startup instead of leaving it silent.
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
    public async Task StartAsync_Warns_When_Outbox_Router_Is_Clobbered_By_InMemory_Router()
    {
        var services = new ServiceCollection();
        // Outbox registers IEventRouter via a factory forwarding to OutboxEventRouter...
        services.AddScoped<IEventRouter>(_ => throw new InvalidOperationException("should not resolve"));
        // ...then a later event-handling registration binds the in-memory router last (the footgun).
        services.AddScoped<IEventRouter, InMemoryTransactionalEventRouter>();

        var (loggerFactory, logger) = CreateLogger();
        var diagnostic = new OutboxRoutingDiagnosticsHostedService(services, loggerFactory.Object);

        await diagnostic.StartAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Once());
    }

    [Fact]
    public async Task StartAsync_Does_Not_Warn_When_Outbox_Router_Is_Intact()
    {
        var services = new ServiceCollection();
        // Intact: the last IEventRouter registration is the outbox factory (not the in-memory router).
        services.AddScoped<IEventRouter>(_ => throw new InvalidOperationException("should not resolve"));

        var (loggerFactory, logger) = CreateLogger();
        var diagnostic = new OutboxRoutingDiagnosticsHostedService(services, loggerFactory.Object);

        await diagnostic.StartAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Never());
    }
}
