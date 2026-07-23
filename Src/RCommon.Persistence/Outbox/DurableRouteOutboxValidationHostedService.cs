using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon.EventHandling.Routing;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// Startup service registered by <see cref="OutboxPersistenceBuilderExtensions.AddOutbox{TOutboxStore}"/>
/// that verifies every durable event route names a datastore that has a registered outbox.
/// </summary>
/// <remarks>
/// <para>
/// A durable route created via <c>Publish&lt;T&gt;().UseOutbox("X")</c> or
/// <c>UseRCommonOutbox("X")</c> writes domain events to the outbox table for datastore "X".
/// If <c>db.AddOutbox(o => o.OnDataStore("X"))</c> was never called, no poller will drain
/// that outbox and events will be silently lost. This service detects the misconfiguration at
/// startup and throws a descriptive <see cref="InvalidOperationException"/> instead of letting
/// the application start in a broken state.
/// </para>
/// <para>
/// When no durable routes are configured (<see cref="IEventRoutingRegistry.DurableStoreNames"/>
/// is empty) the service is a no-op.
/// </para>
/// <para>
/// Comparison between route names and registered outbox names is case-insensitive, matching
/// the convention used throughout the outbox subsystem.
/// </para>
/// </remarks>
internal sealed class DurableRouteOutboxValidationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DurableRouteOutboxValidationHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Use a scope so the service is ordering-safe and consistent with the pattern used by
        // OutboxSchemaVerificationHostedService. Both IEventRoutingRegistry and
        // IOutboxDataStoreRegistry are singletons, so CreateScope() is safe here.
        using var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var routingRegistry = provider.GetRequiredService<IEventRoutingRegistry>();
        var outboxRegistry = provider.GetRequiredService<IOutboxDataStoreRegistry>();

        var durableStoreNames = routingRegistry.DurableStoreNames;
        if (durableStoreNames.Count == 0)
        {
            // No durable routes — nothing to validate.
            return Task.CompletedTask;
        }

        // Build a case-insensitive set of registered outbox datastore names.
        var registeredNames = new System.Collections.Generic.HashSet<string>(
            outboxRegistry.Registrations,
            StringComparer.OrdinalIgnoreCase);

        foreach (var storeName in durableStoreNames)
        {
            if (!registeredNames.Contains(storeName))
            {
                throw new InvalidOperationException(
                    $"RCommon event routing has a durable route targeting datastore '{storeName}', " +
                    $"but no outbox is registered for that datastore. " +
                    $"Ensure that 'db.AddOutbox(o => o.OnDataStore(\"{storeName}\"))' (or the equivalent " +
                    $"'UseOutbox(\"{storeName}\")'/'UseRCommonOutbox(\"{storeName}\")') is called during " +
                    $"persistence configuration so that '{storeName}' participates in outbox processing.");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
