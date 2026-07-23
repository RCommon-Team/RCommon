using Examples.EventHandling.TransactionScript;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon;
using RCommon.EventHandling;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;

// Recipe 3: transaction-script / CRUD + UnitOfWork with ROUTER-ADDED events (the NON-DDD path).
//
// Unlike the Outbox example, where a DDD aggregate raises a domain event via AddDomainEvent and the
// tracker routes it, here a transaction-script service adds the integration event DIRECTLY to the
// OutboxEventRouter within the UnitOfWork, then commits. The buffered event is persisted to the
// outbox by the UnitOfWork commit pipeline (PersistBufferedEventsAsync) and dispatched in-process
// post-commit (RouteEventsAsync / Phase 3, since ImmediateDispatch defaults to true).

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    })
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithSimpleGuidGenerator() // OutboxEventRouter needs IGuidGenerator to stamp each outbox row's Id.
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("AppDb", options => options.UseInMemoryDatabase("transaction-script-example"));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
                ef.AddOutbox<EFCoreOutboxStore>();
            })
            .WithEventHandling<InMemoryEventBusBuilder>(eh =>
            {
                eh.AddSubscriber<StockAdjustedEvent, StockAdjustedHandler>();

                // Declaring the durable route is what lets Phase 3's producer match & dispatch the event
                // in-process to the subscriber (and mark the row processed). For a router-added event the
                // route is NOT required for PERSISTENCE — PersistBufferedEventsAsync drains the router
                // buffer unconditionally — but registering it keeps the post-commit dispatch behaviour
                // identical to the aggregate path.
                eh.Publish<StockAdjustedEvent>().UseOutbox("AppDb");
            });
    })
    .Build();

Console.WriteLine("Example Starting");

using (var schemaScope = host.Services.CreateScope())
{
    var dbContext = schemaScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

await host.StartAsync();

using (var scope = host.Services.CreateScope())
{
    // Transaction-script style: no aggregate. Resolve the repository (plain CRUD write), the UoW
    // factory, and the CONCRETE OutboxEventRouter so we can use the (evt, dataStoreName) overload.
    var stockItems = scope.ServiceProvider.GetRequiredService<ILinqRepository<StockItem>>();
    var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
    var router = scope.ServiceProvider.GetRequiredService<OutboxEventRouter>();

    var stockItem = new StockItem { Sku = "ABC", Quantity = 10 };

    using var uow = unitOfWorkFactory.Create();

    // CRUD write (no aggregate, no domain event).
    await stockItems.AddAsync(stockItem);

    // The transaction script explicitly enqueues the integration event on the router. The two-arg
    // overload (evt, dataStoreName) lives on the CONCRETE OutboxEventRouter, not IEventRouter.
    router.AddTransactionalEvent(new StockAdjustedEvent(stockItem.Id, stockItem.Sku, stockItem.Quantity), "AppDb");

    // CommitAsync runs Phase 1 (PersistBufferedEventsAsync drains the router buffer -> outbox row),
    // Phase 2 (commit), and Phase 3 (RouteEventsAsync -> immediate in-process dispatch + mark processed).
    await uow.CommitAsync();

    Console.WriteLine($"Committed stock adjustment. Subscriber invocations so far: {StockAdjustedHandler.HandledCount}");
}

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var row = await dbContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
    Console.WriteLine($"Outbox row ({row.EventType}) processed at: {row.ProcessedAtUtc?.ToString() ?? "not yet processed"}");
}

await host.StopAsync();

Console.WriteLine("Example Complete");
