using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon.Entities;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// Startup diagnostic registered by <see cref="OutboxPersistenceBuilderExtensions.AddOutbox{TOutboxStore}"/>.
/// Its presence means the transactional outbox is configured, so it checks that outbox routing was not
/// silently overridden by a later registration.
/// </summary>
internal sealed class OutboxRoutingDiagnosticsHostedService : IHostedService
{
    private readonly IServiceCollection _services;
    private readonly ILoggerFactory? _loggerFactory;

    public OutboxRoutingDiagnosticsHostedService(IServiceCollection services, ILoggerFactory? loggerFactory = null)
    {
        _services = services;
        _loggerFactory = loggerFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // DI is last-registration-wins. Durable-event persistence rides on IEntityEventTracker resolving to
        // OutboxEntityEventTracker -- the tracker composes the concrete OutboxEventRouter directly, so the
        // IEventRouter binding is NOT what persists events (checking it produced false positives, since the
        // core ctor pins IEventRouter to the in-memory router unconditionally even on a working outbox host).
        // AddOutbox now binds the tracker authoritatively (Remove-then-Add), so the only way it can be the
        // in-memory tracker at startup is an explicit later override -- which silently defeats the outbox
        // (durable events fire in-process post-commit and are never persisted). Surface that.
        var lastTracker = _services.LastOrDefault(d => d.ServiceType == typeof(IEntityEventTracker));
        if (lastTracker?.ImplementationType == typeof(InMemoryEntityEventTracker))
        {
            _loggerFactory?.CreateLogger<OutboxRoutingDiagnosticsHostedService>().LogWarning(
                "RCommon outbox is configured (AddOutbox) but the effective IEntityEventTracker is the " +
                "in-memory tracker ({Tracker}); a later registration overrode the outbox tracker. Durable " +
                "domain events will be dispatched in-memory and NOT persisted to the outbox. Remove the " +
                "override, or re-assert the outbox tracker last.",
                typeof(InMemoryEntityEventTracker).Name);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
