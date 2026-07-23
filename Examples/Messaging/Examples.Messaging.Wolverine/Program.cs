using Examples.Messaging.Wolverine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Transactions;
using RCommon.Wolverine;
using Wolverine;

// ---------------------------------------------------------------------------------------------------
// Recipe 2a (Wolverine): DDD + UnitOfWork + broker AS A PRODUCER behind RCommon's per-datastore outbox.
//
// Recipe 2a is the SUPPORTED broker-as-producer path for Wolverine. Wolverine's native broker outbox
// (recipe 2b) is NO-GO by design (UseBrokerOutbox<T> throws NotSupportedException), so Wolverine users
// stage durable events in RCommon's OWN __OutboxMessages table instead.
//
// A domain event raised by an aggregate (OrderConfirmed) is declared durable via
//   e.Publish<OrderConfirmed>() + e.UseRCommonOutbox("Orders")
// so committing the UnitOfWork stages it into RCommon's own __OutboxMessages table ATOMICALLY with the
// business Order row (inside the ambient TransactionScope). A background poller later relays that staged
// row to the Wolverine producer (PublishWithWolverineEventProducer -> IMessageBus) post-commit — the
// broker is never touched inside the business transaction. See Examples.Messaging.Wolverine.Tests for
// the e2e proof (AC-16).
//
// Wolverine host wrinkle: Wolverine is wired at the HOST level via IHostBuilder.UseWolverine(...), which
// registers IMessageBus for the RCommon Wolverine producer. RCommon is then added inside ConfigureServices.
//
// NOTE: this example wires recipe 2a against Postgres and therefore needs a reachable database to RUN.
// The wiring is the point; the deterministic proof lives in the integration test. The connection string
// comes from configuration ("OrdersConnection"); if absent a local default is used.
// ---------------------------------------------------------------------------------------------------
try
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, builder) =>
        {
            ConfigurationContainer.Configuration = builder.Build();
        })
        .UseWolverine(options =>
        {
            // In-process local queue keeps the example self-contained; swap for RabbitMq/etc. in production.
            options.LocalQueue("orders");
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
                    // relays each staged event to the Wolverine producer configured below.
                    ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Orders");
                })
                .WithEventHandling<WolverineEventHandlingBuilder>(e =>
                {
                    // Builder-level default: route published events to RCommon's "Orders" outbox (durable).
                    e.UseRCommonOutbox("Orders");
                    // Declare OrderConfirmed as published via Wolverine (broker as producer).
                    e.Publish<OrderConfirmed>();
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
