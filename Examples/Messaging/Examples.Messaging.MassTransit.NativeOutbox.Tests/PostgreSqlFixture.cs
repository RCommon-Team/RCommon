using System;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace Examples.Messaging.MassTransit.NativeOutbox.Tests;

/// <summary>
/// Spins up a real Postgres container (via Podman/Docker) for the recipe 2b integration test. A local
/// copy of the pattern in Tests/RCommon.IntegrationTests/Fixtures/PostgreSqlFixture.cs — example test
/// projects cannot reference RCommon.IntegrationTests, so each carries its own fixture.
/// </summary>
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
                "points at the Podman socket " +
                "(e.g. $env:DOCKER_HOST = \"npipe://./pipe/podman-machine-default\"; " +
                "$env:TESTCONTAINERS_RYUK_DISABLED = \"true\").", ex);
        }
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSql";
}
