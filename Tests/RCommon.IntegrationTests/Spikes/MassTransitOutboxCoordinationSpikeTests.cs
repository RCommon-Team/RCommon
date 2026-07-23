using System;
using System.Threading.Tasks;
using FluentAssertions;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration; // MassTransit's OutboxMessage/OutboxState entities
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.IntegrationTests.Fixtures;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.IntegrationTests.Spikes;

/// <summary>
/// SPIKE (spec AC-15): does a MassTransit <c>Publish</c> issued inside RCommon's UnitOfWork
/// <see cref="System.Transactions.TransactionScope"/> stage atomically into MassTransit's EF Core
/// (bus) outbox tables?
///
/// Mechanism under test (MassTransit 8.5.9): <c>AddEntityFrameworkOutbox&lt;TDbContext&gt;(o =&gt; { o.UsePostgres(); o.UseBusOutbox(); })</c>
/// with an in-memory transport. With <c>UseBusOutbox()</c>, a scoped <see cref="IPublishEndpoint"/>
/// does NOT send to the broker; instead it stages the published message and, via a
/// SavingChanges interceptor on the SAME scoped <c>TDbContext</c>, writes an <c>OutboxMessage</c>
/// row during that DbContext's <c>SaveChangesAsync</c>. So the business row and the MT outbox row
/// are written by one <c>SaveChanges</c> on one DbContext. Whether they commit ATOMICALLY therefore
/// reduces to: does that <c>SaveChanges</c> enlist in RCommon's ambient <c>TransactionScope</c>?
/// (Npgsql auto-enlists an opened connection in an ambient <see cref="System.Transactions"/>
/// transaction, so the hypothesis is YES.)
///
/// This is a SPIKE: both PASS and FAIL are informative. Assertions are honest. If they fail, the
/// failure is the recorded finding (see the SPIKE FINDING comments and the Task 8 findings doc).
/// In-memory transport is used deliberately: the whole point of the outbox is that Publish writes
/// to the DB, not the broker, so no real RabbitMQ broker is required to exercise outbox staging.
/// </summary>
[Trait("Category", "Integration")] // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgresAndRabbitMqCollection.Name)]
public class MassTransitOutboxCoordinationSpikeTests
{
    private readonly PostgreSqlFixture _pg;

    public MassTransitOutboxCoordinationSpikeTests(PostgreSqlFixture pg, RabbitMqFixture rabbit)
    {
        // RabbitMqFixture is required by the collection ctor but intentionally unused: the bus
        // outbox stages to the DB with the in-memory transport, so no real broker is needed.
        _pg = pg;
    }

    // ---- Business entity ----
    public sealed class SpikeWidget
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // ---- Integration event published through MassTransit ----
    public sealed record SpikeIntegrationEvent(Guid Id, string Name);

    /// <summary>
    /// DbContext carrying BOTH the business entity and MassTransit's transactional outbox entities
    /// (InboxState, OutboxState, OutboxMessage) via <c>AddTransactionalOutboxEntities()</c>.
    /// Derives from <see cref="RCommonDbContext"/> so it satisfies RCommon's
    /// <c>AddDbContext&lt;TDbContext&gt; where TDbContext : RCommonDbContext</c> constraint while
    /// still being a plain EF <see cref="DbContext"/> for MassTransit's outbox.
    /// </summary>
    public sealed class SpikeDbContext : RCommonDbContext
    {
        public SpikeDbContext(DbContextOptions<SpikeDbContext> options) : base(options) { }

        public DbSet<SpikeWidget> Widgets => Set<SpikeWidget>();

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

        // RCommon persistence + UnitOfWork (real TransactionScope-backed UoW).
        services.AddRCommon()
            .WithSimpleGuidGenerator() // UnitOfWorkFactory depends on IGuidGenerator to stamp TransactionId
            .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
            .WithPersistence<EFCorePersistenceBuilder>(ef =>
            {
                // Unique per-test database (see PostgreSqlFixture.CreateUniqueDatabaseAsync) so this class
                // never targets the shared default DB and cannot collide with sibling integration classes'
                // EnsureCreated calls when they run in the same test invocation.
                ef.AddDbContext<SpikeDbContext>("SpikeDb", o => o.UseNpgsql(connectionString));
                ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "SpikeDb");
            });

        // MassTransit with its native EF Core BUS outbox, in-memory transport (no broker needed to
        // stage into the outbox table). This is the "broker's native outbox" being wrapped by
        // RCommon's ambient TransactionScope.
        services.AddMassTransit(x =>
        {
            x.AddEntityFrameworkOutbox<SpikeDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox(); // client Publish stages to OutboxMessage instead of hitting the broker
            });

            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Ensures the schema exists AND resets the tables these tests assert on. Both tests in this class
    /// share one Postgres container (collection fixture), so without an explicit reset a committed row
    /// from the atomic-commit test leaks into the rollback test and makes the assertions order-dependent.
    /// Cleaning up front makes each test independent regardless of xUnit's execution order.
    /// </summary>
    private static async Task EnsureCleanSchemaAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SpikeDbContext>();
        await ctx.Database.EnsureCreatedAsync(); // creates business + MT outbox tables
        await ctx.Set<OutboxMessage>().ExecuteDeleteAsync();
        await ctx.Widgets.ExecuteDeleteAsync();
    }

    [Fact]
    public async Task Publish_inside_RCommon_UnitOfWork_stages_atomically_into_MassTransit_outbox()
    {
        await using var provider = BuildProvider(await _pg.CreateUniqueDatabaseAsync("mtspike"));

        // NOTE: the bus is deliberately NOT started here. Bus-outbox STAGING is performed by the
        // scoped IPublishEndpoint plus the DbContext SavingChanges interceptor during SaveChanges;
        // it does not require IBusControl.StartAsync(). Starting the bus would spin up the
        // BusOutboxDeliveryService sweeper, which could deliver+DELETE the staged OutboxMessage row
        // before CountRowsAsync reads it -- a flaky 1->0 false-negative. Not starting the bus means
        // no sweeper runs, so the staged row is stable for the assertion. (The rollback test proves
        // the interceptor path also works with the bus stopped, confirming staging is independent of
        // bus start.)
        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var ctx = sp.GetRequiredService<SpikeDbContext>();
            // The scoped IPublishEndpoint is the bus-outbox endpoint (stages to the DbContext).
            var publish = sp.GetRequiredService<IPublishEndpoint>();

            using var uow = uowFactory.Create();

            ctx.Widgets.Add(new SpikeWidget { Name = "atomic" });
            await publish.Publish(new SpikeIntegrationEvent(Guid.NewGuid(), "atomic"));

            // The bus outbox writes OutboxMessage rows during THIS DbContext's SaveChanges.
            // That SaveChanges must enlist in the ambient TransactionScope for atomicity.
            await ctx.SaveChangesAsync();

            await uow.CommitAsync();
        }

        var (widgets, outboxMessages) = await CountRowsAsync(provider);

        // SPIKE FINDING (atomic commit): expect BOTH the business row and one MT outbox row.
        // If Npgsql enlisted SaveChanges in RCommon's ambient TransactionScope, both are present.
        widgets.Should().Be(1, "the business row should be committed");
        outboxMessages.Should().Be(1,
            "the MassTransit bus-outbox row should have been staged in the same SaveChanges/transaction");
    }

    [Fact]
    public async Task Publish_inside_rolled_back_UnitOfWork_persists_neither()
    {
        await using var provider = BuildProvider(await _pg.CreateUniqueDatabaseAsync("mtspike"));

        // Bus deliberately NOT started (see the atomic-commit test): staging goes through the
        // SaveChanges interceptor, not the bus delivery service.
        await EnsureCleanSchemaAsync(provider);

        using (var scope = provider.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
            var ctx = sp.GetRequiredService<SpikeDbContext>();
            var publish = sp.GetRequiredService<IPublishEndpoint>();

            using var uow = uowFactory.Create();

            ctx.Widgets.Add(new SpikeWidget { Name = "rollback" });
            await publish.Publish(new SpikeIntegrationEvent(Guid.NewGuid(), "rollback"));
            await ctx.SaveChangesAsync();

            // Deliberately do NOT commit. Disposing the UoW without CommitAsync rolls back the
            // ambient TransactionScope (AutoComplete is off by default).
        }

        var (widgets, outboxMessages) = await CountRowsAsync(provider);

        // SPIKE FINDING (rollback): expect NEITHER row to survive if SaveChanges enlisted in the
        // rolled-back TransactionScope.
        widgets.Should().Be(0, "the business row should have been rolled back");
        outboxMessages.Should().Be(0, "the MT outbox row should have been rolled back");
    }

    /// <summary>
    /// Counts business rows and MassTransit OutboxMessage rows on a FRESH scope/connection so the
    /// query reflects committed state only (no ambient transaction, no first-level cache).
    /// </summary>
    private static async Task<(int widgets, int outboxMessages)> CountRowsAsync(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SpikeDbContext>();
        var widgets = await ctx.Widgets.AsNoTracking().CountAsync();
        var outboxMessages = await ctx.Set<OutboxMessage>().AsNoTracking().CountAsync();
        return (widgets, outboxMessages);
    }
}
