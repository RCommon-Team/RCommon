using Microsoft.EntityFrameworkCore;
using RCommon;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;

namespace Examples.EventHandling.Outbox.Modular.Billing;

// ---------------------------------------------------------------------------------------------------
// Billing bounded context — its own datastore ("Billing") with its own transactional outbox.
// ---------------------------------------------------------------------------------------------------

public sealed class InvoiceRaisedEvent : IDomainEvent
{
    public InvoiceRaisedEvent(Guid invoiceId, decimal amount)
    {
        InvoiceId = invoiceId;
        Amount = amount;
    }

    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
    public Guid InvoiceId { get; }
    public decimal Amount { get; }
}

public sealed class Invoice : AggregateRoot<Guid>
{
    public Invoice() : base(Guid.NewGuid()) { }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public void Raise() => AddDomainEvent(new InvoiceRaisedEvent(Id, Amount));
}

public sealed class InvoiceRaisedEventHandler : ISubscriber<InvoiceRaisedEvent>
{
    public Task HandleAsync(InvoiceRaisedEvent @event, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"  [billing] invoice {@event.InvoiceId} raised for {@event.Amount:C}");
        return Task.CompletedTask;
    }
}

public sealed class BillingDbContext : RCommonDbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddOutboxMessages();
    }
}

public static class BillingModule
{
    /// <summary>
    /// Registers the Billing bounded context: its DbContext + datastore, its transactional outbox, and
    /// its event route. Note there is no <c>SetDefaultDataStore</c> here — a non-primary module does not
    /// touch the default; it just contributes its own datastore.
    /// </summary>
    public static IRCommonBuilder AddBillingModule(this IRCommonBuilder rcommon, string database)
    {
        return rcommon
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<BillingDbContext>("Billing", o => o.UseInMemoryDatabase(database));
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Billing");
            })
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
            {
                events.AddSubscriber<InvoiceRaisedEvent, InvoiceRaisedEventHandler>();
                events.Publish<InvoiceRaisedEvent>().UseOutbox("Billing");
            });
    }
}
