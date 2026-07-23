using Microsoft.EntityFrameworkCore;
using RCommon;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;

namespace Examples.EventHandling.Outbox.Modular.Ordering;

// ---------------------------------------------------------------------------------------------------
// Ordering bounded context — its own datastore ("Ordering") with its own transactional outbox.
// Everything this context needs is self-contained in this file, including its RCommon registration.
// ---------------------------------------------------------------------------------------------------

public sealed class OrderPlacedEvent : IDomainEvent
{
    public OrderPlacedEvent(Guid orderId, decimal total)
    {
        OrderId = orderId;
        Total = total;
    }

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid OrderId { get; }
    public decimal Total { get; }
}

public sealed class Order : AggregateRoot<Guid>
{
    public Order() : base(Guid.NewGuid()) { }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }

    public void Place() => AddDomainEvent(new OrderPlacedEvent(Id, Total));
}

public sealed class OrderPlacedEventHandler : ISubscriber<OrderPlacedEvent>
{
    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  [ordering] order {@event.OrderId} placed for {@event.Total:C}");
        return Task.CompletedTask;
    }
}

public sealed class OrderingDbContext : RCommonDbContext
{
    public OrderingDbContext(DbContextOptions<OrderingDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxMessages(); // maps this datastore's __OutboxMessages table
    }
}

public static class OrderingModule
{
    /// <summary>
    /// Registers the Ordering bounded context: its DbContext + datastore, its transactional outbox, and
    /// its event route. Each module owns exactly one datastore and calls <c>WithPersistence</c> once —
    /// the composition root simply chains the modules together (see <c>Program</c>).
    /// </summary>
    public static IRCommonBuilder AddOrderingModule(this IRCommonBuilder rcommon, string database)
    {
        return rcommon
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<OrderingDbContext>("Ordering", o => o.UseInMemoryDatabase(database));

                // Ordering is the application's primary context, so it designates the default datastore.
                // With more than one datastore registered, exactly one module (or the root) must set this;
                // it is not inferred. The outbox poller uses it as its fallback datastore name.
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Ordering");

                // Native RCommon outbox for this datastore (producer + processor).
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Ordering");
            })
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
            {
                events.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
                // Durability is opt-in per route: publish this context's event durably to its own outbox.
                events.Publish<OrderPlacedEvent>().UseOutbox("Ordering");
            });
    }
}
