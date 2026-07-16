using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Linq2Db;
using Xunit;

namespace RCommon.Linq2Db.Tests;

/// <summary>
/// Covers docs/specs/persistence/dapper-linq2db-generated-key-writeback.md for the Linq2Db provider.
/// Unlike Dommel (used by the Dapper provider), LinqToDB does not infer an identity/generated column
/// from the "Id" naming convention -- it requires an explicit mapping (attribute or, as used here,
/// fluent mapping via <see cref="FluentMappingBuilder"/>). Once that mapping is in place,
/// Linq2DbGeneratedKeyHelper is what makes the generated value round-trip back onto the entity.
/// </summary>
public class GeneratedKeyWritebackTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public GeneratedKeyWritebackTests()
    {
        // Widget's fluent mapping (table name, identity key) is registered by this assembly's
        // ModuleInitializer, not here -- see ModuleInitializer.cs for why it must happen at module
        // load time rather than lazily in this constructor.
        _dbPath = Path.Combine(Path.GetTempPath(), $"rcommon-linq2db-keytest-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";

        using var schemaConnection = new SqliteConnection(_connectionString);
        schemaConnection.Open();
        var createTable = schemaConnection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE Widgets (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );
            CREATE TABLE ClientKeyedThing (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL
            );
            """;
        createTable.ExecuteNonQuery();
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRCommon()
            .WithPersistence<Linq2DbPersistenceBuilder>(linq2db =>
            {
                linq2db.AddDataConnection<TestDataConnection>("TestDb",
                    (sp, options) => options.UseSQLite(_connectionString));
                linq2db.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "TestDb");
            });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AddAsync_EntityWithMappedIdentityKey_PopulatesIdOnEntity()
    {
        using var provider = BuildProvider();
        var repository = provider.GetRequiredService<IWriteOnlyRepository<Widget>>();
        var readRepository = provider.GetRequiredService<IReadOnlyRepository<Widget>>();

        var widget = new Widget { Name = "First" };
        widget.Id.Should().Be(0);

        await repository.AddAsync(widget);
        widget.Id.Should().BeGreaterThan(0);

        var second = new Widget { Name = "Second" };
        await repository.AddAsync(second);
        second.Id.Should().BeGreaterThan(widget.Id);

        // Linq2DbRepository.FindAsync(object primaryKey) is a separate, pre-existing unimplemented
        // gap (unrelated to this fix) -- use the predicate overload instead to confirm the row landed.
        var reloaded = await readRepository.FindSingleOrDefaultAsync(w => w.Id == widget.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Name.Should().Be("First");
    }

    [Fact]
    public async Task AddAsync_EntityWithClientSuppliedGuidKey_LeavesKeyUntouched()
    {
        using var provider = BuildProvider();
        var repository = provider.GetRequiredService<IWriteOnlyRepository<ClientKeyedThing>>();

        var thing = new ClientKeyedThing { Name = "Explicit Key" };
        var originalId = thing.Id;

        await repository.AddAsync(thing);

        thing.Id.Should().Be(originalId);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private class TestDataConnection : RCommonDataConnection
    {
        public TestDataConnection(LinqToDB.DataOptions options) : base(options)
        {
        }
    }

    public class Widget : BusinessEntity<int>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ClientKeyedThing : BusinessEntity<Guid>
    {
        public ClientKeyedThing() : base(Guid.NewGuid())
        {
        }

        public string Name { get; set; } = string.Empty;
    }
}
