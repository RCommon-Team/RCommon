using Examples.EventHandling.Outbox.Modular.Billing;
using Examples.EventHandling.Outbox.Modular.Ordering;
using Examples.EventHandling.Outbox.Modular.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RCommon;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;

// ---------------------------------------------------------------------------------------------------
// MODULAR, MULTI-DATASTORE TRANSACTIONAL OUTBOX
//
// This is the shape a real modular application uses: several independent bounded contexts, each owning
// its own datastore and its own transactional outbox, composed together at a single root. Each module
// calls WithPersistence/WithEventHandling for ITSELF; no module knows about the others.
//
// This composition (multiple WithPersistence calls across modules, only some of which configure an
// outbox) is exactly the shape that silently dropped durable events before 3.2.1 (see the changelog
// and Examples.EventHandling.Outbox.Tests/ReproOutboxModularDiTests). As of 3.2.1 the outbox tracker is
// registered authoritatively in the shared outbox core, so every datastore's durable events persist
// regardless of the order in which the modules are composed.
// ---------------------------------------------------------------------------------------------------

var ordersDb = Guid.NewGuid().ToString();
var billingDb = Guid.NewGuid().ToString();
var shippingDb = Guid.NewGuid().ToString();

var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging => logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning))
    .ConfigureServices(services =>
    {
        services.AddRCommon()
            .WithSimpleGuidGenerator() // the outbox router stamps each row's Id
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            // Compose the modules. Order is intentionally NOT Ordering-first to demonstrate that the
            // fixed registration is order-independent.
            .AddBillingModule(billingDb)
            .AddShippingModule(shippingDb)
            .AddOrderingModule(ordersDb);
    })
    .Build();

Console.WriteLine("Modular multi-datastore outbox example starting");

// Create each datastore's schema (including its __OutboxMessages table).
using (var scope = host.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<BillingDbContext>().Database.EnsureCreatedAsync();
    await scope.ServiceProvider.GetRequiredService<ShippingDbContext>().Database.EnsureCreatedAsync();
}

// Start the host so the single OutboxProcessingService (which drains every registered datastore) runs.
await host.StartAsync();

// Commit one aggregate per bounded context. Each raises a durable domain event that must land in ITS
// OWN datastore's outbox — never another's.
using (var scope = host.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();

    var orders = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
    orders.DataStoreName = "Ordering";
    var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
    invoices.DataStoreName = "Billing";
    var shipments = sp.GetRequiredService<IAggregateRepository<Shipment, Guid>>();
    shipments.DataStoreName = "Shipping";

    var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
    order.Place();
    var invoice = new Invoice { CustomerName = "Ada Lovelace", Amount = 249.99m };
    invoice.Raise();
    var shipment = new Shipment { Destination = "London", TrackingNumber = "TRK-1" };
    shipment.Dispatch();

    using var uow = uowFactory.Create();
    await orders.AddAsync(order);
    await invoices.AddAsync(invoice);
    await shipments.AddAsync(shipment);
    await uow.CommitAsync();
}

// Show each datastore's outbox row count — all three persisted, none leaked into another datastore.
using (var scope = host.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var orderingRows = await sp.GetRequiredService<OrderingDbContext>().Set<OutboxMessage>().AsNoTracking().CountAsync();
    var billingRows = await sp.GetRequiredService<BillingDbContext>().Set<OutboxMessage>().AsNoTracking().CountAsync();
    var shippingRows = await sp.GetRequiredService<ShippingDbContext>().Set<OutboxMessage>().AsNoTracking().CountAsync();

    Console.WriteLine($"Outbox rows  ->  Ordering: {orderingRows}, Billing: {billingRows}, Shipping: {shippingRows}");
}

await host.StopAsync();
Console.WriteLine("Modular multi-datastore outbox example complete");
