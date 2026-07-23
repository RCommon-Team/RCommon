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
/// Regression for 3.2.0 defect #15 (silent outbox data loss under modular composition).
///
/// The customer composes their app from independent modules, each of which calls
/// <c>WithPersistence&lt;EFCorePersistenceBuilder&gt;(...)</c> for its own bounded context. Only one
/// module configures the transactional outbox. The others do not.
///
/// <c>WithPersistence</c> runs <c>WithEventTracking</c> at the END of every call, which
/// <c>TryAddScoped&lt;IEntityEventTracker, InMemoryEntityEventTracker&gt;</c>. In 3.2.0 the outbox
/// producer also registered its tracker with <c>TryAdd</c> — so if any non-outbox module registered
/// first, the in-memory tracker was pinned and the outbox tracker registration silently no-opped.
/// Durable events were then dispatched in-process and NEVER written to <c>__OutboxMessages</c>, with
/// no exception and no warning. This test reproduces that composition (non-outbox datastore first,
/// outbox datastore second) and asserts the durable event actually lands in the outbox.
///
/// Runs on the EF Core InMemory provider (fast lane — no containers). InMemory has no real
/// transactions, so this proves routing/persistence wiring, not transactional atomicity (that is
/// covered by the Postgres integration tests).
/// </summary>
public class ReproOutboxModularDiTests
{
    // ---- Domain event published durably to the "Billing" outbox ----
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

    // ---- "Orders" module datastore — NO outbox. Registered FIRST. ----
    public sealed class OrdersDbContext : RCommonDbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder modelBuilder) => base.OnModelCreating(modelBuilder);
    }

    // ---- "Billing" module datastore — HAS the outbox. Registered SECOND. ----
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
            .WithSimpleGuidGenerator()
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
                events.Publish<InvoiceRaisedEvent>().UseOutbox("Billing"))
            // MODULE 1 (no outbox) — registered FIRST. Its WithEventTracking pins the in-memory
            // IEntityEventTracker, which is what defeated the outbox registration in 3.2.0.
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<OrdersDbContext>("Orders", o => o.UseInMemoryDatabase(ordersDb));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");
            })
            // MODULE 2 (outbox) — registered SECOND, as a separate WithPersistence call.
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<BillingDbContext>("Billing", o => o.UseInMemoryDatabase(billingDb));
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Billing");
            });

        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public void OutboxTracker_WinsRegistration_UnderModularComposition()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        using var scope = provider.CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<IEntityEventTracker>();

        tracker.Should().BeOfType<OutboxEntityEventTracker>(
            "the transactional outbox must decorate the entity-event tracker even when a non-outbox " +
            "module's WithPersistence registered the in-memory tracker first (defect #15)");
    }

    [Fact]
    public async Task DurableEvent_IsPersisted_UnderModularComposition()
    {
        using var provider = BuildProvider(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.EnsureCreatedAsync();
            await scope.ServiceProvider.GetRequiredService<BillingDbContext>().Database.EnsureCreatedAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
            invoices.DataStoreName = "Billing";

            var invoice = new Invoice { CustomerName = "Ada Lovelace", Amount = 249.99m };
            invoice.Raise();

            using var uow = uowFactory.Create();
            await invoices.AddAsync(invoice);
            await uow.CommitAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var billing = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var billingOutbox = await billing.Set<OutboxMessage>().AsNoTracking().CountAsync();

            billingOutbox.Should().Be(1,
                "the durable event must be written to the Billing outbox — under modular composition " +
                "3.2.0 silently dropped it (0 rows, no error): defect #15");
        }
    }
}
