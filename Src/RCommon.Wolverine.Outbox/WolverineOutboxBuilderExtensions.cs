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
}
