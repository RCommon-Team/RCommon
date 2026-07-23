using Examples.Messaging.MassTransit;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.MassTransit;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Transactions;

// ---------------------------------------------------------------------------------------------------
// Recipe 2a (MassTransit): DDD + UnitOfWork + broker AS A PRODUCER behind RCommon's per-datastore outbox.
//
// A domain event raised by an aggregate (OrderConfirmed) is declared durable via
//   e.Publish<OrderConfirmed>() + e.UseRCommonOutbox("Orders")
// so committing the UnitOfWork stages it into RCommon's own __OutboxMessages table ATOMICALLY with the
// business Order row (inside the ambient TransactionScope). A background poller later relays that staged
// row to the MassTransit producer (IBus.Publish) post-commit — the broker is never touched inside the
// business transaction. See Examples.Messaging.MassTransit.Tests for the e2e proof (AC-16).
//
// NOTE: this example wires recipe 2a against Postgres and therefore needs a reachable database to RUN.
// The wiring is the point; the deterministic proof lives in the integration test. The connection string
// comes from configuration ("OrdersConnection"); if absent the example prints guidance and exits cleanly.
// ---------------------------------------------------------------------------------------------------
try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, builder) =>
        {
            ConfigurationContainer.Configuration = builder.Build();
        })
        .ConfigureServices((context, services) =>
        {
            var connectionString =
                context.Configuration.GetConnectionString("OrdersConnection")
                ?? "Host=localhost;Port=5432;Database=orders;Username=postgres;Password=postgres";

            services.AddRCommon()
                .WithSimpleGuidGenerator()
                .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
                .WithPersistence<EFCorePersistenceBuilder>(ef =>
                {
                    ef.AddDbContext<AppDbContext>("Orders", o => o.UseNpgsql(connectionString));
                    ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");
                    // Register "Orders" as an RCommon outbox owner. A poller drains it post-commit and
                    // relays each staged event to the MassTransit producer configured below.
                    ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Orders");
                })
                .WithEventHandling<MassTransitEventHandlingBuilder>(e =>
                {
                    // Builder-level default: route published events to RCommon's "Orders" outbox (durable).
                    e.UseRCommonOutbox("Orders");
                    // Declare OrderConfirmed as published via MassTransit (broker as producer).
                    e.Publish<OrderConfirmed>();
                    // In-memory transport keeps the example self-contained; swap for RabbitMq in production.
                    e.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
                });

            services.AddHostedService<Worker>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
