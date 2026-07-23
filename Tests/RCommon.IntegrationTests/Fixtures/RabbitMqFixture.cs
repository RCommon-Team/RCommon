using System;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;
using Xunit;

namespace RCommon.IntegrationTests.Fixtures;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management-alpine")
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
                "Could not start the RabbitMQ test container. Ensure Podman is running and DOCKER_HOST " +
                "points at the Podman socket (see Tests/RCommon.IntegrationTests/README.md).", ex);
        }
        ConnectionString = _container.GetConnectionString();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
