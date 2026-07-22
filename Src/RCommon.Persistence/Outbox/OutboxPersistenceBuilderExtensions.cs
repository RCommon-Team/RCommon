using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Persistence.Outbox;

namespace RCommon;

public static class OutboxPersistenceBuilderExtensions
{
    /// <summary>
    /// Registers the transactional outbox pattern services into the DI container.
    /// </summary>
    /// <typeparam name="TOutboxStore">The <see cref="IOutboxStore"/> implementation to register (scoped).</typeparam>
    /// <param name="builder">The persistence builder to extend.</param>
    /// <param name="configure">Optional action to configure <see cref="OutboxOptions"/>.</param>
    /// <param name="dataStoreName">
    /// The name of the datastore that owns the outbox table. When omitted (or null), the default
    /// datastore name (configured via <c>SetDefaultDataStore</c>) is used and resolved lazily at
    /// runtime — so this can be called before <c>SetDefaultDataStore</c> without issue.
    /// </param>
    /// <returns>The <see cref="IPersistenceBuilder"/> for fluent chaining.</returns>
    /// <remarks>
    /// Registration details:
    /// <list type="bullet">
    ///   <item><description><see cref="IOutboxStore"/> — scoped (<typeparamref name="TOutboxStore"/>)</description></item>
    ///   <item><description><see cref="IOutboxSerializer"/> — singleton (<see cref="JsonOutboxSerializer"/>, replaceable via TryAddSingleton)</description></item>
    ///   <item><description><see cref="OutboxEventRouter"/> — scoped (concrete registration)</description></item>
    ///   <item><description><see cref="IEventRouter"/> — scoped (forwards to <see cref="OutboxEventRouter"/>)</description></item>
    ///   <item><description><see cref="InMemoryEntityEventTracker"/> — scoped (required by <see cref="OutboxEntityEventTracker"/>)</description></item>
    ///   <item><description><see cref="IEntityEventTracker"/> — scoped (<see cref="OutboxEntityEventTracker"/>)</description></item>
    ///   <item><description><see cref="OutboxProcessingService"/> — hosted service (singleton)</description></item>
    ///   <item><description><see cref="IOutboxDataStoreRegistry"/> — singleton (<see cref="OutboxDataStoreRegistry"/>)</description></item>
    /// </list>
    /// </remarks>
    public static IPersistenceBuilder AddOutbox<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null,
        string? dataStoreName = null)
        where TOutboxStore : class, IOutboxStore
    {
        // Outbox store (scoped — participates in per-request transaction)
        builder.Services.AddScoped<IOutboxStore, TOutboxStore>();

        // Serializer (singleton, replaceable)
        builder.Services.TryAddSingleton<IOutboxSerializer, JsonOutboxSerializer>();

        // Outbox event router (scoped — replaces InMemoryTransactionalEventRouter)
        builder.Services.AddScoped<OutboxEventRouter>();
        builder.Services.AddScoped<IEventRouter>(sp => sp.GetRequiredService<OutboxEventRouter>());

        // Entity event tracker decorator (scoped — replaces InMemoryEntityEventTracker)
        builder.Services.AddScoped<InMemoryEntityEventTracker>();
        builder.Services.AddScoped<IEntityEventTracker, OutboxEntityEventTracker>();

        // Background processing service (singleton)
        builder.Services.AddHostedService<OutboxProcessingService>();

        // Startup diagnostic: warn if a later registration silently overrode the outbox IEventRouter.
        builder.Services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sp =>
            new OutboxRoutingDiagnosticsHostedService(
                builder.Services,
                sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>()));

        // Options
        if (configure != null)
        {
            builder.Services.Configure(configure);
        }
        else
        {
            builder.Services.Configure<OutboxOptions>(_ => { });
        }

        // Backoff strategy (singleton, replaceable)
        builder.Services.TryAddSingleton<IBackoffStrategy>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OutboxOptions>>().Value;
            return new ExponentialBackoffStrategy(opts.BackoffBaseDelay, opts.BackoffMaxDelay, opts.BackoffMultiplier);
        });

        // Datastore registry — singleton, ordering-safe.
        // The name (or null for "use the default") is enqueued into OutboxDataStoreRegistrationOptions
        // now (registration time) and resolved lazily when Registrations is first read (runtime).
        // This means AddOutbox can be called before SetDefaultDataStore without issue.
        builder.Services.TryAddSingleton<IOutboxDataStoreRegistry, OutboxDataStoreRegistry>();
        builder.Services.Configure<OutboxDataStoreRegistrationOptions>(o => o.Names.Add(dataStoreName));

        return builder;
    }
}
