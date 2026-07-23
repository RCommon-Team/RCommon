using System;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RCommon.MassTransit;
using RCommon.MassTransit.Outbox;

namespace RCommon;

public static class MassTransitOutboxBuilderExtensions
{
    public static IMassTransitEventHandlingBuilder AddOutbox<TDbContext>(
        this IMassTransitEventHandlingBuilder builder,
        Action<IMassTransitOutboxBuilder>? configure = null)
        where TDbContext : DbContext
    {
        builder.AddEntityFrameworkOutbox<TDbContext>(o =>
        {
            var outboxBuilder = new MassTransitOutboxBuilder(o);
            configure?.Invoke(outboxBuilder);
        });
        return builder;
    }

    /// <summary>
    /// Configures MassTransit's native EF Core bus outbox (recipe 2b) bound to an RCommon datastore:
    /// <c>AddEntityFrameworkOutbox&lt;TDbContext&gt;()</c> + <c>UseBusOutbox()</c> with the chosen provider
    /// (spec AC-14). A published/sent message stages an OutboxMessage row during <typeparamref name="TDbContext"/>'s
    /// SaveChanges inside RCommon's UnitOfWork TransactionScope, committing atomically with business state.
    /// </summary>
    /// <remarks>
    /// <para><typeparamref name="TDbContext"/> must be the SAME DbContext registered for the datastore named
    /// via <c>OnDataStore</c>; a startup validation fails loud on a mismatch. The DbContext must map
    /// MassTransit's outbox entities (<c>modelBuilder.AddTransactionalOutboxEntities()</c>).</para>
    /// <para>Generic + explicit provider by necessity: MassTransit's AddEntityFrameworkOutbox is generic
    /// (needs the type at config time) and RCommon cannot infer the provider at config time.</para>
    /// </remarks>
    public static IMassTransitEventHandlingBuilder UseBrokerOutbox<TDbContext>(
        this IMassTransitEventHandlingBuilder builder,
        Action<MassTransitBrokerOutboxOptions> configure)
        where TDbContext : DbContext
    {
        if (configure is null) throw new ArgumentNullException(nameof(configure));

        var opts = new MassTransitBrokerOutboxOptions();
        configure(opts);

        if (string.IsNullOrWhiteSpace(opts.DataStoreName))
            throw new InvalidOperationException(
                "UseBrokerOutbox requires OnDataStore(\"<name>\") to name the RCommon datastore that owns the broker outbox (AC-14).");
        if (opts.Provider == BrokerOutboxProvider.None)
            throw new InvalidOperationException(
                "UseBrokerOutbox requires a provider: call UsePostgres() or UseSqlServer(). RCommon cannot infer the provider from the datastore registration at configuration time.");

        // Record the (datastore -> DbContext type) binding for the startup co-location validation (Task 2).
        builder.Services.Configure<MassTransitBrokerOutboxRegistrationOptions>(
            o => o.Register(opts.DataStoreName!, typeof(TDbContext)));

        // Register MassTransit's native EF Core bus outbox (the proven recipe-2b wiring).
        builder.AddEntityFrameworkOutbox<TDbContext>(o =>
        {
            if (opts.Provider == BrokerOutboxProvider.Postgres) o.UsePostgres();
            else o.UseSqlServer();
            o.UseBusOutbox(opts.BusOutboxConfigure);
        });

        return builder;
    }
}
