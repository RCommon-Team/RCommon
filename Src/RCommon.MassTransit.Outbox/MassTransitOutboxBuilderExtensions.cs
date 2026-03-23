using MassTransit;
using Microsoft.EntityFrameworkCore;
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
}
