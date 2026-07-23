using System;
using System.Threading.Tasks;
using FluentAssertions; // AwesomeAssertions ships the FluentAssertions namespace as a drop-in replacement
using MassTransit; // UsingInMemory + ConfigureEndpoints are native MassTransit extensions
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.MassTransit;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Transactions;
using Xunit;

// The RCommon outbox row type. This is DIFFERENT from MassTransit's own OutboxMessage —
// recipe 2a stages the durable domain event in RCommon's __OutboxMessages table.
using OutboxMessage = RCommon.Persistence.Outbox.OutboxMessage;

namespace Examples.Messaging.MassTransit.Tests;

/// <summary>
/// AC-16 e2e proof for recipe 2a (MassTransit half): "DDD + UnitOfWork + broker AS A PRODUCER behind
/// RCommon's per-datastore outbox". A durable domain event raised by an aggregate is staged in RCommon's
/// own <c>__OutboxMessages</c> table ATOMICALLY with the business state, inside the UnitOfWork's ambient
/// <see cref="System.Transactions.TransactionScope"/>. In a running host, a background poller would later
/// relay that staged row to the MassTransit producer (Publish) post-commit — proving broker-as-producer
/// durability without ever hitting the broker inside the business transaction.
///
/// The wiring under test is exactly the example's recipe 2a wiring (see the example's Program.cs):
///   e.UseRCommonOutbox("Orders"); e.Publish&lt;OrderConfirmed&gt;(); e.UsingInMemory(...).
///
/// <b>ImmediateDispatch is set to FALSE</b> so committing the UnitOfWork does NOT attempt any in-process
/// relay to the (never-started) bus. The event simply stages as a durable row. This makes the assertion
/// deterministic: the atomic-staging/rollback behaviour is the AC-16 outcome for recipe 2a; we do NOT
/// assert broker relay / ProcessedAtUtc here.
/// </summary>
[Trait("Category", "Integration")] // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgreSqlCollection.Name)]
public class Recipe2aMassTransitTests
{
    private readonly PostgreSqlFixture _pg;

    public Recipe2aMassTransitTests(PostgreSqlFixture pg) => _pg = pg;

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon()
            .WithSimpleGuidGenerator() // OutboxEventRouter/UnitOfWorkFactory need IGuidGenerator to stamp Ids.
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("Orders", o => o.UseNpgsql(_pg.ConnectionString));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");
                // ImmediateDispatch=false: stage only; do not relay to the unstarted bus post-commit.
                ef.AddOutbox<EFCoreOutboxStore>(o => { o.ImmediateDispatch = false; }, dataStoreName: "Orders");
            })
            .WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            {
                e.UseRCommonOutbox("Orders");    // builder default: route published events to RCommon's outbox
                e.Publish<OrderConfirmed>();     // OrderConfirmed is staged to the outbox on commit (by type)
                e.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));
            });

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Ensures the schema exists AND resets the tables asserted on, so both tests are order-independent
    /// even though they share one Postgres container (collection fixture).
    /// </summary>
    private static async Task EnsureCleanSchemaAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.EnsureCreatedAsync(); // creates Orders + __OutboxMessages
        await ctx.Set<OutboxMessage>().ExecuteDeleteAsync();
        await ctx.Set<Order>().ExecuteDeleteAsync();
    }

    private static async Task<(int orders, int outboxMessages)> CountRowsAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orders = await ctx.Set<Order>().AsNoTracking().CountAsync();
        var outboxMessages = await ctx.Set<OutboxMessage>().AsNoTracking().CountAsync();
        return (orders, outboxMessages);
    }

    [Fact]
    public async Task Confirming_an_order_inside_UnitOfWork_stages_the_domain_event_in_RCommons_outbox_atomically()
    {
        await using var provider = BuildProvider();
        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var repo = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
            repo.DataStoreName = "Orders";

            var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
            order.Confirm(); // raises OrderConfirmed via AddDomainEvent

            using var uow = uowFactory.Create();
            await repo.AddAsync(order);
            await uow.CommitAsync();
        }

        var (orders, outboxMessages) = await CountRowsAsync(provider);

        orders.Should().Be(1, "the business row should be committed");
        outboxMessages.Should().Be(1,
            "the durable OrderConfirmed event should have been staged in RCommon's __OutboxMessages " +
            "in the SAME transaction as the business row");
    }

    [Fact]
    public async Task Rolled_back_UnitOfWork_persists_neither_the_order_nor_the_outbox_row()
    {
        await using var provider = BuildProvider();
        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var repo = sp.GetRequiredService<IAggregateRepository<Order, Guid>>();
            repo.DataStoreName = "Orders";

            var order = new Order { CustomerName = "Grace Hopper", Total = 99.00m };
            order.Confirm();

            using var uow = uowFactory.Create();
            await repo.AddAsync(order);

            // Deliberately do NOT commit. Disposing the UoW without CommitAsync rolls back the ambient
            // TransactionScope, so neither the business row nor the staged outbox row survives.
        }

        var (orders, outboxMessages) = await CountRowsAsync(provider);

        orders.Should().Be(0, "the business row must be rolled back");
        outboxMessages.Should().Be(0, "the staged outbox row must be rolled back with the transaction");
    }
}
