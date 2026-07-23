using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RCommon.Entities;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public class OutboxTopologySplitTests
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

    private static TestPersistenceBuilder NewBuilder(out IServiceCollection services)
    {
        services = new ServiceCollection();
        return new TestPersistenceBuilder(services);
    }

    // The two outbox diagnostics are registered with the TYPED FACTORY overload
    // (ServiceDescriptor.Singleton<IHostedService, TImpl>(sp => ...)), so their descriptor's
    // ImplementationType is null and both are indistinguishable by descriptor inspection. The only
    // reliable way to detect a specific hosted-service implementation is to BUILD the provider and
    // match on the runtime type of the resolved IHostedService instances. Building the provider
    // instantiates OutboxProcessingService when present, whose ctor requires a configured
    // DefaultDataStoreName -- BuildHostedProvider sets one so construction never throws.
    private static bool HasHostedService<T>(IServiceCollection services) where T : class
    {
        using var provider = BuildHostedProvider(services);
        return provider.GetServices<IHostedService>().Any(s => s is T);
    }

    private static ServiceProvider BuildHostedProvider(IServiceCollection services)
    {
        // Idempotent to call repeatedly on the same collection: AddLogging uses TryAdd internally, and
        // re-Configuring DefaultDataStoreOptions to the same value is harmless.
        services.AddLogging();
        services.Configure<DefaultDataStoreOptions>(o => o.DefaultDataStoreName = "test");
        return services.BuildServiceProvider();
    }

    private static bool RegistersStore(IServiceCollection services) =>
        services.Any(d => d.ServiceType == typeof(IOutboxStore));

    [Fact]
    public void AddOutboxProducer_registers_store_router_tracker_and_both_diagnostics_but_no_poller()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutboxProducer<FakeOutboxStore>(dataStoreName: "test");

        RegistersStore(services).Should().BeTrue("the producer must register the outbox store");
        services.Any(d => d.ServiceType == typeof(OutboxEventRouter)).Should().BeTrue();
        services.Any(d => d.ServiceType == typeof(IEntityEventTracker)
            && d.ImplementationType == typeof(OutboxEntityEventTracker)).Should().BeTrue();
        // The routing-clobber diagnostic is producer-only; the durable-route fail-loud validator comes
        // from the shared core. A producer host gets BOTH.
        HasHostedService<OutboxRoutingDiagnosticsHostedService>(services).Should()
            .BeTrue("the routing-clobber diagnostic is producer-side");
        HasHostedService<DurableRouteOutboxValidationHostedService>(services).Should()
            .BeTrue("the MN-3 durable-route validator is in the shared core");
        HasHostedService<OutboxProcessingService>(services).Should()
            .BeFalse("the producer must NOT register the hosted poller");
    }

    [Fact]
    public void AddOutboxProcessor_registers_store_poller_and_the_MN3_validator_but_not_the_routing_diagnostic()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutboxProcessor<FakeOutboxStore>(dataStoreName: "test");

        RegistersStore(services).Should().BeTrue("the processor needs the store to claim/mark rows");
        HasHostedService<OutboxProcessingService>(services).Should()
            .BeTrue("the processor must register the hosted poller");
        // A processor-only host MUST keep the MN-3 fail-loud durable-route validator (it is in the core)...
        HasHostedService<DurableRouteOutboxValidationHostedService>(services).Should()
            .BeTrue("the MN-3 durable-route validator is in the shared core, so processor-only hosts keep it");
        // ...but must NOT get the producer-only routing-clobber diagnostic (it would false-warn here).
        HasHostedService<OutboxRoutingDiagnosticsHostedService>(services).Should()
            .BeFalse("the routing-clobber diagnostic is producer-only and would false-warn on a processor host");
    }

    [Fact]
    public void AddOutbox_registers_both_producer_and_processor()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "test");

        services.Any(d => d.ServiceType == typeof(OutboxEventRouter)).Should().BeTrue("producer wiring");
        HasHostedService<OutboxProcessingService>(services).Should().BeTrue("processor wiring");
    }

    [Fact]
    public void AddOutbox_called_twice_registers_exactly_one_poller()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Orders");
        builder.AddOutbox<FakeOutboxStore>(dataStoreName: "Billing");

        services.Count(d => d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(OutboxProcessingService))
            .Should().Be(1, "the poller drains all datastores; there must be exactly one");
    }

    [Fact]
    public void Separate_producer_and_processor_calls_equal_AddOutbox_for_the_poller()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutboxProducer<FakeOutboxStore>(dataStoreName: "test");
        builder.AddOutboxProcessor<FakeOutboxStore>(dataStoreName: "test");

        services.Count(d => d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(OutboxProcessingService))
            .Should().Be(1);
        services.Any(d => d.ServiceType == typeof(OutboxEventRouter)).Should().BeTrue();

        // Separate producer+processor calls run the shared core twice (once each), yet must yield exactly
        // the same hosted-service shape as a single AddOutbox: one poller + two diagnostics (the MN-3
        // durable-route validator from the core lands exactly once despite the double core invocation).
        // This mirrors the load-bearing invariant in AddOutboxIdempotencyTests for the new split surface.
        var hostedServices = services.Count(d => d.ServiceType == typeof(IHostedService));
        var diagnostics = services.Count(d => d.ServiceType == typeof(IHostedService)
            && d.ImplementationType != typeof(OutboxProcessingService));
        hostedServices.Should().Be(3, "one poller + two diagnostics, even across separate producer/processor calls");
        diagnostics.Should().Be(2, "the MN-3 validator (core) and the routing-clobber diagnostic (producer), each once");
    }

    private static IOutboxDataStoreRegistry BuildRegistry(IServiceCollection services)
    {
        services.AddLogging();
        services.Configure<DefaultDataStoreOptions>(o => o.DefaultDataStoreName = "DefaultStore");
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOutboxDataStoreRegistry>();
    }

    [Fact]
    public void OnDataStore_in_configure_delegate_lands_in_the_registry()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(configure: o => o.OnDataStore("Billing"));

        BuildRegistry(services).Registrations.Should().Contain("Billing");
    }

    [Fact]
    public void Explicit_dataStoreName_parameter_wins_over_OnDataStore()
    {
        var builder = NewBuilder(out var services);

        builder.AddOutbox<FakeOutboxStore>(
            configure: o => o.OnDataStore("FromConfigure"),
            dataStoreName: "FromParameter");

        var registrations = BuildRegistry(services).Registrations;
        registrations.Should().Contain("FromParameter");
        registrations.Should().NotContain("FromConfigure");
    }
}
