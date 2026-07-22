using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RCommon.Entities;
using RCommon.IntegrationTests.Fixtures;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.IntegrationTests;

/// <summary>
/// CAPSTONE (spec AC-7): proves the headline B4 fix end-to-end against real Postgres. An event raised
/// by an aggregate persisted to a NON-DEFAULT datastore ("Billing") must land in THAT datastore's
/// <c>__OutboxMessages</c> table within the same transaction — NOT the default datastore's ("Orders").
/// A rolled-back UnitOfWork must leave neither business state nor outbox rows in either datastore.
///
/// Two genuinely separate datastores are modelled as two distinct Postgres DATABASES on one container
/// (each context points at a different <c>Database=</c>), so each owns its own isolated
/// <c>__OutboxMessages</c> table. Both datastores are registered as outbox owners under a SINGLE poller
/// (AC-9) via two <see cref="OutboxPersistenceBuilderExtensions.AddOutbox{TOutboxStore}"/> calls, which
/// is safe because those registrations are idempotent for their shared singletons.
/// </summary>
[Trait("Category", "Integration")] // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgreSqlCollection.Name)]
public class CrossDataStoreOutboxTests
{
    private readonly PostgreSqlFixture _pg;

    public CrossDataStoreOutboxTests(PostgreSqlFixture pg) => _pg = pg;

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
        // into it. OutboxMessage is auto-mapped because "Orders" is a registered outbox owner.
    }

    // ---- Non-default "Billing" datastore context ----
    public sealed class BillingDbContext : RCommonDbContext
    {
        public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }
        public DbSet<Invoice> Invoices => Set<Invoice>();
    }

    private ServiceProvider BuildProvider(string billingConnectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon()
            .WithSimpleGuidGenerator() // OutboxEventRouter needs IGuidGenerator to stamp each outbox row's Id.
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<OrdersDbContext>("Orders", o => o.UseNpgsql(_pg.ConnectionString));
                ef.AddDbContext<BillingDbContext>("Billing", o => o.UseNpgsql(billingConnectionString));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");

                // Register BOTH datastores as outbox owners. AddOutbox's shared singletons (poller,
                // diagnostics, store binding) are idempotent, so this yields ONE poller draining both
                // datastores while each name lands in the registry (AC-9). OutboxMessage is then
                // auto-mapped onto both contexts' models.
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Orders");
                ef.AddOutbox<EFCoreOutboxStore>(dataStoreName: "Billing");
            });

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Creates the second Postgres database for the Billing datastore and returns its connection string.
    /// The fixture's connection string targets the default database (Orders).
    /// </summary>
    private async Task<string> CreateBillingDatabaseAsync()
    {
        var billingDbName = "billing_" + Guid.NewGuid().ToString("N");

        // CREATE DATABASE cannot run inside a transaction; use a plain connection on the default DB.
        await using (var admin = new NpgsqlConnection(_pg.ConnectionString))
        {
            await admin.OpenAsync();
            await using var cmd = admin.CreateCommand();
            cmd.CommandText = $"CREATE DATABASE \"{billingDbName}\";";
            await cmd.ExecuteNonQueryAsync();
        }

        var builder = new NpgsqlConnectionStringBuilder(_pg.ConnectionString) { Database = billingDbName };
        return builder.ConnectionString;
    }

    private static async Task EnsureSchemasAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var billing = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        await orders.Database.EnsureCreatedAsync(); // creates Orders' __OutboxMessages
        await billing.Database.EnsureCreatedAsync(); // creates Invoices + Billing's __OutboxMessages
    }

    private static async Task<(int invoices, int billingOutbox, int ordersOutbox)> CountRowsAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var billing = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var invoices = await billing.Set<Invoice>().AsNoTracking().CountAsync();
        var billingOutbox = await billing.Set<OutboxMessage>().AsNoTracking().CountAsync();
        var ordersOutbox = await orders.Set<OutboxMessage>().AsNoTracking().CountAsync();
        return (invoices, billingOutbox, ordersOutbox);
    }

    [Fact]
    public async Task Event_from_non_default_datastore_aggregate_lands_in_that_datastores_outbox_only()
    {
        var billingConnectionString = await CreateBillingDatabaseAsync();
        await using var provider = BuildProvider(billingConnectionString);
        await EnsureSchemasAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            // Resolve the repository for the "Billing" datastore so AddEntity(entity, "Billing") fires.
            var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
            invoices.DataStoreName = "Billing";

            var invoice = new Invoice { CustomerName = "Ada Lovelace", Amount = 249.99m };
            invoice.Raise(); // raises InvoiceRaisedEvent via AddDomainEvent

            using var uow = uowFactory.Create();
            await invoices.AddAsync(invoice);
            await uow.CommitAsync();
        }

        var (invoiceCount, billingOutbox, ordersOutbox) = await CountRowsAsync(provider);

        invoiceCount.Should().Be(1, "the Billing aggregate was committed");
        billingOutbox.Should().Be(1, "the event was raised by a Billing-datastore aggregate, so it belongs in Billing's outbox (B4 fix)");
        ordersOutbox.Should().Be(0, "the event must NOT leak into the default (Orders) datastore's outbox");
    }

    [Fact]
    public async Task Rolled_back_unit_of_work_persists_neither_business_state_nor_outbox_rows_in_either_datastore()
    {
        var billingConnectionString = await CreateBillingDatabaseAsync();
        await using var provider = BuildProvider(billingConnectionString);
        await EnsureSchemasAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var invoices = sp.GetRequiredService<IAggregateRepository<Invoice, Guid>>();
            invoices.DataStoreName = "Billing";

            var invoice = new Invoice { CustomerName = "Grace Hopper", Amount = 99.00m };
            invoice.Raise();

            using var uow = uowFactory.Create();
            await invoices.AddAsync(invoice);

            // Deliberately do NOT commit. Disposing the UoW without CommitAsync rolls back the ambient
            // transaction, so neither the business row nor the staged outbox row survives.
        }

        var (invoiceCount, billingOutbox, ordersOutbox) = await CountRowsAsync(provider);

        invoiceCount.Should().Be(0, "the business row must be rolled back");
        billingOutbox.Should().Be(0, "the Billing outbox row must be rolled back with the transaction");
        ordersOutbox.Should().Be(0, "nothing was ever written to the Orders datastore");
    }
}
