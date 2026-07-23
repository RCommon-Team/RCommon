using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

/// <summary>
/// Startup service registered unconditionally by <see cref="EFCorePersistenceBuilder"/> that verifies,
/// for each datastore listed in <see cref="IOutboxDataStoreRegistry.Registrations"/>, that the
/// corresponding <see cref="RCommonDbContext"/> has the <see cref="OutboxMessage"/> entity mapped in
/// its EF Core model.
/// </summary>
/// <remarks>
/// <para>
/// A registered outbox datastore whose DbContext model does not include <c>OutboxMessage</c> will
/// silently drop all domain events — they are written to a table that does not exist. This service
/// detects that misconfiguration at startup and throws an <see cref="InvalidOperationException"/> with
/// an actionable message naming the problematic datastore, rather than letting the application start
/// in a broken state.
/// </para>
/// <para>
/// When no outbox is configured (<see cref="IOutboxDataStoreRegistry.Registrations"/> is empty) the
/// service is a no-op, so unconditional registration from the EF Core builder is safe even in
/// applications that have not called <c>AddOutbox</c>.
/// </para>
/// <para>
/// Services are resolved at <see cref="StartAsync"/> time (not at registration time) so this service
/// is ordering-safe with respect to <c>AddOutbox</c> and <c>AddDbContext</c> call order.
/// </para>
/// </remarks>
internal sealed class OutboxSchemaVerificationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public OutboxSchemaVerificationHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a scope so scoped services (e.g. RCommonDbContext) can be resolved.
        using var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var registry = provider.GetService<IOutboxDataStoreRegistry>();
        if (registry is null || registry.Registrations.Count == 0)
        {
            // No outbox configured — nothing to verify.
            return Task.CompletedTask;
        }

        var factory = provider.GetRequiredService<IDataStoreFactory>();

        foreach (var datastoreName in registry.Registrations)
        {
            RCommonDbContext context;

            try
            {
                context = factory.Resolve<RCommonDbContext>(datastoreName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"RCommon outbox is registered for datastore '{datastoreName}', but that datastore " +
                    $"could not be resolved from the DI container. Ensure that AddDbContext<TDbContext>(\"{datastoreName}\", ...) " +
                    $"is called during EF Core persistence configuration.",
                    ex);
            }

            var entityType = context.Model.FindEntityType(typeof(OutboxMessage));
            if (entityType is null)
            {
                throw new InvalidOperationException(
                    $"RCommon outbox is registered for datastore '{datastoreName}', but the OutboxMessage entity " +
                    $"is not mapped in the DbContext model for that datastore. " +
                    $"Ensure that AddDbContext<TDbContext>(\"{datastoreName}\", ...) is called after AddOutbox so the " +
                    $"OutboxMessage auto-mapping can be applied, or include a migration that creates the outbox table.");
            }
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
