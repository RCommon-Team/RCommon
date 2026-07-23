using System;
using System.Threading.Tasks;
using FluentAssertions; // AwesomeAssertions ships the FluentAssertions namespace as a drop-in replacement
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Transactions;
using RCommon.Wolverine;
using Wolverine; // UseWolverine is the native host-level Wolverine integration
using Xunit;

// The RCommon outbox row type. This is DIFFERENT from any broker-native outbox — recipe 2a stages the
// durable domain event in RCommon's own __OutboxMessages table.
using OutboxMessage = RCommon.Persistence.Outbox.OutboxMessage;

namespace Examples.Messaging.Wolverine.Tests;

/// <summary>
/// AC-16 e2e proof for recipe 2a (WOLVERINE half): "DDD + UnitOfWork + broker AS A PRODUCER behind
/// RCommon's per-datastore outbox". A durable domain event raised by an aggregate is staged in RCommon's
/// own <c>__OutboxMessages</c> table ATOMICALLY with the business state, inside the UnitOfWork's ambient
/// <see cref="System.Transactions.TransactionScope"/>. In a running host, a background poller would later
/// relay that staged row to the Wolverine producer (<c>PublishWithWolverineEventProducer</c> -> IMessageBus)
/// post-commit — proving broker-as-producer durability without ever hitting the broker inside the business
/// transaction.
///
/// Recipe 2a is the SUPPORTED broker-as-producer path for Wolverine: Wolverine's native broker outbox
/// (recipe 2b) is NO-GO by design (<c>UseBrokerOutbox&lt;T&gt;</c> throws NotSupportedException), so Wolverine
/// users stage durable events in RCommon's own outbox instead.
///
/// The wiring under test is exactly the example's recipe 2a wiring (see the example's Program.cs):
///   e.UseRCommonOutbox("Orders"); e.Publish&lt;OrderConfirmed&gt;(); with Wolverine wired at the HOST level.
///
/// <b>Wolverine host wrinkle:</b> the RCommon Wolverine producer depends on Wolverine's <c>IMessageBus</c>,
/// which is only registered via <see cref="Host"/>-level <c>UseWolverine(...)</c> — not on a raw
/// <see cref="IServiceCollection"/>. So the provider under test is built from an <see cref="IHost"/> exactly
/// as the example does: <c>Host.CreateDefaultBuilder().UseWolverine(...).ConfigureServices(AddRCommon...)</c>.
///
/// <b>ImmediateDispatch is set to FALSE</b> so committing the UnitOfWork does NOT attempt any in-process
/// relay to the bus. The event simply stages as a durable row. The host is never started, so no poller runs.
/// This makes the assertion deterministic: the atomic-staging/rollback behaviour is the AC-16 outcome for
/// recipe 2a; we do NOT assert broker relay / ProcessedAtUtc here.
/// </summary>
[Trait("Category", "Integration")] // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgreSqlCollection.Name)]
public class Recipe2aWolverineTests
{
    private readonly PostgreSqlFixture _pg;

    public Recipe2aWolverineTests(PostgreSqlFixture pg) => _pg = pg;

    /// <summary>
    /// Builds the recipe 2a provider the SAME way the example does: an <see cref="IHost"/> with Wolverine
    /// wired at the host level (so <c>IMessageBus</c> exists for the RCommon Wolverine producer) plus
    /// RCommon's UnitOfWork + EF Core persistence + per-datastore outbox. The host is NOT started, so no
    /// background poller relays; commit only STAGES the durable row.
    /// </summary>
    private IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .UseWolverine(options =>
            {
                // A local queue keeps Wolverine self-contained; recipe 2a never touches the broker inside
                // the business transaction (ImmediateDispatch=false + host not started).
                options.LocalQueue("orders");
            })
            .ConfigureServices(services =>
            {
                services.AddRCommon()
                    .WithSimpleGuidGenerator() // OutboxEventRouter/UnitOfWorkFactory need IGuidGenerator to stamp Ids.
                    .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
                    .WithPersistence<EFCorePersistenceBuilder>(ef =>
                    {
                        ef.AddDbContext<AppDbContext>("Orders", o => o.UseNpgsql(_pg.ConnectionString));
                        ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");
                        // ImmediateDispatch=false: stage only; do not relay to the (never-started) bus post-commit.
                        ef.AddOutbox<EFCoreOutboxStore>(o => { o.ImmediateDispatch = false; }, dataStoreName: "Orders");
                    })
                    .WithEventHandling<WolverineEventHandlingBuilder>(e =>
                    {
                        e.UseRCommonOutbox("Orders"); // builder default: route published events to RCommon's outbox
                        e.Publish<OrderConfirmed>();  // OrderConfirmed is staged to the outbox on commit (by type)
                    });
            })
            .Build();
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
        using var host = BuildHost();
        var provider = host.Services;
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
            "in the SAME transaction as the business row (a running host's poller would then relay it " +
            "to the Wolverine producer post-commit)");
    }

    [Fact]
    public async Task Rolled_back_UnitOfWork_persists_neither_the_order_nor_the_outbox_row()
    {
        using var host = BuildHost();
        var provider = host.Services;
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
