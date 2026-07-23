using System;
using System.Threading.Tasks;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace RCommon.IntegrationTests.Fixtures;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Fail loud with an actionable message if no container runtime is reachable.
        try
        {
            await _container.StartAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Could not start the Postgres test container. Ensure Podman is running and DOCKER_HOST " +
                "points at the Podman socket (see Tests/RCommon.IntegrationTests/README.md).", ex);
        }
        ConnectionString = _container.GetConnectionString();
    }

    /// <summary>
    /// Creates a fresh, uniquely-named Postgres database on the shared container and returns a
    /// connection string targeting it.
    ///
    /// WHY THIS EXISTS: several integration test classes live in the SAME xUnit collection and share
    /// ONE Postgres container. If more than one of them points a schema-creating DbContext at the
    /// shared DEFAULT database and calls <c>Database.EnsureCreatedAsync()</c>, they collide:
    /// EnsureCreated is all-or-nothing, so once ANY class creates its tables on that default DB, every
    /// other class's <c>EnsureCreatedAsync</c> sees existing tables and creates NOTHING — leaving those
    /// classes' tables absent and their queries failing with <c>42P01: relation ... does not exist</c>
    /// (only when the classes run together in one <c>dotnet test</c> invocation, not in isolation).
    /// Giving each schema-creating context its OWN unique database keeps them fully isolated.
    /// </summary>
    public async Task<string> CreateUniqueDatabaseAsync(string prefix = "db")
    {
        var name = $"{prefix}_{Guid.NewGuid():N}";

        // CREATE DATABASE cannot run inside a transaction; use a plain admin connection on the default DB.
        await using var admin = new NpgsqlConnection(ConnectionString);
        await admin.OpenAsync();
        await using var cmd = admin.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{name}\";";
        await cmd.ExecuteNonQueryAsync();

        return new NpgsqlConnectionStringBuilder(ConnectionString) { Database = name }.ConnectionString;
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
