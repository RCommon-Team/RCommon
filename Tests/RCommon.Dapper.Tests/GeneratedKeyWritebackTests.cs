using System;
using System.IO;
using System.Threading.Tasks;
using Dommel;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Sql;
using Xunit;

namespace RCommon.Dapper.Tests;

/// <summary>
/// Covers docs/specs/persistence/dapper-linq2db-generated-key-writeback.md: an entity with a
/// database-generated key must have that key populated on the instance after <c>AddAsync</c>,
/// matching how EF Core's change tracker already behaves. Uses a real SQLite connection since the
/// bug only reproduces against a real ADO provider, not the mocked unit tests elsewhere in this project.
/// </summary>
public class GeneratedKeyWritebackTests : IDisposable
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public GeneratedKeyWritebackTests()
    {
        DommelMapper.AddSqlBuilder(typeof(SqliteConnection), new SqliteSqlBuilder());

        _dbPath = Path.Combine(Path.GetTempPath(), $"rcommon-dapper-keytest-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";

        using var schemaConnection = new SqliteConnection(_connectionString);
        schemaConnection.Open();
        var createTable = schemaConnection.CreateCommand();
        createTable.CommandText = """
            CREATE TABLE Widgets (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL
            );
            CREATE TABLE ClientKeyedThings (
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
            .WithPersistence<DapperPersistenceBuilder>(dapper =>
            {
                dapper.AddDbConnection<TestDbConnection>("TestDb", options =>
                {
                    options.DbFactory = SqliteFactory.Instance;
                    options.ConnectionString = _connectionString;
                });
                dapper.SetDefaultDataStore(ds => ds.DefaultDataStoreName = "TestDb");
            });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task AddAsync_EntityWithGeneratedIntKey_PopulatesIdOnEntity()
    {
        using var provider = BuildProvider();
        var repository = provider.GetRequiredService<ISqlMapperRepository<Widget>>();

        var widget = new Widget { Name = "Test Widget" };
        widget.Id.Should().Be(0);

        await repository.AddAsync(widget);

        widget.Id.Should().BeGreaterThan(0);

        var reloaded = await repository.FindAsync(widget.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Name.Should().Be("Test Widget");
    }

    [Fact]
    public async Task AddAsync_EntityWithClientSuppliedGuidKey_LeavesKeyUntouched()
    {
        using var provider = BuildProvider();
        var repository = provider.GetRequiredService<ISqlMapperRepository<ClientKeyedThing>>();

        var thing = new ClientKeyedThing { Name = "Explicit Key" };
        var originalId = thing.Id;

        await repository.AddAsync(thing);

        thing.Id.Should().Be(originalId);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools native connections by default; clear the pool first so the
        // file handle is actually released before deleting the temp database.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private class TestDbConnection : RDbConnection
    {
        public TestDbConnection(Microsoft.Extensions.Options.IOptions<RDbConnectionOptions> options)
            : base(options)
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
