using Microsoft.EntityFrameworkCore;
using RCommon;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;

namespace Examples.EventHandling.Outbox.Modular.Shipping;

// ---------------------------------------------------------------------------------------------------
// Shipping bounded context — its own datastore ("Shipping") with its own transactional outbox. A third
// datastore makes the point that the pattern scales to N modules with no cross-module coordination.
// ---------------------------------------------------------------------------------------------------

public sealed class ShipmentDispatchedEvent : IDomainEvent
{
    public ShipmentDispatchedEvent(Guid shipmentId, string trackingNumber)
    {
        ShipmentId = shipmentId;
        TrackingNumber = trackingNumber;
    }

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid ShipmentId { get; }
    public string TrackingNumber { get; } = string.Empty;
}

public sealed class Shipment : AggregateRoot<Guid>
{
    public Shipment() : base(Guid.NewGuid()) { }
    public string Destination { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;

    public void Dispatch() => AddDomainEvent(new ShipmentDispatchedEvent(Id, TrackingNumber));
}

public sealed class ShipmentDispatchedEventHandler : ISubscriber<ShipmentDispatchedEvent>
{
    public Task HandleAsync(ShipmentDispatchedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  [shipping] shipment {@event.ShipmentId} dispatched ({@event.TrackingNumber})");
        return Task.CompletedTask;
    }
}

public sealed class ShippingDbContext : RCommonDbContext
{
    public ShippingDbContext(DbContextOptions<ShippingDbContext> options) : base(options) { }
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxMessages();
    }
}

public static class ShippingModule
{
    public static IRCommonBuilder AddShippingModule(this IRCommonBuilder rcommon, string database)
    {
        return rcommon
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<ShippingDbContext>("Shipping", o => o.UseInMemoryDatabase(database));
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Shipping");
            })
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
            {
                events.AddSubscriber<ShipmentDispatchedEvent, ShipmentDispatchedEventHandler>();
                events.Publish<ShipmentDispatchedEvent>().UseOutbox("Shipping");
            });
    }
}
