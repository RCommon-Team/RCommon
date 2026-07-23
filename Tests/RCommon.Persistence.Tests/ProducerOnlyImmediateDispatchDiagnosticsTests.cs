using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Covers the producer-only footgun (AC-21): a host that registers the outbox PRODUCER but not the
/// poller (<see cref="OutboxProcessingService"/>) yet leaves <see cref="OutboxOptions.ImmediateDispatch"/>
/// at its default (<c>true</c>) will immediately dispatch and mark rows processed post-commit on the
/// producer host — before the processor host ever relays them. The diagnostic warns at startup.
/// </summary>
public class ProducerOnlyImmediateDispatchDiagnosticsTests
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

    // ---- Decision method (unit) -------------------------------------------------

    [Fact]
    public void ShouldWarn_True_When_ProducerOnly_And_ImmediateDispatch_True()
    {
        var services = new ServiceCollection(); // no OutboxProcessingService registered
        ProducerImmediateDispatchDiagnosticsHostedService
            .ShouldWarn(services, immediateDispatch: true)
            .Should().BeTrue();
    }

    [Fact]
    public void ShouldWarn_False_When_Poller_Is_Registered()
    {
        var services = new ServiceCollection();
        services.AddHostedService<OutboxProcessingService>();

        ProducerImmediateDispatchDiagnosticsHostedService
            .ShouldWarn(services, immediateDispatch: true)
            .Should().BeFalse();
    }

    [Fact]
    public void ShouldWarn_False_When_ProducerOnly_But_ImmediateDispatch_False()
    {
        var services = new ServiceCollection();
        ProducerImmediateDispatchDiagnosticsHostedService
            .ShouldWarn(services, immediateDispatch: false)
            .Should().BeFalse();
    }

    // ---- Hosted service (integration of decision + logging) ---------------------

    [Fact]
    public async Task StartAsync_Warns_When_ProducerOnly_And_ImmediateDispatch_True()
    {
        var services = new ServiceCollection(); // producer-only: no poller
        var (loggerFactory, logger) = CreateLogger();
        var diagnostic = new ProducerImmediateDispatchDiagnosticsHostedService(
            services,
            Options.Create(new OutboxOptions { ImmediateDispatch = true }),
            loggerFactory.Object);

        await diagnostic.StartAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Once());
    }

    [Fact]
    public async Task StartAsync_Does_Not_Warn_When_Poller_Registered()
    {
        var services = new ServiceCollection();
        services.AddHostedService<OutboxProcessingService>();

        var (loggerFactory, logger) = CreateLogger();
        var diagnostic = new ProducerImmediateDispatchDiagnosticsHostedService(
            services,
            Options.Create(new OutboxOptions { ImmediateDispatch = true }),
            loggerFactory.Object);

        await diagnostic.StartAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Never());
    }

    [Fact]
    public async Task StartAsync_Does_Not_Warn_When_ImmediateDispatch_False()
    {
        var services = new ServiceCollection(); // producer-only

        var (loggerFactory, logger) = CreateLogger();
        var diagnostic = new ProducerImmediateDispatchDiagnosticsHostedService(
            services,
            Options.Create(new OutboxOptions { ImmediateDispatch = false }),
            loggerFactory.Object);

        await diagnostic.StartAsync(CancellationToken.None);

        VerifyWarning(logger, Times.Never());
    }
}
