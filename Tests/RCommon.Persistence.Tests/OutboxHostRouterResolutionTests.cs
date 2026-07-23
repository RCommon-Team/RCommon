using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.Persistence.Outbox;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// DI-resolution guard for the Phase-3a wiring refactor (AC-2). The <see cref="OutboxEntityEventTracker"/>
/// composes its OWN concrete <see cref="InMemoryTransactionalEventRouter"/> (the transient dispatcher) plus
/// the concrete <see cref="OutboxEventRouter"/> (durable) plus the <see cref="IEventRoutingRegistry"/>. These
/// tests prove all four constructor dependencies are satisfiable from a realistically-configured outbox host,
/// and document what <see cref="IEventRouter"/> actually resolves to in that host.
/// </summary>
public class OutboxHostRouterResolutionTests
{
    private sealed class TestPersistenceBuilder : IPersistenceBuilder
    {
        public TestPersistenceBuilder(IServiceCollection services) => Services = services;
        public IServiceCollection Services { get; }
        public IPersistenceBuilder SetDefaultDataStore(Action<DefaultDataStoreOptions> options)
        {
            Services.Configure(options);
            return this;
        }
    }

    private sealed class FakeOutboxStore : IOutboxStore
    {
        public Task SaveAsync(IOutboxMessage message, string dataStoreName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, string dataStoreName, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IOutboxMessage>>(Array.Empty<IOutboxMessage>());
        public Task MarkProcessedAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, string dataStoreName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task MarkDeadLetteredAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset, string dataStoreName, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IOutboxMessage>>(Array.Empty<IOutboxMessage>());
        public Task ReplayDeadLetterAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteProcessedAsync(TimeSpan olderThan, string dataStoreName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteDeadLetteredAsync(TimeSpan olderThan, string dataStoreName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    /// <summary>
    /// Builds a service provider that mirrors a real outbox-configured host: core event handling
    /// (AddRCommon registers EventSubscriptionManager, IEventRoutingRegistry, IEventRouter ->
    /// InMemoryTransactionalEventRouter, EventHandlingOptions, and IGuidGenerator), the persistence
    /// defaults the outbox depends on (ITenantIdAccessor, DefaultDataStoreOptions), and AddOutbox.
    /// </summary>
    private static ServiceProvider BuildOutboxHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Core event handling registrations (RCommonBuilder ctor registers: subscription manager,
        // routing registry, IEventRouter -> InMemoryTransactionalEventRouter, EventHandlingOptions).
        // WithSimpleGuidGenerator registers IGuidGenerator, which OutboxEventRouter depends on.
        services.AddRCommon().WithSimpleGuidGenerator();

        // Persistence defaults the outbox router depends on. WithPersistence normally registers these;
        // this project has no concrete persistence provider referenced, so register them directly to
        // keep the host minimal.
        services.AddOptions<DefaultDataStoreOptions>()
            .Configure(o => o.DefaultDataStoreName = "test");
        services.AddTransient<ITenantIdAccessor, NullTenantIdAccessor>();

        var builder = new TestPersistenceBuilder(services);
        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "test");

        return services.BuildServiceProvider();
    }

    [Fact]
    public void InMemoryTransactionalEventRouter_ResolvesAsItsOwnConcreteScopedType()
    {
        using var provider = BuildOutboxHost();
        using var scope = provider.CreateScope();

        Action act = () => scope.ServiceProvider.GetRequiredService<InMemoryTransactionalEventRouter>();

        act.Should().NotThrow();
    }

    [Fact]
    public void OutboxEntityEventTracker_ResolvesWithAllFourConstructorDependencies()
    {
        using var provider = BuildOutboxHost();
        using var scope = provider.CreateScope();

        // AddOutbox registers the tracker as IEntityEventTracker -> OutboxEntityEventTracker. Resolving it
        // proves inner + outboxRouter + inProcessRouter (concrete) + routingRegistry are all satisfiable.
        IEntityEventTracker? tracker = null;
        Action act = () => tracker = scope.ServiceProvider.GetRequiredService<IEntityEventTracker>();

        act.Should().NotThrow();
        tracker.Should().BeOfType<OutboxEntityEventTracker>();
    }

    [Fact]
    public void IEventRouter_IsNotTheSameInstanceAsTheConcreteInProcessRouter()
    {
        // Informative: the tracker's transient dispatcher (the concrete InMemoryTransactionalEventRouter it
        // composes) must be a separate object from whatever IEventRouter resolves to in the outbox host.
        // In this host IEventRouter actually resolves to InMemoryTransactionalEventRouter (core registers it
        // FIRST via AddScoped, so AddOutbox's TryAddScoped<IEventRouter> -> OutboxEventRouter is a no-op).
        // Even so, the concrete scoped InMemoryTransactionalEventRouter is a DISTINCT registration/instance
        // from the IEventRouter binding, which is what the concrete-injection design guarantees.
        using var provider = BuildOutboxHost();
        using var scope = provider.CreateScope();

        var eventRouter = scope.ServiceProvider.GetRequiredService<IEventRouter>();
        var concreteInProcess = scope.ServiceProvider.GetRequiredService<InMemoryTransactionalEventRouter>();

        eventRouter.Should().NotBeSameAs(concreteInProcess,
            "the tracker's composed transient dispatcher must be independent of the host's IEventRouter binding");
    }
}
