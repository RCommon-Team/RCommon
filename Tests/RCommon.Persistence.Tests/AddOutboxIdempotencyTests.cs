using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Guards the multi-datastore outbox registration shape (AC-9): several outbox-owning datastores must
/// share a SINGLE poller and a SINGLE routing diagnostic, while every registered datastore name is
/// accumulated in the <see cref="IOutboxDataStoreRegistry"/>. This is achieved by making
/// <see cref="OutboxPersistenceBuilderExtensions.AddOutbox{TOutboxStore}"/> idempotent for its shared
/// singletons, so calling it once per datastore is safe.
/// </summary>
public class AddOutboxIdempotencyTests
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

    private static IPersistenceBuilder NewBuilder(out IServiceCollection services)
    {
        services = new ServiceCollection();
        services.AddLogging();
        return new TestPersistenceBuilder(services);
    }

    [Fact]
    public void AddOutbox_CalledOncePerDatastore_AccumulatesBothNamesInRegistry()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Orders");
        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Billing");

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IOutboxDataStoreRegistry>();

        registry.Registrations.Should().BeEquivalentTo(new[] { "Orders", "Billing" });
    }

    [Fact]
    public void AddOutbox_CalledTwice_RegistersExactlyOnePoller()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Orders");
        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Billing");

        // The poller is registered via AddHostedService<OutboxProcessingService>() -> a type-based
        // descriptor whose ImplementationType is the concrete poller.
        var pollers = services.Count(d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(OutboxProcessingService));

        pollers.Should().Be(1, "a single poller must drain every registered datastore (AC-9)");
    }

    [Fact]
    public void AddOutbox_CalledTwice_RegistersExactlyOneRoutingDiagnostic()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Orders");
        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Billing");

        // Diagnostics are registered via TryAddEnumerable with the typed factory overload so each
        // is a shared singleton. Currently there are two diagnostics:
        //   1. OutboxRoutingDiagnosticsHostedService  — warns when IEventRouter is clobbered
        //   2. DurableRouteOutboxValidationHostedService — throws when a durable route names an
        //      unregistered outbox datastore
        // Total IHostedService count == 3 (one poller + two diagnostics) proves idempotency.
        var hostedServices = services.Count(d => d.ServiceType == typeof(IHostedService));
        var diagnostics = services.Count(d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType != typeof(OutboxProcessingService));

        hostedServices.Should().Be(3, "exactly one poller and two routing diagnostics should be registered");
        diagnostics.Should().Be(2, "each diagnostic is a shared singleton, not one-per-datastore");
    }

    [Fact]
    public void AddOutbox_CalledTwice_RegistersExactlyOneOutboxStoreBinding()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Orders");
        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Billing");

        var storeBindings = services.Count(d => d.ServiceType == typeof(IOutboxStore));

        storeBindings.Should().Be(1, "duplicate IOutboxStore bindings would double-save each outbox row");
    }
}
