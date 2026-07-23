# Event Handling Phase 3c — Outbox Producer/Processor Topology Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Split the monolithic `AddOutbox<TOutboxStore>` registration into first-class `AddOutboxProducer` (store/router/tracker, no hosted poller) and `AddOutboxProcessor` (hosted poller) entry points, with `AddOutbox` = producer + processor, and add the AC-named `OnDataStore(...)` fluent form for datastore scoping. (Spec AC-21.)

**Architecture:** A single private `AddOutboxCore<TOutboxStore>` helper registers the shared, idempotent core (store, serializer, options, backoff, datastore-name enqueue, registry). `AddOutboxProducer` layers the producer-side wiring (routers, trackers, durable-route validation, routing diagnostics) on top of the core; `AddOutboxProcessor` layers the `OutboxProcessingService` hosted poller on top of the core. `AddOutbox` composes both — the shared core registers once via `Try*`/dedup idempotency. `OnDataStore(name)` on `OutboxOptions` is a config-time way to name the owning datastore; the positional `dataStoreName` parameter is preserved for back-compat and wins when both are supplied. This is a pure decomposition + additive API: single-host `AddOutbox` behavior (including `ImmediateDispatch = true`) is unchanged.

**Tech Stack:** .NET (net8/9/10 for Src), xUnit 2.9.3, AwesomeAssertions 7.2.1 (namespace `FluentAssertions`), Microsoft.Extensions.DependencyInjection / Options / Hosting.

---

## Context for the implementer

You are working on branch `feature/event-handling-outbox-recipes` (do NOT switch branches, do NOT commit to main). TDD is mandatory: write the failing test, watch it fail, minimal code to pass, refactor. Never sign commits with a Claude/AI signature or co-author line.

### The spec contract (AC-21)

> **AC-21 (producer/processor topology):** First-class `AddOutboxProducer` (store/router/tracker, no hosted poller) and `AddOutboxProcessor` (hosted poller) registration methods exist alongside `AddOutbox` (= producer + processor). Each is datastore-scoped (`OnDataStore(...)`), consolidating the multi-host topology into this release's registration rework.

### Why this split matters (multi-host topology)

- A **producer-only host** writes events to the outbox inside the business transaction but does NOT run the poller. It calls `AddOutboxProducer` and should set `OutboxOptions.ImmediateDispatch = false` (see the existing XML-doc on `ImmediateDispatch` — an immediate producer-side dispatch would mark a row processed before a remote subscriber sees it).
- A **processor-only host** runs the poller that drains/relays outbox rows. It calls `AddOutboxProcessor`.
- A **single host** does both. It calls `AddOutbox` (= producer + processor).

### The current monolith — what `AddOutbox<TOutboxStore>` registers today

Read `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs` first. It currently registers, in one method:

**Shared core** (needed by both producer and processor):
- `IOutboxStore` → `TOutboxStore` (scoped, `TryAddScoped`)
- `IOutboxSerializer` → `JsonOutboxSerializer` (singleton, `TryAddSingleton`)
- `IBackoffStrategy` → `ExponentialBackoffStrategy` (singleton, `TryAddSingleton`, factory reads `OutboxOptions`)
- `OutboxOptions` via `services.Configure(configure)` (or empty configure)
- `IOutboxDataStoreRegistry` → `OutboxDataStoreRegistry` (singleton, `TryAddSingleton`)
- `services.Configure<OutboxDataStoreRegistrationOptions>(o => o.Names.Add(dataStoreName))` — enqueues the owning datastore name (null = "use default")
- `DurableRouteOutboxValidationHostedService` (singleton `IHostedService` via `TryAddEnumerable` keyed on impl type — startup fail-loud when a durable route names a datastore with no registered outbox). **This is the MN-3 fail-loud guard and belongs in the core, NOT producer-only:** a processor-only host must also catch a durable route pointing at a missing outbox datastore at startup. Placing it in the core means every topology (`AddOutbox`, producer-only, processor-only) keeps the validation, while `TryAddEnumerable` dedup + the composed `AddOutbox` running the core twice still yields exactly one instance.

**Producer-side only** (writing rows / dispatch pipeline):
- `OutboxEventRouter` (scoped, `TryAddScoped`)
- `IEventRouter` → `OutboxEventRouter` forwarder (scoped, `TryAddScoped`)
- `InMemoryTransactionalEventRouter` (scoped, `TryAddScoped` — the concrete transient dispatcher the tracker composes)
- `InMemoryEntityEventTracker` (scoped, `TryAddScoped`)
- `IEntityEventTracker` → `OutboxEntityEventTracker` (scoped, `TryAddScoped`)
- `OutboxRoutingDiagnosticsHostedService` (singleton `IHostedService` via `TryAddEnumerable` keyed on impl type — warns if a later registration overrode the outbox `IEventRouter`). **This stays producer-only:** it inspects the producer's `IEventRouter` → `OutboxEventRouter` binding, which a processor-only host never registers; running it on a processor-only host would emit a spurious "outbox router was clobbered" warning.

**Processor-side only** (polling/relaying):
- `OutboxProcessingService` via `services.AddHostedService<OutboxProcessingService>()` (uses `TryAddEnumerable` keyed on impl type → exactly one poller even if called twice)

> **Load-bearing existing guard:** `Tests/RCommon.Persistence.Tests/AddOutboxIdempotencyTests.cs` hard-asserts that after two `AddOutbox` calls there are exactly **3** `IHostedService` registrations (1 poller + 2 diagnostics) and **1** `IOutboxStore` binding. The split MUST preserve this: composed `AddOutbox` = core (1 durable-route validator) + producer (1 routing diagnostic) + processor (1 poller) = 3 hosted services. Do NOT modify this test; it is a regression guard for the refactor.

> **Idempotency is already the design.** Every registration above is `Try*`/`TryAddEnumerable`, and `OutboxDataStoreRegistry.Registrations` deduplicates names case-insensitively via a `HashSet`. That is why `AddOutbox = AddOutboxProducer + AddOutboxProcessor` is safe even though the shared core is registered twice — the second run no-ops, and the doubled `Names.Add` dedupes to one entry (the poller never double-drains a datastore). Verify this claim by reading `OutboxDataStoreRegistry.Registrations`.

### The `OnDataStore` name-resolution subtlety

The datastore name is consumed at **registration time** (it is enqueued into `OutboxDataStoreRegistrationOptions.Names` synchronously inside the extension method), NOT purely at runtime. But `OnDataStore` is set inside the `configure` delegate (`Action<OutboxOptions>`), which normally runs later via `services.Configure`. To bridge this: run the `configure` delegate eagerly against a throwaway probe `OutboxOptions` to extract `DataStoreName`, resolve the effective name as `dataStoreName ?? probe.DataStoreName`, and enqueue that. Still call `services.Configure(configure)` so runtime `OutboxOptions` (PollingInterval, ImmediateDispatch, etc.) are applied. Config delegates are side-effect-free w.r.t. the options object, so invoking twice is safe.

### Files you will touch

- Modify: `Src/RCommon.Persistence/Outbox/OutboxOptions.cs` — add `DataStoreName` + `OnDataStore`.
- Modify: `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs` — extract core, add producer/processor, recompose `AddOutbox`.
- Test: `Tests/RCommon.Persistence.Tests/OutboxOptionsTests.cs` (create or extend).
- Test: `Tests/RCommon.Persistence.Tests/OutboxTopologySplitTests.cs` (create).

Do NOT change `OutboxProcessingService`, the routers, the trackers, or the registry — this is a registration refactor only. Do NOT change any existing call site (the positional `dataStoreName` param stays). Existing tests must stay green unchanged: `AddOutboxIdempotencyTests`, `OutboxHostRouterResolutionTests`, `RouteDrivenOutboxPipelineTests` (in `Tests/RCommon.Persistence.Tests/`), and the integration-lane `CrossDataStoreOutboxTests` (in `Tests/RCommon.IntegrationTests/`, run only on the Podman harness).

### Build / test commands

- Build a single project: `dotnet build Src/RCommon.Persistence/RCommon.Persistence.csproj`
- Run the persistence test project (fast lane — no containers): `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "Category!=Integration"`
- Full fast lane (before finishing): `dotnet test Src/RCommon.sln --filter "Category!=Integration"`

---

## Task 1: `OnDataStore` fluent form on `OutboxOptions`

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxOptions.cs`
- Test: `Tests/RCommon.Persistence.Tests/OutboxOptionsTests.cs` (create if absent)

- [ ] **Step 1: Write the failing tests**

Add to `OutboxOptionsTests.cs`:

```csharp
using System;
using FluentAssertions;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public class OutboxOptionsTests
{
    [Fact]
    public void OnDataStore_sets_DataStoreName_and_returns_same_instance_for_chaining()
    {
        var options = new OutboxOptions();

        var returned = options.OnDataStore("Billing");

        options.DataStoreName.Should().Be("Billing");
        returned.Should().BeSameAs(options);
    }

    [Fact]
    public void DataStoreName_is_null_by_default()
    {
        new OutboxOptions().DataStoreName.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OnDataStore_rejects_null_empty_or_whitespace(string? bad)
    {
        var options = new OutboxOptions();

        Action act = () => options.OnDataStore(bad!);

        act.Should().Throw<ArgumentException>();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~OutboxOptionsTests"`
Expected: FAIL to compile (`DataStoreName` / `OnDataStore` do not exist).

- [ ] **Step 3: Implement `OnDataStore` on `OutboxOptions`**

Add to `OutboxOptions.cs`:

```csharp
/// <summary>
/// The name of the datastore that owns this outbox table. <c>null</c> means "use the configured
/// default datastore". Set via <see cref="OnDataStore"/>.
/// </summary>
public string? DataStoreName { get; private set; }

/// <summary>
/// Names the datastore that owns this outbox table (AC-21). Fluent, returns this instance.
/// </summary>
/// <param name="dataStoreName">The owning datastore name. Must not be null, empty, or whitespace.</param>
public OutboxOptions OnDataStore(string dataStoreName)
{
    if (string.IsNullOrWhiteSpace(dataStoreName))
        throw new ArgumentException("Data store name must not be null, empty, or whitespace.", nameof(dataStoreName));

    DataStoreName = dataStoreName;
    return this;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~OutboxOptionsTests"`
Expected: PASS (all 5 cases).

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/OutboxOptions.cs Tests/RCommon.Persistence.Tests/OutboxOptionsTests.cs
git commit -m "feat(outbox): add OnDataStore fluent form to OutboxOptions (AC-21)"
```

---

## Task 2: Split into `AddOutboxProducer` / `AddOutboxProcessor`; recompose `AddOutbox`

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs`
- Test: `Tests/RCommon.Persistence.Tests/OutboxTopologySplitTests.cs` (create)

This task decomposes the monolith. Work strictly TDD — one failing test at a time.

- [ ] **Step 1: Write the first failing test — producer registers no poller**

Create `OutboxTopologySplitTests.cs`. Assertions inspect `ServiceCollection` descriptors directly (no provider build needed, so no `DefaultDataStoreOptions` required). Use the same `TestPersistenceBuilder` + `FakeOutboxStore` pattern as `OutboxHostRouterResolutionTests.cs`.

```csharp
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
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~OutboxTopologySplitTests"`
Expected: FAIL to compile (`AddOutboxProducer` does not exist).

- [ ] **Step 3: Extract the shared core + implement `AddOutboxProducer`**

Refactor `OutboxPersistenceBuilderExtensions.cs`. Introduce a private `AddOutboxCore<TOutboxStore>` that registers the shared, idempotent core, and a private `ResolveDataStoreName` helper that probes the configure delegate. Then implement `AddOutboxProducer` = core + producer-side wiring. Full file content:

```csharp
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Persistence.Outbox;

namespace RCommon;

public static class OutboxPersistenceBuilderExtensions
{
    /// <summary>
    /// Registers BOTH the outbox producer (store, routers, tracker) and the processor (hosted poller)
    /// for a datastore — the single-host topology. Equivalent to calling
    /// <see cref="AddOutboxProducer{TOutboxStore}"/> then <see cref="AddOutboxProcessor{TOutboxStore}"/>.
    /// </summary>
    /// <remarks>
    /// All registrations are idempotent (<c>Try*</c>/<c>TryAddEnumerable</c>) and datastore names
    /// deduplicate case-insensitively, so composing producer + processor (which each register the
    /// shared core) is safe and yields exactly one poller and one datastore registration.
    /// </remarks>
    public static IPersistenceBuilder AddOutbox<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null,
        string? dataStoreName = null)
        where TOutboxStore : class, IOutboxStore
    {
        builder.AddOutboxProducer<TOutboxStore>(configure, dataStoreName);
        builder.AddOutboxProcessor<TOutboxStore>(configure, dataStoreName);
        return builder;
    }

    /// <summary>
    /// Registers the outbox PRODUCER for a datastore: the store, serializer, event routers, entity-event
    /// tracker, backoff strategy, datastore registry, and startup diagnostics — but NOT the hosted poller
    /// (<see cref="OutboxProcessingService"/>). Use on a producer-only host in a producer/processor
    /// topology (AC-21).
    /// </summary>
    /// <remarks>
    /// A producer-only host (one that writes outbox rows but does not run the poller) should set
    /// <see cref="OutboxOptions.ImmediateDispatch"/> to <c>false</c> — otherwise an immediate producer-side
    /// dispatch marks the row processed before a remote subscriber (on the processor host) ever sees it.
    /// </remarks>
    public static IPersistenceBuilder AddOutboxProducer<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null,
        string? dataStoreName = null)
        where TOutboxStore : class, IOutboxStore
    {
        builder.AddOutboxCore<TOutboxStore>(configure, dataStoreName);

        // Outbox event router (scoped, concrete) + IEventRouter forwarder.
        builder.Services.TryAddScoped<OutboxEventRouter>();
        builder.Services.TryAddScoped<IEventRouter>(sp => sp.GetRequiredService<OutboxEventRouter>());

        // In-process transactional router as its OWN concrete scoped type (the transient dispatcher the
        // OutboxEntityEventTracker composes directly for the Phase-2 FIFO drain, independent of whatever
        // IEventRouter resolves to in the outbox host).
        builder.Services.TryAddScoped<InMemoryTransactionalEventRouter>();

        // Entity event tracker decorator (scoped — replaces InMemoryEntityEventTracker).
        builder.Services.TryAddScoped<InMemoryEntityEventTracker>();
        builder.Services.TryAddScoped<IEntityEventTracker, OutboxEntityEventTracker>();

        // Startup diagnostic: warn if a later registration silently overrode the outbox IEventRouter.
        // Producer-only: it inspects the producer's IEventRouter -> OutboxEventRouter binding, which a
        // processor-only host never registers, so running it there would false-warn.
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, OutboxRoutingDiagnosticsHostedService>(
                sp => new OutboxRoutingDiagnosticsHostedService(
                    builder.Services,
                    sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>())));

        return builder;
    }

    /// <summary>
    /// Registers the outbox PROCESSOR for a datastore: the shared core plus the hosted poller
    /// (<see cref="OutboxProcessingService"/>) that claims, dispatches, and marks outbox rows. Use on a
    /// processor host in a producer/processor topology (AC-21).
    /// </summary>
    public static IPersistenceBuilder AddOutboxProcessor<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure = null,
        string? dataStoreName = null)
        where TOutboxStore : class, IOutboxStore
    {
        builder.AddOutboxCore<TOutboxStore>(configure, dataStoreName);

        // Background processing service (singleton). AddHostedService uses TryAddEnumerable keyed on the
        // implementation type, so calling this (or AddOutbox) more than once still yields exactly ONE
        // OutboxProcessingService, which drains every registered datastore.
        builder.Services.AddHostedService<OutboxProcessingService>();

        return builder;
    }

    /// <summary>
    /// Registers the shared, idempotent outbox core used by both the producer and the processor:
    /// store, serializer, options, backoff strategy, datastore registry, and datastore-name enqueue.
    /// </summary>
    private static IPersistenceBuilder AddOutboxCore<TOutboxStore>(
        this IPersistenceBuilder builder,
        Action<OutboxOptions>? configure,
        string? dataStoreName)
        where TOutboxStore : class, IOutboxStore
    {
        // Outbox store (scoped — participates in per-request transaction).
        builder.Services.TryAddScoped<IOutboxStore, TOutboxStore>();

        // Serializer (singleton, replaceable).
        builder.Services.TryAddSingleton<IOutboxSerializer, JsonOutboxSerializer>();

        // Options.
        if (configure != null)
            builder.Services.Configure(configure);
        else
            builder.Services.Configure<OutboxOptions>(_ => { });

        // Backoff strategy (singleton, replaceable).
        builder.Services.TryAddSingleton<IBackoffStrategy>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<OutboxOptions>>().Value;
            return new ExponentialBackoffStrategy(opts.BackoffBaseDelay, opts.BackoffMaxDelay, opts.BackoffMultiplier);
        });

        // Datastore registry — singleton, ordering-safe. Resolve the effective name: an explicit positional
        // dataStoreName wins; otherwise a name set via OnDataStore(...) inside the configure delegate; else
        // null (= "use the configured default datastore", resolved lazily by the registry at read time).
        builder.Services.TryAddSingleton<IOutboxDataStoreRegistry, OutboxDataStoreRegistry>();
        var effectiveName = ResolveDataStoreName(configure, dataStoreName);
        builder.Services.Configure<OutboxDataStoreRegistrationOptions>(o => o.Names.Add(effectiveName));

        // Startup diagnostic (MN-3 fail-loud): throw when a durable event route names a datastore that has
        // no registered outbox. In the CORE so producer-only, processor-only, and composed AddOutbox hosts
        // all keep the guard. TryAddEnumerable keyed on impl type => exactly one instance even when the core
        // runs twice (composed AddOutbox) or across multiple datastore registrations.
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, DurableRouteOutboxValidationHostedService>(
                sp => new DurableRouteOutboxValidationHostedService(sp)));

        return builder;
    }

    /// <summary>
    /// Resolves the owning datastore name. An explicit positional <paramref name="dataStoreName"/> takes
    /// precedence; otherwise the <c>configure</c> delegate is probed against a throwaway
    /// <see cref="OutboxOptions"/> to read any <see cref="OutboxOptions.DataStoreName"/> set via
    /// <see cref="OutboxOptions.OnDataStore"/>. Returns <c>null</c> when neither is supplied.
    /// </summary>
    private static string? ResolveDataStoreName(Action<OutboxOptions>? configure, string? dataStoreName)
    {
        if (!string.IsNullOrWhiteSpace(dataStoreName))
            return dataStoreName;

        if (configure == null)
            return null;

        var probe = new OutboxOptions();
        configure(probe);
        return probe.DataStoreName;
    }
}
```

- [ ] **Step 4: Run to verify the Step-1 test passes**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~OutboxTopologySplitTests"`
Expected: PASS.

- [ ] **Step 5: Add the remaining topology tests**

Append to `OutboxTopologySplitTests.cs`:

```csharp
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
    }
```

- [ ] **Step 6: Run to verify all topology tests pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~OutboxTopologySplitTests"`
Expected: PASS (5 tests).

- [ ] **Step 7: Run the whole persistence test project to prove no regression**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "Category!=Integration"`
Expected: PASS (all existing tests, including `OutboxHostRouterResolutionTests`, unchanged).

- [ ] **Step 8: Commit**

```bash
git add Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs Tests/RCommon.Persistence.Tests/OutboxTopologySplitTests.cs
git commit -m "feat(outbox): split AddOutbox into AddOutboxProducer/AddOutboxProcessor (AC-21)"
```

---

## Task 3: Prove `OnDataStore` name flows to the registry through the split

**Files:**
- Test: `Tests/RCommon.Persistence.Tests/OutboxTopologySplitTests.cs` (extend)

This closes the loop between Task 1 (`OnDataStore` on options) and Task 2 (the split): a name set via `OnDataStore` inside the configure delegate must land in `IOutboxDataStoreRegistry.Registrations`, and the positional param must win when both are present. This requires building the provider, so configure `DefaultDataStoreOptions` (the registry folds in the default only for null entries; a real name does not need it, but building the registry is cleaner with it present).

- [ ] **Step 1: Write the failing tests**

Append to `OutboxTopologySplitTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run to verify they fail (or pass)**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~OutboxTopologySplitTests"`
Expected: These may already PASS if Task 2 implemented `ResolveDataStoreName` correctly — that is fine (they are regression guards over the resolution precedence). If either fails, fix `ResolveDataStoreName` and re-run. If they compile-fail, that indicates a missing using or API mismatch — resolve before proceeding.

- [ ] **Step 3: Run the full persistence project**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "Category!=Integration"`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add Tests/RCommon.Persistence.Tests/OutboxTopologySplitTests.cs
git commit -m "test(outbox): prove OnDataStore name resolution through producer/processor split (AC-21)"
```

---

## Task 4: Full fast-lane verification

**Files:** none (verification only)

- [ ] **Step 1: Build the solution (Release)**

Run: `dotnet build Src/RCommon.sln -c Release`
Expected: 0 errors, no new warnings.

- [ ] **Step 2: Run the entire fast lane**

Run: `dotnet test Src/RCommon.sln --filter "Category!=Integration"`
Expected: All test projects PASS, 0 failures. Confirm `CrossDataStoreOutboxTests` (in `Tests/RCommon.IntegrationTests/`, not the persistence project — it is `[Trait("Category","Integration")]` + `[Collection(PostgreSqlCollection.Name)]`) is excluded from this fast lane, and that the split did not disturb any other project.

- [ ] **Step 3: (No commit)** — report results to the controller.

---

## Notes / out of scope for Phase 3c

- The design-doc recipe form `db.AddOutbox(o => o.OnDataStore("Orders"))` with NO type parameter implies a non-generic EFCore-level convenience overload defaulting `TOutboxStore = EFCoreOutboxStore`. That is a recipe-ergonomics concern for **Phase 5 (examples)** and is intentionally NOT built here. Phase 3c delivers the generic `AddOutbox<TOutboxStore>` / `AddOutboxProducer<TOutboxStore>` / `AddOutboxProcessor<TOutboxStore>` in `RCommon.Persistence` plus `OnDataStore`.
- No behavior change: `ImmediateDispatch` default stays `true`; single-host `AddOutbox` wiring is byte-for-byte equivalent to before (verified by the unchanged `OutboxHostRouterResolutionTests` and the integration tests). Producer-only hosts opt into `ImmediateDispatch = false` themselves.
- Multi-host runtime behavior (an actual producer-only process handing off to a separate processor process) is proven end-to-end by the existing cross-host integration coverage; Phase 3c only proves the registration surface via DI-descriptor assertions.
