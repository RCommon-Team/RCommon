using Examples.EventHandling.Outbox;
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
                ef.AddDbContext<AppDbContext>("AppDb", options => options.UseInMemoryDatabase("outbox-example"));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");

                // Registers OutboxProcessingService as a hosted service, IOutboxStore, and replaces
                // the default in-memory event router/entity tracker with the outbox-backed ones. See
                // event-handling/outbox-producer-processor-topology.mdx for the full three-phase flow
                // this enables.
                ef.AddOutbox<EFCoreOutboxStore>();
            })
            .WithEventHandling<InMemoryEventBusBuilder>(eh =>
            {
                eh.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
            });
    })
    .Build();

Console.WriteLine("Example Starting");

using (var schemaScope = host.Services.CreateScope())
{
    var dbContext = schemaScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

// Start the host so OutboxProcessingService (the durable poller) is running -- it isn't needed for
// this single-host, default-ImmediateDispatch happy path (Phase 3 dispatches immediately after
// commit), but a real app always has it running as the durable fallback and cross-host mechanism.
await host.StartAsync();

using (var scope = host.Services.CreateScope())
{
    var orders = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Order, Guid>>();
    var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

    var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
    order.Place(); // raises OrderPlacedEvent via AddDomainEvent

    using var uow = unitOfWorkFactory.Create();
    await orders.AddAsync(order);

    // Phase 1 (persist the outbox row within the transaction), Phase 2 (commit), and Phase 3
    // (best-effort immediate in-process dispatch to OrderPlacedEventHandler above) all happen here.
    await uow.CommitAsync();

    Console.WriteLine($"Committed order {order.Id}. Subscriber invocations so far: {OrderPlacedEventHandler.HandledCount}");
}

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var row = await dbContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();
    Console.WriteLine($"Outbox row processed at: {row.ProcessedAtUtc?.ToString() ?? "not yet processed"}");
}

await host.StopAsync();

Console.WriteLine("Example Complete");
