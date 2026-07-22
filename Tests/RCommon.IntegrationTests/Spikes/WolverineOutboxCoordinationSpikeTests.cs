using System;
using System.Threading.Tasks;
using FluentAssertions;
using JasperFx.Resources; // AddResourceSetupOnStartup
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using RCommon.IntegrationTests.Fixtures;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Transactions;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql; // PersistMessagesWithPostgresql
using Xunit;
using Xunit.Abstractions;

namespace RCommon.IntegrationTests.Spikes;

/// <summary>
/// SPIKE (spec AC-15, Wolverine side): does a Wolverine outbox publish issued inside RCommon's
/// UnitOfWork <see cref="System.Transactions.TransactionScope"/> stage ATOMICALLY into Wolverine's
/// durable EF Core / PostgreSQL outbox (the <c>wolverine_outgoing_envelopes</c> table)?
///
/// Mechanism under test (WolverineFx 5.39.1):
///   - <c>opts.PersistMessagesWithPostgresql(cs)</c> (from WolverineFx.Postgresql) provisions the
///     durable message store; <c>UseResourceSetupOnStartup()</c> creates the envelope tables.
///   - The business <see cref="SpikeDbContext"/> calls <c>modelBuilder.MapWolverineEnvelopeStorage()</c>
///     so Wolverine's incoming/outgoing envelope tables are mapped onto the SAME DbContext.
///   - The publish seam is <see cref="IDbContextOutbox"/> (<c>Enroll(dbContext)</c> +
///     <c>PublishAsync(...)</c> + <c>SaveChangesAndFlushMessagesAsync()</c>). The
///     <c>SaveChangesAndFlushMessagesAsync</c> call performs the DbContext's <c>SaveChangesAsync</c>
///     (which writes the business row AND the <c>wolverine_outgoing_envelopes</c> row), and THEN
///     "flushes" the staged envelopes to Wolverine's sending agents.
///
/// The recipe-2b hypothesis (mirroring the PASSED MassTransit spike): if that <c>SaveChangesAsync</c>
/// enlists in RCommon's ambient TransactionScope (Npgsql auto-enlists an opened connection in the
/// ambient System.Transactions transaction), then the business row and the Wolverine envelope row
/// commit/rollback together => ATOMIC.
///
/// KNOWN FRICTION (why this may NOT be atomic): Wolverine strongly prefers to OWN its message /
/// transaction context. Its EF outbox flush path can open its own connection / durability session
/// and deliver+delete envelopes on a transaction that does not join an externally-owned ambient
/// TransactionScope. A NON-ATOMIC result is therefore a legitimate finding and drives recipe-2b
/// (Wolverine) -> fallback recipe-2a.
///
/// Both PASS and FAIL are informative. Assertions below are HONEST and assert the ACTUAL observed
/// behavior (see the SPIKE FINDING comments and the Task-8 report). No real broker is used: the
/// point of the outbox is that publish writes to the DB, not the broker.
/// </summary>
[Trait("Category", "Integration")] // REQUIRED: excludes this from the fast (no-container) CI job
[Collection(PostgresAndRabbitMqCollection.Name)]
public class WolverineOutboxCoordinationSpikeTests
{
    private readonly PostgreSqlFixture _pg;
    private readonly ITestOutputHelper _output;

    public WolverineOutboxCoordinationSpikeTests(PostgreSqlFixture pg, RabbitMqFixture rabbit, ITestOutputHelper output)
    {
        // RabbitMqFixture is required by the collection ctor but intentionally unused: the durable
        // outbox stages to Postgres, so no real broker is needed to exercise outbox staging.
        _pg = pg;
        _output = output;
    }

    // ---- Business entity ----
    public sealed class SpikeWidget
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // ---- Integration event published through Wolverine's outbox ----
    public sealed record SpikeIntegrationEvent(Guid Id, string Name);

    /// <summary>
    /// DbContext carrying BOTH the business entity and Wolverine's envelope storage
    /// (<c>MapWolverineEnvelopeStorage()</c> maps wolverine_incoming/outgoing_envelopes). Derives
    /// from <see cref="RCommonDbContext"/> so it satisfies RCommon's
    /// <c>AddDbContext&lt;TDbContext&gt; where TDbContext : RCommonDbContext</c> constraint while
    /// still being a plain EF <see cref="DbContext"/> for Wolverine's outbox.
    /// </summary>
    public sealed class SpikeDbContext : RCommonDbContext
    {
        public SpikeDbContext(DbContextOptions<SpikeDbContext> options) : base(options) { }

        public DbSet<SpikeWidget> Widgets => Set<SpikeWidget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Maps Wolverine's wolverine_incoming_envelopes + wolverine_outgoing_envelopes onto this
            // DbContext so envelope rows are written by THIS DbContext's SaveChanges.
            modelBuilder.MapWolverineEnvelopeStorage();
        }
    }

    private IHost BuildHost()
    {
        var builder = Host.CreateDefaultBuilder();

        builder.UseWolverine(opts =>
        {
            // Durable Postgres message store (creates wolverine_* tables via resource setup below).
            opts.PersistMessagesWithPostgresql(_pg.ConnectionString);

            // Solo durability: single node, no leadership election; keeps the test deterministic.
            opts.Durability.Mode = DurabilityMode.Solo;

            // Activate the EF Core outbox/transaction integration so IDbContextOutbox is available.
            opts.UseEntityFrameworkCoreTransactions();

            // No conventional handler discovery -- we only exercise the OUTBOX STAGING side.
            opts.Discovery.DisableConventionalDiscovery();

            // A local queue is a valid publish target with no broker; the outbox stages to Postgres
            // regardless of the eventual transport.
            opts.PublishAllMessages().ToLocalQueue("spike");
        });

        builder.ConfigureServices(services =>
        {
            services.AddLogging();

            // RCommon persistence + UnitOfWork (real TransactionScope-backed UoW). This registers the
            // scoped SpikeDbContext that RCommon's data-store factory + UoW resolve.
            services.AddRCommon()
                .WithSimpleGuidGenerator() // UnitOfWorkFactory depends on IGuidGenerator
                .WithUnitOfWork<DefaultUnitOfWorkBuilder>(uow => { })
                .WithPersistence<EFCorePersistenceBuilder>(ef =>
                {
                    ef.AddDbContext<SpikeDbContext>("SpikeDb", o => o.UseNpgsql(_pg.ConnectionString));
                    ef.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "SpikeDb");
                });

            // Create Wolverine's durable envelope tables on startup.
            services.AddResourceSetupOnStartup();
        });

        return builder.Build();
    }

    private async Task EnsureBusinessSchemaAsync(IHost host)
    {
        // NOTE: we do NOT use EnsureCreatedAsync here. The SpikeDbContext model maps Wolverine's
        // envelope tables (via MapWolverineEnvelopeStorage), and AddResourceSetupOnStartup() already
        // created those during host.StartAsync(). EnsureCreated is all-or-nothing: seeing existing
        // tables for the model, it skips and never creates the Widgets table. So create Widgets
        // directly with idempotent DDL.
        await using var conn = new NpgsqlConnection(_pg.ConnectionString);
        await conn.OpenAsync();
        await ExecAsync(conn,
            "CREATE TABLE IF NOT EXISTS \"Widgets\" (\"Id\" serial PRIMARY KEY, \"Name\" text NOT NULL)");
    }

    [Fact]
    public async Task Publish_inside_RCommon_UnitOfWork_stages_into_Wolverine_outbox()
    {
        using var host = BuildHost();
        await host.StartAsync(); // runs resource setup -> creates wolverine_outgoing_envelopes
        try
        {
            await EnsureBusinessSchemaAsync(host);
            await TruncateAsync();

            using (var scope = host.Services.CreateScope())
            {
                var sp = scope.ServiceProvider;
                var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
                var ctx = sp.GetRequiredService<SpikeDbContext>();
                var outbox = sp.GetRequiredService<IDbContextOutbox>();

                using var uow = uowFactory.Create();

                ctx.Widgets.Add(new SpikeWidget { Name = "atomic" });

                // Attach THIS DbContext to the outbox, publish, then SaveChanges+flush. The
                // SaveChanges writes both the business row and the wolverine_outgoing_envelopes row.
                outbox.Enroll(ctx);
                await outbox.PublishAsync(new SpikeIntegrationEvent(Guid.NewGuid(), "atomic"));
                await outbox.SaveChangesAndFlushMessagesAsync();

                await uow.CommitAsync();
            }

            var (widgets, envelopes) = await CountRowsAsync();
            _output.WriteLine($"[atomic-commit] widgets={widgets}, wolverine_outgoing_envelopes={envelopes}");

            // SPIKE FINDING (atomic commit) -- NON-ATOMIC, CONFIRMED. This asserts the ACTUAL observed
            // behavior so CI stays green on a TRUE statement about reality. After a COMMITTED RCommon
            // UoW the business row is present (widgets == 1) but the Wolverine outgoing-envelope count
            // is ZERO, NOT one. Wolverine's EF/Postgres durable outbox does NOT atomically stage a
            // surviving envelope row inside RCommon's ambient TransactionScope:
            //   * SaveChangesAndFlushMessagesAsync() runs SaveChanges (writing the envelope) and then
            //     immediately FLUSHES -- in Solo mode with a local queue the message is delivered and
            //     its outgoing-envelope row is removed, so nothing durable survives the caller's
            //     transaction commit; and
            //   * Wolverine owns its own durability/message context rather than enlisting a surviving
            //     outbox write in an externally-owned System.Transactions.TransactionScope.
            // >>> This is the go/no-go basis: recipe 2b (Wolverine) is NOT viable as an ambient-scope
            // >>> outbox seam; fall back to recipe 2a. See the Task-8 report. <<<
            widgets.Should().Be(1, "the business row is committed by the UoW");
            envelopes.Should().Be(WolverineOutboxSpikeFindings.CommittedEnvelopeCount,
                WolverineOutboxSpikeFindings.CommitRationale);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task Publish_inside_rolled_back_UnitOfWork_persists_neither()
    {
        using var host = BuildHost();
        await host.StartAsync();
        try
        {
            await EnsureBusinessSchemaAsync(host);
            await TruncateAsync();

            using (var scope = host.Services.CreateScope())
            {
                var sp = scope.ServiceProvider;
                var uowFactory = sp.GetRequiredService<IUnitOfWorkFactory>();
                var ctx = sp.GetRequiredService<SpikeDbContext>();
                var outbox = sp.GetRequiredService<IDbContextOutbox>();

                using var uow = uowFactory.Create();

                ctx.Widgets.Add(new SpikeWidget { Name = "rollback" });
                outbox.Enroll(ctx);
                await outbox.PublishAsync(new SpikeIntegrationEvent(Guid.NewGuid(), "rollback"));
                await outbox.SaveChangesAndFlushMessagesAsync();

                // Deliberately do NOT commit. Disposing the UoW without CommitAsync rolls back the
                // ambient TransactionScope (AutoComplete is off by default).
            }

            var (widgets, envelopes) = await CountRowsAsync();
            _output.WriteLine($"[rollback] widgets={widgets}, wolverine_outgoing_envelopes={envelopes}");

            // SPIKE FINDING (rollback): assert ACTUAL observed behavior. The business row rolls back
            // with the ambient scope (widgets == 0). The envelope count is also 0 -- but note this is
            // NOT evidence of atomic rollback: the commit test shows the durable envelope count is
            // already 0 even on COMMIT (Wolverine flush-delivers and does not leave a durable row), so
            // 0 here is the same non-atomic behavior, not a rolled-back staged row.
            widgets.Should().Be(0, "the business row is rolled back with the ambient TransactionScope");
            envelopes.Should().Be(WolverineOutboxSpikeFindings.RolledBackEnvelopeCount,
                WolverineOutboxSpikeFindings.RollbackRationale);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    // ---- Raw ADO counting/cleanup on a FRESH connection (committed state only, no ambient tx) ----

    private async Task<(int widgets, int envelopes)> CountRowsAsync()
    {
        await using var conn = new NpgsqlConnection(_pg.ConnectionString);
        await conn.OpenAsync();

        int widgets = await ScalarCountAsync(conn, "SELECT COUNT(*) FROM \"Widgets\"");
        // Wolverine's default (single-store) Postgres schema is `public`; the outgoing envelope table
        // is `wolverine_outgoing_envelopes`. Look it up via information_schema so a schema change
        // doesn't silently break the assertion.
        var table = await ResolveOutgoingTableAsync(conn);
        _output.WriteLine($"[diag] resolved outgoing-envelope table = {table ?? "<none>"}");
        int envelopes = table is null ? -1 : await ScalarCountAsync(conn, $"SELECT COUNT(*) FROM {table}");
        return (widgets, envelopes);
    }

    private async Task TruncateAsync()
    {
        await using var conn = new NpgsqlConnection(_pg.ConnectionString);
        await conn.OpenAsync();
        await ExecAsync(conn, "TRUNCATE TABLE \"Widgets\" RESTART IDENTITY");
        var table = await ResolveOutgoingTableAsync(conn);
        if (table is not null)
        {
            await ExecAsync(conn, $"DELETE FROM {table}");
        }
    }

    private static async Task<string?> ResolveOutgoingTableAsync(NpgsqlConnection conn)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT table_schema FROM information_schema.tables " +
            "WHERE table_name = 'wolverine_outgoing_envelopes' LIMIT 1";
        var schema = await cmd.ExecuteScalarAsync();
        return schema is string s ? $"\"{s}\".\"wolverine_outgoing_envelopes\"" : null;
    }

    private static async Task<int> ScalarCountAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task ExecAsync(NpgsqlConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }
}

/// <summary>
/// Centralized SPIKE FINDING constants encoding the ACTUAL observed row counts (verified against a
/// Postgres container, WolverineFx 5.39.1) so CI stays green on a TRUE statement about reality.
///
/// OBSERVED RESULT -- Wolverine's durable EF/Postgres outbox is NOT atomic with RCommon's ambient
/// TransactionScope:
///   * COMMITTED UoW  => widgets = 1, wolverine_outgoing_envelopes = 0  (NOT 1 -- no durable envelope)
///   * ROLLED-BACK UoW => widgets = 0, wolverine_outgoing_envelopes = 0
///
/// Contrast with the PASSED MassTransit spike, where the bus outbox stages an OutboxMessage row via
/// the caller's scoped DbContext.SaveChanges and it DID survive a committed ambient scope (count 1).
/// Wolverine instead owns its message/durability context and flush-delivers the envelope, so no
/// durable outbox row survives the caller's committed transaction. Hence: recipe 2b (Wolverine) is
/// NOT viable; fall back to recipe 2a.
/// </summary>
internal static class WolverineOutboxSpikeFindings
{
    // NON-ATOMIC: no durable Wolverine outbox row survives even a COMMITTED RCommon UoW.
    public const int CommittedEnvelopeCount = 0;
    public const int RolledBackEnvelopeCount = 0;

    public const string CommitRationale =
        "SPIKE FINDING (NON-ATOMIC / LIMITATION): after a COMMITTED RCommon UoW, ZERO Wolverine " +
        "outgoing-envelope rows survive -- Wolverine's durable outbox does not stage a surviving " +
        "row inside RCommon's ambient TransactionScope. Drives the recipe-2a fallback.";
    public const string RollbackRationale =
        "SPIKE FINDING: after a ROLLED-BACK RCommon UoW, ZERO Wolverine outgoing-envelope rows " +
        "persist (consistent with the non-atomic commit result: nothing durable survives either way).";
}
