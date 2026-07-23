using System;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.IntegrationTests.Fixtures;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.IntegrationTests;

/// <summary>
/// Pins the transactional contract for a unit of work that spans MORE THAN ONE datastore — the question
/// a heterogeneous-engine deployment (e.g. Postgres + SQL Server) runs into in production.
///
/// THE CONTRACT (what these tests assert):
///  1. An outbox row is atomic with ITS OWN datastore's state change — they share one connection/local
///     transaction (co-location, spec MN-4). The outbox never enlists a second resource manager.
///  2. There is NO distributed atomicity across datastores. RCommon's <see cref="UnitOfWork"/> wraps a
///     single ambient <see cref="TransactionScope"/> (Required). If one unit of work writes to a SECOND
///     datastore, a second physical connection enlists and the ambient transaction promotes to a
///     distributed transaction (2PC). On Postgres that means <c>PREPARE TRANSACTION</c>, which is
///     disabled by default (<c>max_prepared_transactions = 0</c>) and fails loud — it never silently
///     half-commits. (Postgres + SQL Server would promote to MSDTC with the same "no cross-datastore
///     atomicity" outcome. Two Postgres databases reproduce the identical promotion because the trigger
///     is "a second connection in one scope," not the engine pairing.)
///  3. Therefore an application that writes state to more than one datastore in one logical operation
///     must split it into a UNIT OF WORK PER DATASTORE. Each commits locally and atomically (state +
///     that datastore's outbox row); cross-datastore delivery is eventual (at-least-once via the poller).
///
/// Modelled as two distinct Postgres databases on one container (each context points at its own
/// <c>Database=</c>), so each owns an isolated <c>__OutboxMessages</c> table.
/// </summary>
[Trait("Category", "Integration")]
[Collection(PostgreSqlCollection.Name)]
public class MultiDataStoreUnitOfWorkSemanticsTests
{
    private readonly PostgreSqlFixture _pg;

    public MultiDataStoreUnitOfWorkSemanticsTests(PostgreSqlFixture pg) => _pg = pg;

    // ---- Ordering datastore ----
    public sealed class OrderPlacedEvent : IDomainEvent
    {
        public OrderPlacedEvent(Guid orderId) => OrderId = orderId;
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
        public Guid OrderId { get; }
    }

    public sealed class Order : AggregateRoot<Guid>
    {
        public Order() : base(Guid.NewGuid()) { }
        public string CustomerName { get; set; } = string.Empty;
        public void Place() => AddDomainEvent(new OrderPlacedEvent(Id));
    }

    public sealed class OrdersDbContext : RCommonDbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
        public DbSet<Order> Orders => Set<Order>();
    }

    // ---- Billing datastore (a genuinely separate database) ----
    public sealed class InvoiceRaisedEvent : IDomainEvent
    {
        public InvoiceRaisedEvent(Guid invoiceId) => InvoiceId = invoiceId;
        public Guid EventId { get; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
        public Guid InvoiceId { get; }
    }

    public sealed class Invoice : AggregateRoot<Guid>
    {
        public Invoice() : base(Guid.NewGuid()) { }
        public string CustomerName { get; set; } = string.Empty;
        public void Raise() => AddDomainEvent(new InvoiceRaisedEvent(Id));
    }

    public sealed class BillingDbContext : RCommonDbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }
        public DbSet<Invoice> Invoices => Set<Invoice>();
    }

    private ServiceProvider BuildProvider(string ordersConnectionString, string billingConnectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithEventHandling<InMemoryEventBusBuilder>(events =>
            {
                // Each event is durable to ITS OWN co-located datastore's outbox.
                events.Publish<OrderPlacedEvent>().UseOutbox("Orders");
                events.Publish<InvoiceRaisedEvent>().UseOutbox("Billing");
            })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<OrdersDbContext>("Orders", o => o.UseNpgsql(ordersConnectionString));
                ef.AddDbContext<BillingDbContext>("Billing", o => o.UseNpgsql(billingConnectionString));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");
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

    private static async Task<(int orders, int ordersOutbox, int invoices, int billingOutbox)> CountAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var billing = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        return (
            await orders.Set<Order>().AsNoTracking().CountAsync(),
            await orders.Set<OutboxMessage>().AsNoTracking().CountAsync(),
            await billing.Set<Invoice>().AsNoTracking().CountAsync(),
            await billing.Set<OutboxMessage>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Single_unit_of_work_writing_two_datastores_fails_loud_and_commits_nothing()
    {
        var ordersCs = await _pg.CreateUniqueDatabaseAsync("orders");
        var billingCs = await _pg.CreateUniqueDatabaseAsync("billing");
        await using var provider = BuildProvider(ordersCs, billingCs);
        await EnsureSchemasAsync(provider);

        TransactionAbortedException? thrown = null;
        try
        {
            using var scope = provider.CreateScope();
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();

            var orders = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
            orders.DataStoreName = "Orders";
            var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
            invoices.DataStoreName = "Billing";

            var order = new Order { CustomerName = "Ada Lovelace" };
            order.Place();
            var invoice = new Invoice { CustomerName = "Ada Lovelace" };
            invoice.Raise();

            using var uow = uowFactory.Create();
            await orders.AddAsync(order);      // enlists the Orders connection
            await invoices.AddAsync(invoice);  // enlists a SECOND connection => ambient tx promotes to 2PC
            await uow.CommitAsync();            // PREPARE TRANSACTION rejected by Postgres
        }
        catch (TransactionAbortedException ex)
        {
            thrown = ex;
        }

        thrown.Should().NotBeNull(
            "a single unit of work spanning two datastores promotes to a distributed transaction; RCommon " +
            "provides NO cross-datastore atomicity and must fail loud rather than half-commit");
        thrown!.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be("55000",
                "Postgres rejects PREPARE TRANSACTION when prepared transactions are disabled (the default)");

        var (orderRows, ordersOutbox, invoiceRows, billingOutbox) = await CountAsync(provider);
        orderRows.Should().Be(0, "the aborted transaction must leave no Orders state");
        ordersOutbox.Should().Be(0, "the aborted transaction must leave no Orders outbox row");
        invoiceRows.Should().Be(0, "the aborted transaction must leave no Billing state");
        billingOutbox.Should().Be(0, "the aborted transaction must leave no Billing outbox row");
    }

    [Fact]
    public async Task Per_datastore_unit_of_work_scopes_each_commit_locally_with_their_own_outbox_row()
    {
        var ordersCs = await _pg.CreateUniqueDatabaseAsync("orders");
        var billingCs = await _pg.CreateUniqueDatabaseAsync("billing");
        await using var provider = BuildProvider(ordersCs, billingCs);
        await EnsureSchemasAsync(provider);

        // ONE logical operation, split into a unit of work PER datastore. Each is a single connection, so
        // each commits with a local transaction (no promotion) — state + that datastore's outbox row.
        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var orders = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
            orders.DataStoreName = "Orders";

            var order = new Order { CustomerName = "Ada Lovelace" };
            order.Place();

            using var uow = uowFactory.Create();
            await orders.AddAsync(order);
            await uow.CommitAsync();
        }

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
            invoices.DataStoreName = "Billing";

            var invoice = new Invoice { CustomerName = "Ada Lovelace" };
            invoice.Raise();

            using var uow = uowFactory.Create();
            await invoices.AddAsync(invoice);
            await uow.CommitAsync();
        }

        var (orderRows, ordersOutbox, invoiceRows, billingOutbox) = await CountAsync(provider);
        orderRows.Should().Be(1, "the Orders unit of work committed locally");
        ordersOutbox.Should().Be(1, "the Order's durable event is atomic with the Orders state change");
        invoiceRows.Should().Be(1, "the Billing unit of work committed locally");
        billingOutbox.Should().Be(1, "the Invoice's durable event is atomic with the Billing state change");
    }
}
