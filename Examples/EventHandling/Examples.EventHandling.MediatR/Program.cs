using Examples.EventHandling.MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon;
using RCommon.MediatR;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;

// Recipe 5: DDD + UnitOfWork + in-process mediator (MediatR).
//
// A domain event raised by an aggregate is dispatched IN-PROCESS through MediatR to an
// ISubscriber<T> handler as part of the UnitOfWork COMMIT pipeline. MediatR is in-process only --
// there is no broker and no transport.
//
// The aggregate raises the event via AddDomainEvent, the repository stages it, and
// IUnitOfWorkFactory.Create() + CommitAsync() drives it through the transactional event router to
// the MediatR publish producer and on to the subscriber. This is the REAL canonical flow: the event
// is delivered by the commit pipeline, not by directly resolving and invoking the producer.
//
// WithEventHandling<MediatREventHandlingBuilder> self-registers MediatR (it calls
// services.AddMediatR(...) internally), so there is no manual AddMediatR here. The wiring needs BOTH
// verbs: Publish<OrderPlacedEvent>() registers PublishWithMediatREventProducer (the in-process
// producer), and AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>() bridges the RCommon
// subscriber to a MediatR notification handler. AddSubscriber alone does not register the producer.
// The Publish<T>() route here is TRANSIENT (no .UseOutbox), so the event is dispatched immediately by
// the commit pipeline rather than persisted to an outbox.
var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
    })
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("AppDb", options => options.UseInMemoryDatabase("mediatr-example"));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "AppDb");
            })
            .WithEventHandling<MediatREventHandlingBuilder>(events =>
            {
                events.Publish<OrderPlacedEvent>();                              // transient in-process route (no .UseOutbox)
                events.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
            });
    })
    .Build();

Console.WriteLine("Example Starting");

using (var schemaScope = host.Services.CreateScope())
{
    var dbContext = schemaScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

using (var scope = host.Services.CreateScope())
{
    var orders = scope.ServiceProvider.GetRequiredService<IAggregateRepository<Order, Guid>>();
    var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

    // The aggregate raises the domain event via the DDD API.
    var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
    order.Place(); // raises OrderPlacedEvent via AddDomainEvent

    using var uow = unitOfWorkFactory.Create();
    await orders.AddAsync(order);

    // CommitAsync drives the aggregate-raised domain event through the transactional event router to
    // the MediatR publish producer and on to OrderPlacedEventHandler in-process.
    await uow.CommitAsync();

    Console.WriteLine($"Committed order {order.Id}. Subscriber invocations so far: {OrderPlacedEventHandler.HandledCount}");
}

Console.WriteLine("Example Complete");
