using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;
using Xunit;

namespace Examples.EventHandling.Outbox.Tests;

/// <summary>
/// Recipe 1, 2-datastore variant (spec B4/U5): proves per-datastore routing isolation. Two datastores
/// ("Orders" + "Billing") are each registered with their own <see cref="RCommonDbContext"/> and their
/// own outbox. An event is published durably to "Billing" only; the Billing aggregate is directed to
/// the "Billing" datastore via <c>repo.DataStoreName = "Billing"</c>. After commit, the Billing outbox
/// has exactly one row and the Orders outbox has none — the durable route lands the event in the owning
/// datastore's <c>__OutboxMessages</c> and does NOT leak into the other datastore.
///
/// NOTE ON THE PROVIDER: this recipe runs on the EF Core InMemory provider (fast lane — no containers).
/// InMemory has no real transactions, so it can only demonstrate routing/dispatch, NOT transactional
/// atomicity (buffer-within-transaction / rollback-discards-outbox-row). Atomicity is gated separately
/// by the Postgres integration tests in RCommon.IntegrationTests.CrossDataStoreOutboxTests.
/// </summary>
public class TwoDataStoreOutboxTests
{
    // ---- Domain event raised by the Billing aggregate ----
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

    // ---- Business aggregate persisted to the NON-DEFAULT "Billing" datastore ----
    public sealed class Invoice : AggregateRoot<Guid>
    {
        public Invoice() : base(Guid.NewGuid()) { }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public void Raise() => AddDomainEvent(new InvoiceRaisedEvent(Id, Amount));
    }

    // ---- Default "Orders" datastore context ----
    public sealed class OrdersDbContext : RCommonDbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }

        // No business entity needed here; this datastore only proves the Billing event does NOT leak
        // into it. __OutboxMessages must still be mapped because "Orders" is a registered outbox owner.
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddOutboxMessages();
    }

    // ---- Non-default "Billing" datastore context ----
    public sealed class BillingDbContext : RCommonDbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }
        public DbSet<Invoice> Invoices => Set<Invoice>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.AddOutboxMessages();
    }

    private static ServiceProvider BuildProvider(string ordersDb, string billingDb)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon()
            .WithSimpleGuidGenerator() // OutboxEventRouter needs IGuidGenerator to stamp each outbox row's Id.
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            // Durability is route-driven and opt-in: publish the Billing aggregate's event durably to the
            // co-located "Billing" outbox so it lands in Billing's __OutboxMessages and nowhere else.
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
                events.Publish<InvoiceRaisedEvent>().UseOutbox("Billing"))
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<OrdersDbContext>("Orders", o => o.UseInMemoryDatabase(ordersDb));
                ef.AddDbContext<BillingDbContext>("Billing", o => o.UseInMemoryDatabase(billingDb));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");

                // Register BOTH datastores as outbox owners so each name lands in the routing registry
                // and __OutboxMessages is mapped onto both contexts.
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Orders");
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Billing");
            });

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task EnsureSchemasAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<BillingDbContext>().Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task DurableEvent_LandsInOwningDatastoreOutboxOnly_AndNotTheOtherDatastore()
    {
        using var provider = BuildProvider(
            ordersDb: Guid.NewGuid().ToString(),
            billingDb: Guid.NewGuid().ToString());
        await EnsureSchemasAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();

            // Direct the Billing aggregate to the "Billing" datastore so its event's owning datastore
            // matches the durable route's target.
            var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
            invoices.DataStoreName = "Billing";

            var invoice = new Invoice { CustomerName = "Ada Lovelace", Amount = 249.99m };
            invoice.Raise(); // raises InvoiceRaisedEvent via AddDomainEvent

            using var uow = uowFactory.Create();
            await invoices.AddAsync(invoice);
            await uow.CommitAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var billing = sp.GetRequiredService<BillingDbContext>();
            var orders = sp.GetRequiredService<OrdersDbContext>();

            var billingOutbox = await billing.Set<OutboxMessage>().AsNoTracking().CountAsync();
            var ordersOutbox = await orders.Set<OutboxMessage>().AsNoTracking().CountAsync();

            billingOutbox.Should().Be(1,
                "the event was published durably to \"Billing\" and raised by a Billing-datastore aggregate");
            ordersOutbox.Should().Be(0,
                "per-datastore routing isolation: the event must NOT leak into the Orders datastore's outbox");
        }
    }
}
