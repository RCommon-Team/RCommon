using Examples.Messaging.MassTransit.NativeOutbox;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.MassTransit;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Transactions;

// ---------------------------------------------------------------------------------------------------
// Recipe 2b (MassTransit ONLY): DDD + UnitOfWork + broker-NATIVE outbox, RCommon-wrapped.
//
// MassTransit ships its own EF Core "bus outbox". RCommon's PUBLIC verb UseBrokerOutbox<TDbContext> wraps
// AddEntityFrameworkOutbox<TDbContext>() + UseBusOutbox() so a published integration event stages a
// MassTransit OutboxMessage row during the DbContext's SaveChanges — which enlists in RCommon's ambient
// UnitOfWork TransactionScope, committing (or rolling back) ATOMICALLY with the business state.
//
// This is the runnable companion to the AC-15 gate Tests/RCommon.IntegrationTests/RecipeTwoBBrokerOutboxTests
// and to the e2e integration test in Examples.Messaging.MassTransit.NativeOutbox.Tests.
//
// NOTE: this example wires against Postgres and therefore needs a reachable database to RUN. The wiring is
// the point; the deterministic proof lives in the integration test. The connection string comes from
// configuration ("OrdersConnection"); if absent a local default is used.
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
                .WithSimpleGuidGenerator() // UnitOfWorkFactory depends on IGuidGenerator to stamp TransactionId
                .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
                .WithPersistence<EFCorePersistenceBuilder>(ef =>
                {
                    ef.AddDbContext<AppDbContext>("Orders", o => o.UseNpgsql(connectionString));
                    ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");
                })
                .WithEventHandling<MassTransitEventHandlingBuilder>(e =>
                {
                    // Recipe 2b through the PUBLIC wrapper: MassTransit's native EF Core bus outbox, bound to
                    // the "Orders" datastore. The AppDbContext maps AddTransactionalOutboxEntities().
                    e.UseBrokerOutbox<AppDbContext>(o => o.OnDataStore("Orders").UsePostgres());
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
