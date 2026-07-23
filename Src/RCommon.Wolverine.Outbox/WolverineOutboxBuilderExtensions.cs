using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using RCommon.Wolverine;
using RCommon.Wolverine.Outbox;

namespace RCommon;

public static class WolverineOutboxBuilderExtensions
{
    public static IWolverineEventHandlingBuilder AddOutbox(
        this IWolverineEventHandlingBuilder builder,
        Action<IWolverineOutboxBuilder>? configure = null)
    {
        builder.Services.ConfigureWolverine(opts =>
        {
            var outboxBuilder = new WolverineOutboxBuilder(opts);
            configure?.Invoke(outboxBuilder);
        });
        return builder;
    }

    /// <summary>
    /// NOT SUPPORTED for Wolverine. WolverineFx's native EF Core outbox writes its envelope on its OWN
    /// DbTransaction, which suppresses ambient System.Transactions enlistment, so it cannot stage atomically
    /// inside RCommon's UnitOfWork TransactionScope (verified in the Phase-0 coordination spike). Use
    /// <c>UseRCommonOutbox("&lt;datastore&gt;")</c> (recipe 2a) instead: RCommon's own per-datastore outbox writes
    /// the row atomically and a processor relays it via Wolverine.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public static IWolverineEventHandlingBuilder UseBrokerOutbox<TDbContext>(
        this IWolverineEventHandlingBuilder builder,
        Action<WolverineBrokerOutboxOptions> configure)
        where TDbContext : DbContext
        => throw new NotSupportedException(
            "UseBrokerOutbox is not supported for Wolverine: its native EF Core outbox commits its envelope on its " +
            "own DbTransaction, which cannot enlist in RCommon's UnitOfWork TransactionScope (recipe 2b NO-GO, " +
            "verified by the Phase-0 coordination spike). Use UseRCommonOutbox(\"<datastore>\") instead (recipe 2a): " +
            "RCommon's per-datastore outbox stages the row atomically and a processor relays it via Wolverine.");
}
