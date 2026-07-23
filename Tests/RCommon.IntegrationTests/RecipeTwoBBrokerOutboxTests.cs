using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration; // MassTransit's OutboxMessage entity
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.IntegrationTests.Fixtures;
using RCommon.MassTransit;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.IntegrationTests;

/// <summary>
/// AC-15 gate for recipe 2b (broker-native outbox): proves that the PUBLIC
/// <see cref="MassTransitOutboxBuilderExtensions.UseBrokerOutbox{TDbContext}"/> wrapper — driven through
/// the normal RCommon pipeline (<c>WithEventHandling&lt;MassTransitEventHandlingBuilder&gt;</c>) — yields the
/// SAME atomic staging as the raw <c>AddEntityFrameworkOutbox</c> wiring proven in
/// <c>Spikes/MassTransitOutboxCoordinationSpikeTests</c>.
///
/// The mechanism (MassTransit 8.5.9): with <c>UseBusOutbox()</c>, a scoped <see cref="IPublishEndpoint"/>
/// does not hit the broker; it stages an <c>OutboxMessage</c> row during the SAME scoped DbContext's
/// <c>SaveChangesAsync</c>. Npgsql auto-enlists that SaveChanges in RCommon's ambient UnitOfWork
/// <see cref="System.Transactions.TransactionScope"/>, so the business row and the outbox row commit — or
/// roll back — together. This test promotes the raw-wiring spike into a test of the actual public API.
///
/// In-memory transport is used deliberately: outbox staging writes to the DB, not the broker, so no real
/// RabbitMQ broker is required. The <see cref="PostgreSqlFixture"/> alone suffices.
/// </summary>
[Trait("Category", "Integration")] // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgreSqlCollection.Name)]
public class RecipeTwoBBrokerOutboxTests
{
    private readonly PostgreSqlFixture _pg;

    public RecipeTwoBBrokerOutboxTests(PostgreSqlFixture pg) => _pg = pg;

    // ---- Business entity ----
    public sealed class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // ---- Integration event published through MassTransit ----
    public sealed record RecipeIntegrationEvent(Guid Id, string Name);

    /// <summary>
    /// DbContext carrying BOTH the business entity and MassTransit's transactional outbox entities. It must
    /// map the outbox entities via <c>AddTransactionalOutboxEntities()</c> — the wrapper cannot inject that.
    /// Derives from <see cref="RCommonDbContext"/> to satisfy RCommon's <c>AddDbContext</c> constraint while
    /// remaining a plain EF <see cref="DbContext"/> for MassTransit's outbox.
    /// </summary>
    public sealed class RecipeDbContext : RCommonDbContext
    {
        public RecipeDbContext(DbContextOptions<RecipeDbContext> options) : base(options) { }

        public DbSet<Widget> Widgets => Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.AddTransactionalOutboxEntities(); // InboxState + OutboxState + OutboxMessage
        }
    }

    private ServiceProvider BuildProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon()
            .WithSimpleGuidGenerator() // UnitOfWorkFactory depends on IGuidGenerator to stamp TransactionId
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                // Unique per-test database (see PostgreSqlFixture.CreateUniqueDatabaseAsync) so this class
                // never targets the shared default DB and cannot collide with sibling integration classes'
                // EnsureCreated calls when they run in the same test invocation.
                ef.AddDbContext<RecipeDbContext>("RecipeDb", o => o.UseNpgsql(connectionString));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "RecipeDb");
            })
            // Recipe 2b through the PUBLIC wrapper: no raw AddEntityFrameworkOutbox here.
            .WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            {
                e.UseBrokerOutbox<RecipeDbContext>(o => o.OnDataStore("RecipeDb").UsePostgres());
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
        var ctx = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
        await ctx.Database.EnsureCreatedAsync(); // creates business + MT outbox tables
        await ctx.Set<OutboxMessage>().ExecuteDeleteAsync();
        await ctx.Widgets.ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Publish_through_UseBrokerOutbox_inside_UnitOfWork_stages_atomically()
    {
        await using var provider = BuildProvider(await _pg.CreateUniqueDatabaseAsync("recipe2b"));

        // The bus is deliberately NOT started: bus-outbox STAGING happens via the scoped IPublishEndpoint
        // plus the DbContext SavingChanges interceptor during SaveChanges — it does not require the bus to
        // be running. Starting the bus would spin up the delivery sweeper, which could deliver+DELETE the
        // staged row before CountRowsAsync reads it (a flaky 1->0 false-negative). See the spike for detail.
        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var ctx = sp.GetRequiredService<RecipeDbContext>();
            var publish = sp.GetRequiredService<IPublishEndpoint>();

            using var uow = uowFactory.Create();

            ctx.Widgets.Add(new Widget { Name = "atomic" });
            await publish.Publish(new RecipeIntegrationEvent(Guid.NewGuid(), "atomic"));

            // The bus outbox writes the OutboxMessage row during THIS DbContext's SaveChanges, which must
            // enlist in the ambient TransactionScope for atomicity.
            await ctx.SaveChangesAsync();

            await uow.CommitAsync();
        }

        var (widgets, outboxMessages) = await CountRowsAsync(provider);

        widgets.Should().Be(1, "the business row should be committed");
        outboxMessages.Should().Be(1,
            "the wrapper's bus-outbox row should have been staged in the same SaveChanges/transaction");
    }

    [Fact]
    public async Task Publish_through_UseBrokerOutbox_inside_rolled_back_UnitOfWork_persists_neither()
    {
        await using var provider = BuildProvider(await _pg.CreateUniqueDatabaseAsync("recipe2b"));

        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var ctx = sp.GetRequiredService<RecipeDbContext>();
            var publish = sp.GetRequiredService<IPublishEndpoint>();

            using var uow = uowFactory.Create();

            ctx.Widgets.Add(new Widget { Name = "rollback" });
            await publish.Publish(new RecipeIntegrationEvent(Guid.NewGuid(), "rollback"));
            await ctx.SaveChangesAsync();

            // Deliberately do NOT commit. Disposing the UoW without CommitAsync rolls back the ambient
            // TransactionScope (AutoComplete is off by default).
        }

        var (widgets, outboxMessages) = await CountRowsAsync(provider);

        widgets.Should().Be(0, "the business row should have been rolled back");
        outboxMessages.Should().Be(0, "the wrapper's outbox row should have been rolled back with it");
    }

    /// <summary>
    /// Counts business rows and MassTransit OutboxMessage rows on a FRESH scope/connection so the query
    /// reflects committed state only (no ambient transaction, no first-level cache).
    /// </summary>
    private static async Task<(int widgets, int outboxMessages)> CountRowsAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
        var widgets = await ctx.Widgets.AsNoTracking().CountAsync();
        var outboxMessages = await ctx.Set<OutboxMessage>().AsNoTracking().CountAsync();
        return (widgets, outboxMessages);
    }
}
