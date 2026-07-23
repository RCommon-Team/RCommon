using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// Startup diagnostic registered by <see cref="OutboxPersistenceBuilderExtensions.AddOutboxProducer{TOutboxStore}"/>.
/// Warns when a PRODUCER-ONLY host (the outbox producer is registered but no poller
/// <see cref="OutboxProcessingService"/> is registered in the same container) leaves
/// <see cref="OutboxOptions.ImmediateDispatch"/> at its default (<c>true</c>).
/// </summary>
/// <remarks>
/// <para>
/// On a producer-only host <c>ImmediateDispatch = true</c> is a footgun: the producer dispatches events
/// in-process post-commit and marks the outbox rows processed BEFORE the remote processor host ever relays
/// them, so a subscriber on the processor host never sees them (the poller's <c>ClaimAsync</c> filters on
/// <c>ProcessedAtUtc IS NULL</c>). A producer-only host should set <see cref="OutboxOptions.ImmediateDispatch"/>
/// to <c>false</c> so the rows are left for the processor host's poller.
/// </para>
/// <para>
/// This is registered from the producer path only. A single-host <c>AddOutbox</c> registers both the
/// producer and the poller, so <see cref="ShouldWarn"/> returns <c>false</c> there.
/// </para>
/// </remarks>
internal sealed class ProducerImmediateDispatchDiagnosticsHostedService : IHostedService
{
    private readonly IServiceCollection _services;
    private readonly IOptions<OutboxOptions> _options;
    private readonly ILoggerFactory? _loggerFactory;

    public ProducerImmediateDispatchDiagnosticsHostedService(
        IServiceCollection services,
        IOptions<OutboxOptions> options,
        ILoggerFactory? loggerFactory = null)
    {
        _services = services;
        _options = options;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Decision: warn only when the poller is NOT registered (producer-only) AND immediate dispatch is on.
    /// Factored out as a static so it can be unit-tested without a logger.
    /// </summary>
    internal static bool ShouldWarn(IServiceCollection services, bool immediateDispatch)
    {
        if (!immediateDispatch)
            return false;

        // Producer-only == no OutboxProcessingService hosted service registered in this container.
        var pollerRegistered = services.Any(d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(OutboxProcessingService));

        return !pollerRegistered;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (ShouldWarn(_services, _options.Value.ImmediateDispatch))
        {
            _loggerFactory?.CreateLogger<ProducerImmediateDispatchDiagnosticsHostedService>().LogWarning(
                "RCommon outbox producer is registered without a poller (OutboxProcessingService) on this " +
                "host, but OutboxOptions.ImmediateDispatch is true (the default). On a producer-only host this " +
                "dispatches events in-process and marks the outbox rows processed post-commit BEFORE the " +
                "processor host relays them, so remote subscribers never receive them. Set " +
                "OutboxOptions.ImmediateDispatch = false on this producer-only host so the rows are left for " +
                "the processor host's poller.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
