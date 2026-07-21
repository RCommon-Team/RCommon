using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling.Producers;

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
        // DI is last-registration-wins. AddOutbox binds IEventRouter to the OutboxEventRouter (via a
        // forwarding factory, so its ImplementationType is null). If a later event-handling registration
        // binds the in-memory router, that wins and outbox routing is silently defeated -- events fire
        // in-process post-commit and are never persisted to the outbox.
        var lastRouter = _services.LastOrDefault(d => d.ServiceType == typeof(IEventRouter));
        if (lastRouter?.ImplementationType == typeof(InMemoryTransactionalEventRouter))
        {
            _loggerFactory?.CreateLogger<OutboxRoutingDiagnosticsHostedService>().LogWarning(
                "RCommon outbox is configured (AddOutbox) but the effective IEventRouter is the in-memory " +
                "router ({Router}); a later registration overrode the outbox router. Domain events will be " +
                "dispatched in-memory and NOT persisted to the outbox. Register AddOutbox after any " +
                "event-handling configuration that binds IEventRouter, or re-assert the outbox router last.",
                typeof(InMemoryTransactionalEventRouter).Name);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
