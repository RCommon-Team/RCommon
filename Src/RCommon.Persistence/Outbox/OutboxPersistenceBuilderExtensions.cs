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
    /// Registers BOTH the outbox producer (store, routers, tracker) and the processor (hosted poller)
    /// for a datastore — the single-host topology. Equivalent to calling
    /// <see cref="AddOutboxProducer{TOutboxStore}"/> then <see cref="AddOutboxProcessor{TOutboxStore}"/>.
    /// </summary>
    /// <remarks>
    /// All registrations are idempotent (<c>Try*</c>/<c>TryAddEnumerable</c>) and datastore names
    /// deduplicate case-insensitively, so composing producer + processor (which each register the
    /// shared core) is safe and yields exactly one poller and one datastore registration.
    /// <para>
    /// The <paramref name="configure"/> delegate may be invoked more than once (once eagerly to resolve
    /// the owning datastore name via <see cref="OutboxOptions.OnDataStore"/>, and again by the options
    /// system), so it must be side-effect-free.
    /// </para>
    /// </remarks>
    public static IPersistenceBuilder AddOutbox<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null,
        string? dataStoreName = null)
        where TOutboxStore : class, IOutboxStore
    {
        builder.AddOutboxProducer<TOutboxStore>(configure, dataStoreName);
        builder.AddOutboxProcessor<TOutboxStore>(configure, dataStoreName);
        return builder;
    }

    /// <summary>
    /// Registers the outbox PRODUCER for a datastore: the store, serializer, event routers, entity-event
    /// tracker, backoff strategy, datastore registry, and startup diagnostics — but NOT the hosted poller
    /// (<see cref="OutboxProcessingService"/>). Use on a producer-only host in a producer/processor
    /// topology (AC-21).
    /// </summary>
    /// <remarks>
    /// A producer-only host (one that writes outbox rows but does not run the poller) should set
    /// <see cref="OutboxOptions.ImmediateDispatch"/> to <c>false</c> — otherwise an immediate producer-side
    /// dispatch marks the row processed before a remote subscriber (on the processor host) ever sees it.
    /// </remarks>
    public static IPersistenceBuilder AddOutboxProducer<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null,
        string? dataStoreName = null)
        where TOutboxStore : class, IOutboxStore
    {
        // Routing (tracker + routers) lives in AddOutboxCore so that EVERY outbox host — producer,
        // processor, or the combined AddOutbox — persists durable events. See AddOutboxCore for why.
        builder.AddOutboxCore<TOutboxStore>(configure, dataStoreName);

        // Startup diagnostic: warn if a later registration silently overrode the outbox tracker.
        // Producer-only: a processor-only host legitimately never commits domain entities, so warning
        // there would be noise; the shared MN-3 durable-route validator (in the core) covers both hosts.
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, OutboxRoutingDiagnosticsHostedService>(
                sp => new OutboxRoutingDiagnosticsHostedService(
                    builder.Services,
                    sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>())));

        // Startup diagnostic (AC-21 footgun): warn if this is a producer-only host (no poller registered)
        // that leaves ImmediateDispatch = true. Registered on the producer path only; a composed AddOutbox
        // also registers the poller, so ShouldWarn short-circuits to false there.
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, ProducerImmediateDispatchDiagnosticsHostedService>(
                sp => new ProducerImmediateDispatchDiagnosticsHostedService(
                    builder.Services,
                    sp.GetRequiredService<IOptions<OutboxOptions>>(),
                    sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>())));

        return builder;
    }

    /// <summary>
    /// Registers the outbox PROCESSOR for a datastore: the shared core plus the hosted poller
    /// (<see cref="OutboxProcessingService"/>) that claims, dispatches, and marks outbox rows. Use on a
    /// processor host in a producer/processor topology (AC-21).
    /// </summary>
    public static IPersistenceBuilder AddOutboxProcessor<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null,
        string? dataStoreName = null)
        where TOutboxStore : class, IOutboxStore
    {
        builder.AddOutboxCore<TOutboxStore>(configure, dataStoreName);

        // Background processing service (singleton). AddHostedService uses TryAddEnumerable keyed on the
        // implementation type, so calling this (or AddOutbox) more than once still yields exactly ONE
        // OutboxProcessingService, which drains every registered datastore.
        builder.Services.AddHostedService<OutboxProcessingService>();

        return builder;
    }

    /// <summary>
    /// Registers the shared, idempotent outbox core used by both the producer and the processor:
    /// store, serializer, options, backoff strategy, datastore registry, and datastore-name enqueue.
    /// </summary>
    private static IPersistenceBuilder AddOutboxCore<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure,
        string? dataStoreName)
        where TOutboxStore : class, IOutboxStore
    {
        // Outbox store (scoped — participates in per-request transaction).
        builder.Services.TryAddScoped<IOutboxStore, TOutboxStore>();

        // Outbox routing — registered in the CORE (shared by producer, processor, and the combined
        // AddOutbox) so ANY host that both runs the outbox and commits domain entities persists durable
        // events. A pure poller host that never commits entities simply never constructs the tracker.
        //
        // Concrete collaborators the OutboxEntityEventTracker composes directly. Safe to TryAdd: they are
        // the outbox's own types, so nothing else registers them. Registering all three together (not just
        // the interface binding) is what prevents the "unable to resolve InMemoryEntityEventTracker" DI
        // failure when the outbox tracker is bound (report #16). The InMemoryTransactionalEventRouter is the
        // transient dispatcher for the Phase-2 FIFO drain, independent of whatever IEventRouter resolves to.
        builder.Services.TryAddScoped<OutboxEventRouter>();
        builder.Services.TryAddScoped<InMemoryTransactionalEventRouter>();
        builder.Services.TryAddScoped<InMemoryEntityEventTracker>();

        // Authoritative outbox routing. These MUST win regardless of the order in which WithPersistence /
        // WithEventHandling / AddRCommon ran, so they are registered with Remove-then-Add rather than TryAdd.
        //
        // Why TryAdd is wrong here (3.2.0 defect #15 — silent outbox data loss):
        //   * WithEventTracking runs at the END of EVERY WithPersistence<T> call and TryAdds the in-memory
        //     IEntityEventTracker. Under modular / multi-datastore composition a non-outbox WithPersistence
        //     can run first, pinning the in-memory tracker; a later AddOutbox TryAdd then no-ops and every
        //     durable event is silently dispatched in-process and never written to the outbox.
        //   * RCommonBuilder's constructor registers IEventRouter -> InMemoryTransactionalEventRouter with an
        //     unconditional AddScoped, so an outbox IEventRouter forwarder registered with TryAdd could never
        //     win in ANY configuration.
        // OutboxEntityEventTracker is a strict superset of the in-memory tracker (durable events -> outbox,
        // transient events -> in-process), so it is always correct for it to win. Remove-then-Add is
        // idempotent across repeated AddOutbox calls (it nets exactly one registration), and a subsequent
        // non-outbox WithPersistence TryAdd will no-op because the outbox registration is already present.
        builder.Services.RemoveAll<IEventRouter>();
        builder.Services.AddScoped<IEventRouter>(sp => sp.GetRequiredService<OutboxEventRouter>());

        builder.Services.RemoveAll<IEntityEventTracker>();
        builder.Services.AddScoped<IEntityEventTracker, OutboxEntityEventTracker>();

        // Serializer (singleton, replaceable).
        builder.Services.TryAddSingleton<IOutboxSerializer, JsonOutboxSerializer>();

        // Options.
        if (configure != null)
            builder.Services.Configure(configure);
        else
            builder.Services.Configure<OutboxOptions>(_ => { });

        // Backoff strategy (singleton, replaceable).
        builder.Services.TryAddSingleton<IBackoffStrategy>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OutboxOptions>>().Value;
            return new ExponentialBackoffStrategy(opts.BackoffBaseDelay, opts.BackoffMaxDelay, opts.BackoffMultiplier);
        });

        // Datastore registry — singleton, ordering-safe. Resolve the effective name: an explicit positional
        // dataStoreName wins; otherwise a name set via OnDataStore(...) inside the configure delegate; else
        // null (= "use the configured default datastore", resolved lazily by the registry at read time).
        builder.Services.TryAddSingleton<IOutboxDataStoreRegistry, OutboxDataStoreRegistry>();
        var effectiveName = ResolveDataStoreName(configure, dataStoreName);
        builder.Services.Configure<OutboxDataStoreRegistrationOptions>(o => o.Names.Add(effectiveName));

        // Startup diagnostic (MN-3 fail-loud): throw when a durable event route names a datastore that has
        // no registered outbox. In the CORE so producer-only, processor-only, and composed AddOutbox hosts
        // all keep the guard. TryAddEnumerable keyed on impl type => exactly one instance even when the core
        // runs twice (composed AddOutbox) or across multiple datastore registrations.
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, DurableRouteOutboxValidationHostedService>(
                sp => new DurableRouteOutboxValidationHostedService(sp)));

        return builder;
    }

    /// <summary>
    /// Resolves the owning datastore name. An explicit positional <paramref name="dataStoreName"/> takes
    /// precedence; otherwise the <c>configure</c> delegate is probed against a throwaway
    /// <see cref="OutboxOptions"/> to read any <see cref="OutboxOptions.DataStoreName"/> set via
    /// <see cref="OutboxOptions.OnDataStore"/>. Returns <c>null</c> when neither is supplied.
    /// </summary>
    private static string? ResolveDataStoreName(Action<OutboxOptions>? configure, string? dataStoreName)
    {
        if (!string.IsNullOrWhiteSpace(dataStoreName))
            return dataStoreName;

        if (configure == null)
            return null;

        var probe = new OutboxOptions();
        configure(probe);
        return probe.DataStoreName;
    }
}
