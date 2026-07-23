using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.EfCore.Tests;

/// <summary>
/// Covers the fail-loud startup check: for each datastore in <see cref="IOutboxDataStoreRegistry.Registrations"/>,
/// the corresponding <see cref="RCommonDbContext"/> must have <see cref="OutboxMessage"/> mapped in its model.
/// A registered outbox datastore without the mapping silently drops events, so the service throws rather than
/// letting the application start in a broken state.
/// </summary>
public class OutboxSchemaVerificationHostedServiceTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// A minimal RCommonDbContext that has OutboxMessage mapped (because UseOutboxDataStore tagged it).
    /// </summary>
    private static AutoMapDbContext BuildMappedContext(string datastoreName)
    {
        var registry = new FakeOutboxDataStoreRegistry(datastoreName);
        var options = new DbContextOptionsBuilder<AutoMapDbContext>()
            .UseSqlite("DataSource=:memory:")
            .UseOutboxDataStore(datastoreName, registry)
            .Options;
        return new AutoMapDbContext(options);
    }

    /// <summary>
    /// A minimal RCommonDbContext that does NOT have OutboxMessage mapped.
    /// </summary>
    private static AutoMapDbContext BuildUnmappedContext()
    {
        var options = new DbContextOptionsBuilder<AutoMapDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        return new AutoMapDbContext(options);
    }

    /// <summary>
    /// Builds a scoped service provider that will resolve the given context for any datastore name.
    /// </summary>
    private static IServiceProvider BuildScopedProvider(RCommonDbContext context, IOutboxDataStoreRegistry registry)
    {
        var factoryMock = new Mock<IDataStoreFactory>();
        factoryMock
            .Setup(f => f.Resolve<RCommonDbContext>(It.IsAny<string>()))
            .Returns(context);

        var services = new ServiceCollection();
        services.AddSingleton(registry);
        services.AddSingleton(factoryMock.Object);

        return services.BuildServiceProvider();
    }

    private sealed class FakeOutboxDataStoreRegistry : IOutboxDataStoreRegistry
    {
        public FakeOutboxDataStoreRegistry(params string[] names)
            => Registrations = names;

        public IReadOnlyCollection<string> Registrations { get; }
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task StartAsync_DoesNotThrow_WhenOutboxMessageIsMapped()
    {
        // Registry says "A" owns an outbox; the resolved context has OutboxMessage mapped.
        const string datastoreName = "A";
        using var context = BuildMappedContext(datastoreName);
        var registry = new FakeOutboxDataStoreRegistry(datastoreName);
        var provider = BuildScopedProvider(context, registry);

        var sut = new OutboxSchemaVerificationHostedService(provider);

        Func<Task> act = () => sut.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync(
            "a registered outbox datastore whose context maps OutboxMessage is healthy");
    }

    [Fact]
    public async Task StartAsync_Throws_WhenOutboxMessageIsNotMapped()
    {
        // Registry says "Missing" owns an outbox, but the resolved context has NO OutboxMessage mapping.
        const string datastoreName = "Missing";
        using var context = BuildUnmappedContext();
        var registry = new FakeOutboxDataStoreRegistry(datastoreName);
        var provider = BuildScopedProvider(context, registry);

        var sut = new OutboxSchemaVerificationHostedService(provider);

        Func<Task> act = () => sut.StartAsync(CancellationToken.None);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>(
            "a registered outbox datastore missing the OutboxMessage mapping must fail loud");

        ex.Which.Message.Should().Contain(datastoreName,
            "the error message must name the problematic datastore so the developer can fix it");
        ex.Which.Message.Should().Contain("OutboxMessage",
            "the error message should mention what mapping is missing");
    }

    [Fact]
    public async Task StartAsync_IsNoOp_WhenRegistryIsEmpty()
    {
        // No outbox configured at all — the service should do nothing.
        var registry = new FakeOutboxDataStoreRegistry();  // empty
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxDataStoreRegistry>(registry);
        // IDataStoreFactory deliberately NOT registered — if the service touches it for an empty
        // registry, the resolve will throw, making the bug obvious.
        var provider = services.BuildServiceProvider();

        var sut = new OutboxSchemaVerificationHostedService(provider);

        Func<Task> act = () => sut.StartAsync(CancellationToken.None);

        await act.Should().NotThrowAsync("an empty registry means no outbox is configured — no-op");
    }

    [Fact]
    public async Task StopAsync_AlwaysCompletesWithoutThrowing()
    {
        var registry = new FakeOutboxDataStoreRegistry();
        var services = new ServiceCollection();
        services.AddSingleton<IOutboxDataStoreRegistry>(registry);
        var provider = services.BuildServiceProvider();

        var sut = new OutboxSchemaVerificationHostedService(provider);

        Func<Task> act = () => sut.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
