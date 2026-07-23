using System;
using System.Threading.Tasks;
using FluentAssertions; // AwesomeAssertions ships the FluentAssertions namespace as a drop-in replacement
using MassTransit; // UsingInMemory + ConfigureEndpoints + IPublishEndpoint are native MassTransit
using MassTransit.EntityFrameworkCoreIntegration; // MassTransit's OutboxMessage entity
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.MassTransit;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Transactions;
using Xunit;

namespace Examples.Messaging.MassTransit.NativeOutbox.Tests;

/// <summary>
/// AC-15 e2e proof for recipe 2b (MassTransit ONLY): "DDD + UnitOfWork + broker-NATIVE outbox, RCommon-wrapped".
/// It is the runnable companion to the RCommon-level gate
/// Tests/RCommon.IntegrationTests/RecipeTwoBBrokerOutboxTests and proves the SAME behaviour through the same
/// public wrapper, relocated into this Examples project.
///
/// The mechanism: with <c>UseBusOutbox()</c> (configured by RCommon's <c>UseBrokerOutbox&lt;AppDbContext&gt;</c>
/// wrapper), a scoped <see cref="IPublishEndpoint"/> does NOT hit the broker; it stages a MassTransit
/// <c>OutboxMessage</c> row during the SAME scoped <see cref="AppDbContext"/>'s <c>SaveChangesAsync</c>. Npgsql
/// auto-enlists that SaveChanges in RCommon's ambient UnitOfWork <see cref="System.Transactions.TransactionScope"/>,
/// so the business row and the MassTransit outbox row commit — or roll back — together.
///
/// In-memory transport is used deliberately: outbox staging writes to the DB, not the broker, so no real
/// RabbitMQ broker is required. The <see cref="PostgreSqlFixture"/> alone suffices.
/// </summary>
[Trait("Category", "Integration")] // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgreSqlCollection.Name)]
public class Recipe2bNativeOutboxTests
{
    private readonly PostgreSqlFixture _pg;

    public Recipe2bNativeOutboxTests(PostgreSqlFixture pg) => _pg = pg;

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon()
            .WithSimpleGuidGenerator() // UnitOfWorkFactory depends on IGuidGenerator to stamp TransactionId
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                ef.AddDbContext<AppDbContext>("Orders", o => o.UseNpgsql(_pg.ConnectionString));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "Orders");
            })
            // Recipe 2b through the PUBLIC wrapper: MassTransit's native EF Core bus outbox.
            .WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            {
                e.UseBrokerOutbox<AppDbContext>(o => o.OnDataStore("Orders").UsePostgres());
                e.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
            });

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Ensures the schema exists AND resets the tables this test asserts on. Both tests in this class share
    /// one Postgres container (collection fixture), so without an explicit reset a committed row from one
    /// test leaks into the next and makes the assertions order-dependent. Cleaning up front makes each test
    /// independent regardless of xUnit's execution order.
    /// </summary>
    private static async Task EnsureCleanSchemaAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.EnsureCreatedAsync(); // creates Orders + MassTransit outbox tables
        await ctx.Set<OutboxMessage>().ExecuteDeleteAsync();
        await ctx.Orders.ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Publish_through_UseBrokerOutbox_inside_UnitOfWork_stages_atomically()
    {
        await using var provider = BuildProvider();

        // The bus is deliberately NOT started: bus-outbox STAGING happens via the scoped IPublishEndpoint
        // plus the DbContext SavingChanges interceptor during SaveChanges — it does not require the bus to
        // be running. Starting the bus would spin up the delivery sweeper, which could deliver+DELETE the
        // staged row before CountRowsAsync reads it (a flaky 1->0 false-negative).
        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var ctx = sp.GetRequiredService<AppDbContext>();
            var publish = sp.GetRequiredService<IPublishEndpoint>();

            using var uow = uowFactory.Create();

            ctx.Orders.Add(new Order { CustomerName = "Ada Lovelace", Total = 249.99m });
            await publish.Publish(new OrderConfirmed(Guid.NewGuid(), "Ada Lovelace", 249.99m));

            // The bus outbox writes the OutboxMessage row during THIS DbContext's SaveChanges, which must
            // enlist in the ambient TransactionScope for atomicity.
            await ctx.SaveChangesAsync();

            await uow.CommitAsync();
        }

        var (orders, outboxMessages) = await CountRowsAsync(provider);

        orders.Should().Be(1, "the business row should be committed");
        outboxMessages.Should().Be(1,
            "the wrapper's bus-outbox row should have been staged in the same SaveChanges/transaction");
    }

    [Fact]
    public async Task Publish_through_UseBrokerOutbox_inside_rolled_back_UnitOfWork_persists_neither()
    {
        await using var provider = BuildProvider();

        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var ctx = sp.GetRequiredService<AppDbContext>();
            var publish = sp.GetRequiredService<IPublishEndpoint>();

            using var uow = uowFactory.Create();

            ctx.Orders.Add(new Order { CustomerName = "Grace Hopper", Total = 99.00m });
            await publish.Publish(new OrderConfirmed(Guid.NewGuid(), "Grace Hopper", 99.00m));
            await ctx.SaveChangesAsync();

            // Deliberately do NOT commit. Disposing the UoW without CommitAsync rolls back the ambient
            // TransactionScope (AutoComplete is off by default).
        }

        var (orders, outboxMessages) = await CountRowsAsync(provider);

        orders.Should().Be(0, "the business row should have been rolled back");
        outboxMessages.Should().Be(0, "the wrapper's outbox row should have been rolled back with it");
    }

    /// <summary>
    /// Counts business rows and MassTransit OutboxMessage rows on a FRESH scope/connection so the query
    /// reflects committed state only (no ambient transaction, no first-level cache).
    /// </summary>
    private static async Task<(int orders, int outboxMessages)> CountRowsAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orders = await ctx.Orders.AsNoTracking().CountAsync();
        var outboxMessages = await ctx.Set<OutboxMessage>().AsNoTracking().CountAsync();
        return (orders, outboxMessages);
    }
}
