# Event Handling Phase 4b — MassTransit `UseBrokerOutbox` Native-Outbox Wrapper Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Add a datastore-aware `UseBrokerOutbox<TDbContext>(o => o.OnDataStore("X").UsePostgres())` wrapper on the MassTransit event-handling builder that configures MassTransit's native EF Core bus outbox (`AddEntityFrameworkOutbox<TDbContext>` + `UseBusOutbox()`) bound to an RCommon datastore (recipe 2b, spec AC-14), plus a startup fail-loud validation that the named datastore's registered `DbContext` type matches `TDbContext`. Add a guided `NotSupportedException`-throwing `UseBrokerOutbox` on the Wolverine builder (recipe 2b NO-GO per the Phase-0 spike).

**Architecture:** A new `MassTransitBrokerOutboxOptions` captures the datastore name + provider + optional bus-outbox config WITHOUT needing MassTransit's configurator, so the wrapper can validate eagerly before calling the generic `AddEntityFrameworkOutbox<TDbContext>`. The wrapper records `(dataStoreName → typeof(TDbContext))` into an options accumulator; a startup `IHostedService` cross-checks it against RCommon's `DataStoreFactoryOptions` (name → concrete `DbContext` type) and throws loud on a missing datastore or a type mismatch — enforcing the spike's "same scoped DbContext" caveat at the config-time layer. Wolverine's `UseBrokerOutbox` throws immediately with a message redirecting to `UseRCommonOutbox` (recipe 2a).

**Tech Stack:** .NET net8/9/10; MassTransit 8.5.9 + MassTransit.EntityFrameworkCore 8.5.9; WolverineFx 5.39.1; EF Core; xUnit; AwesomeAssertions (namespace `FluentAssertions`); Moq.

---

## Context for the implementer

Branch `feature/event-handling-outbox-recipes` is checked out — do NOT switch branches, do NOT commit to main. TDD mandatory. Never sign commits with a Claude/AI signature or co-author line.

### Why `UseBrokerOutbox` is GENERIC (not `UseBrokerOutbox("name")`)

MassTransit's `AddEntityFrameworkOutbox<TDbContext>()` is a generic method that must be called at configuration time. RCommon's datastore registration records the name→DbContext-type mapping only via an `IOptions<DataStoreFactoryOptions>` accumulator (not safely readable at config time, and order-dependent), and the DB provider (Npgsql vs SqlServer) is baked into the DbContext options closure and is NOT queryable at config time at all. So the type and provider must be supplied by the developer. This is a deliberate, grounded deviation from the design-doc sketch `UseBrokerOutbox(o => o.OnDataStore("X"))` (which omitted the type param). The `OnDataStore("X")` name is still required — it binds the broker outbox to an RCommon datastore and drives the startup co-location validation.

### The proven recipe-2b wiring to reproduce (from the Phase-0 spike, verified atomic)

`Tests/RCommon.IntegrationTests/Spikes/MassTransitOutboxCoordinationSpikeTests.cs` proved this exact wiring commits atomically inside RCommon's UoW `TransactionScope`:

```csharp
services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<SpikeDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox(); // Publish stages an OutboxMessage row instead of hitting the broker
    });
    x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});
```
The DbContext maps MassTransit's outbox entities via `modelBuilder.AddTransactionalOutboxEntities()` in `OnModelCreating`. `UseBrokerOutbox<TDbContext>` reproduces the `AddEntityFrameworkOutbox<TDbContext>(o => { provider; UseBusOutbox(); })` part; the developer still adds `AddTransactionalOutboxEntities()` to their DbContext (documented, out of scope for the wrapper).

### Existing code to build on

- `Src/RCommon.MassTransit.Outbox/MassTransitOutboxBuilderExtensions.cs` — existing `AddOutbox<TDbContext>(builder, Action<IMassTransitOutboxBuilder>?)` calls `builder.AddEntityFrameworkOutbox<TDbContext>(o => { new MassTransitOutboxBuilder(o); configure(...); })`. LEAVE THIS METHOD as-is (back-compat). Add `UseBrokerOutbox` alongside.
- `Src/RCommon.MassTransit.Outbox/IMassTransitOutboxBuilder.cs` / `MassTransitOutboxBuilder.cs` — wraps MassTransit's `IEntityFrameworkOutboxConfigurator` with `UsePostgres()`/`UseSqlServer()`/`UseBusOutbox(...)`. Existing unit tests (`Tests/RCommon.MassTransit.Outbox.Tests/MassTransitOutboxBuilderTests.cs`) mock `IEntityFrameworkOutboxConfigurator` with Moq — mirror that style.
- `Src/RCommon.Persistence/DataStoreFactoryOptions.cs` — `Values` is a `ConcurrentBag<DataStoreValue>`; `DataStoreValue` has `Name`, `BaseType`, `ConcreteType`. `RCommon.MassTransit.Outbox` references `RCommon.Persistence`, so `DataStoreFactoryOptions`/`DataStoreValue` are visible. It does NOT reference `RCommon.EfCore`, so you cannot name `RCommonDbContext`; constrain `where TDbContext : DbContext` (EF Core's `DbContext`, matching the existing `AddOutbox<TDbContext>`).
- The startup fail-loud pattern to mirror: `Src/RCommon.Persistence/Outbox/DurableRouteOutboxValidationHostedService.cs` (an `IHostedService` that validates at `StartAsync` and throws).

### Project references

`RCommon.MassTransit.Outbox` → `RCommon.MassTransit` + `RCommon.Persistence` (+ `MassTransit.EntityFrameworkCore`). `RCommon.Wolverine.Outbox` → `RCommon.Wolverine` + (WolverineFx.EntityFrameworkCore). Do NOT add new project references.

### Build / test commands

- Build: `dotnet build Src/RCommon.MassTransit.Outbox/RCommon.MassTransit.Outbox.csproj` / `...Wolverine.Outbox...`
- MT outbox tests: `dotnet test Tests/RCommon.MassTransit.Outbox.Tests/RCommon.MassTransit.Outbox.Tests.csproj --filter "Category!=Integration"`
- Wolverine outbox tests: `dotnet test Tests/RCommon.Wolverine.Outbox.Tests/RCommon.Wolverine.Outbox.Tests.csproj --filter "Category!=Integration"`

### Scope guardrails

- Do NOT touch the routing registry / RCommon per-event durability — `UseBrokerOutbox` (recipe 2b, broker-native) is orthogonal to `.UseOutbox`/`UseRCommonOutbox` (recipe 2a, RCommon outbox). A developer using `UseBrokerOutbox` calls `Publish<T>()` WITHOUT `.UseOutbox(...)`. (Document; do not enforce in 4b.)
- Do NOT implement the AC-15 atomicity integration test here — that is Phase 4c.
- Do NOT modify the 4a verbs or any pipeline/tracker/poller code.

---

## Task 1: `MassTransitBrokerOutboxOptions` + `UseBrokerOutbox<TDbContext>`

**Files:**
- Create: `Src/RCommon.MassTransit.Outbox/MassTransitBrokerOutboxOptions.cs`
- Create: `Src/RCommon.MassTransit.Outbox/MassTransitBrokerOutboxRegistrationOptions.cs`
- Modify: `Src/RCommon.MassTransit.Outbox/MassTransitOutboxBuilderExtensions.cs`
- Test: `Tests/RCommon.MassTransit.Outbox.Tests/UseBrokerOutboxTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `UseBrokerOutboxTests.cs`. Use `new RCommonBuilder(services)` + `WithEventHandling<MassTransitEventHandlingBuilder>(...)` (this harness is proven to work with no transport — 4a relied on it). Cover: options record datastore name + provider; `UseBrokerOutbox` throws if `OnDataStore` omitted; throws if no provider chosen; on valid config records `(name → typeof(TDbContext))` into `MassTransitBrokerOutboxRegistrationOptions` (assert via a built provider reading `IOptions<MassTransitBrokerOutboxRegistrationOptions>`); and does not throw.

```csharp
using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.MassTransit;
using Xunit;

namespace RCommon.MassTransit.Outbox.Tests;

public class UseBrokerOutboxTests
{
    // Minimal EF DbContext to serve as TDbContext. AddEntityFrameworkOutbox<T> only registers
    // services at config time; it does not require the context to be otherwise registered, and the
    // tests below never build/start MassTransit's hosted services.
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    private static (ServiceCollection services, RCommonBuilder builder) NewHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return (services, new RCommonBuilder(services));
    }

    [Fact]
    public void Options_OnDataStore_records_name_and_rejects_blank()
    {
        var o = new MassTransitBrokerOutboxOptions();
        o.OnDataStore("Orders");
        o.DataStoreName.Should().Be("Orders");

        Action bad = () => new MassTransitBrokerOutboxOptions().OnDataStore("  ");
        bad.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Options_provider_selection()
    {
        new MassTransitBrokerOutboxOptions().UsePostgres().Provider
            .Should().Be(BrokerOutboxProvider.Postgres);
        new MassTransitBrokerOutboxOptions().UseSqlServer().Provider
            .Should().Be(BrokerOutboxProvider.SqlServer);
    }

    [Fact]
    public void UseBrokerOutbox_throws_when_OnDataStore_is_omitted()
    {
        var (_, builder) = NewHost();
        Action act = () => builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.UsePostgres()));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OnDataStore*");
    }

    [Fact]
    public void UseBrokerOutbox_throws_when_no_provider_selected()
    {
        var (_, builder) = NewHost();
        Action act = () => builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.OnDataStore("Orders")));
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*UsePostgres*UseSqlServer*");
    }

    [Fact]
    public void UseBrokerOutbox_records_datastore_to_dbcontext_mapping()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.OnDataStore("Orders").UsePostgres()));

        using var provider = services.BuildServiceProvider();
        var reg = provider.GetRequiredService<IOptions<MassTransitBrokerOutboxRegistrationOptions>>().Value;
        reg.Registrations.Should().ContainSingle(r =>
            r.DataStoreName == "Orders" && r.DbContextType == typeof(TestDbContext));
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests/RCommon.MassTransit.Outbox.Tests/RCommon.MassTransit.Outbox.Tests.csproj --filter "FullyQualifiedName~UseBrokerOutboxTests"`
Expected: compile failure (`MassTransitBrokerOutboxOptions`, `UseBrokerOutbox`, `BrokerOutboxProvider`, `MassTransitBrokerOutboxRegistrationOptions` do not exist).

> If instead you hit a `DbContext`/`DbContextOptions<>` "type not found" compile error in the TEST project, EF Core is only reaching the test transitively. Add `<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.1" />` to `Tests/RCommon.MassTransit.Outbox.Tests/RCommon.MassTransit.Outbox.Tests.csproj` (version-aligned to the EF Core that MassTransit.EntityFrameworkCore 8.5.9 brings transitively — avoids NU1605). Likely unnecessary (transitive assets usually flow), but this is the fix if needed.

- [ ] **Step 3: Implement the options + registration accumulator + wrapper**

Create `MassTransitBrokerOutboxOptions.cs`:

```csharp
using System;
using MassTransit;

namespace RCommon.MassTransit.Outbox;

/// <summary>Which relational provider MassTransit's EF Core outbox should target.</summary>
public enum BrokerOutboxProvider { None = 0, Postgres, SqlServer }

/// <summary>
/// Options for <see cref="MassTransitOutboxBuilderExtensions.UseBrokerOutbox{TDbContext}"/> (recipe 2b).
/// Captures the owning RCommon datastore name and the provider WITHOUT needing MassTransit's
/// configurator, so the wrapper can validate before calling the generic AddEntityFrameworkOutbox.
/// </summary>
public sealed class MassTransitBrokerOutboxOptions
{
    /// <summary>The RCommon datastore whose DbContext owns the broker outbox. Required (AC-14).</summary>
    public string? DataStoreName { get; private set; }

    /// <summary>The chosen relational provider. Required — RCommon cannot infer it at config time.</summary>
    public BrokerOutboxProvider Provider { get; private set; } = BrokerOutboxProvider.None;

    /// <summary>Optional customization of MassTransit's bus outbox; applied by the wrapper.</summary>
    public Action<IBusOutboxConfigurator>? BusOutboxConfigure { get; private set; }

    public MassTransitBrokerOutboxOptions OnDataStore(string dataStoreName)
    {
        if (string.IsNullOrWhiteSpace(dataStoreName))
            throw new ArgumentException("Data store name must not be null, empty, or whitespace.", nameof(dataStoreName));
        DataStoreName = dataStoreName;
        return this;
    }

    public MassTransitBrokerOutboxOptions UsePostgres() { Provider = BrokerOutboxProvider.Postgres; return this; }
    public MassTransitBrokerOutboxOptions UseSqlServer() { Provider = BrokerOutboxProvider.SqlServer; return this; }

    public MassTransitBrokerOutboxOptions UseBusOutbox(Action<IBusOutboxConfigurator>? configure = null)
    {
        BusOutboxConfigure = configure;
        return this;
    }
}
```

Create `MassTransitBrokerOutboxRegistrationOptions.cs`:

```csharp
using System;
using System.Collections.Generic;

namespace RCommon.MassTransit.Outbox;

/// <summary>Accumulates (datastore name → DbContext type) bindings declared via UseBrokerOutbox,
/// for the startup co-location validation (Task 2).</summary>
public sealed class MassTransitBrokerOutboxRegistrationOptions
{
    public List<BrokerOutboxRegistration> Registrations { get; } = new();
    public void Register(string dataStoreName, Type dbContextType)
        => Registrations.Add(new BrokerOutboxRegistration(dataStoreName, dbContextType));
}

public sealed record BrokerOutboxRegistration(string DataStoreName, Type DbContextType);
```

Add to `MassTransitOutboxBuilderExtensions.cs` (keep the existing `AddOutbox<TDbContext>`; add usings `using Microsoft.EntityFrameworkCore;`, `using Microsoft.Extensions.DependencyInjection;`, and `using Microsoft.Extensions.DependencyInjection.Extensions;` — the last is needed for `TryAddEnumerable` used in Task 2):

```csharp
/// <summary>
/// Configures MassTransit's native EF Core bus outbox (recipe 2b) bound to an RCommon datastore:
/// <c>AddEntityFrameworkOutbox&lt;TDbContext&gt;()</c> + <c>UseBusOutbox()</c> with the chosen provider
/// (spec AC-14). A published/sent message stages an OutboxMessage row during <typeparamref name="TDbContext"/>'s
/// SaveChanges inside RCommon's UnitOfWork TransactionScope, committing atomically with business state.
/// </summary>
/// <remarks>
/// <para><typeparamref name="TDbContext"/> must be the SAME DbContext registered for the datastore named
/// via <c>OnDataStore</c>; a startup validation fails loud on a mismatch. The DbContext must map
/// MassTransit's outbox entities (<c>modelBuilder.AddTransactionalOutboxEntities()</c>).</para>
/// <para>Generic + explicit provider by necessity: MassTransit's AddEntityFrameworkOutbox is generic
/// (needs the type at config time) and RCommon cannot infer the provider at config time.</para>
/// </remarks>
public static IMassTransitEventHandlingBuilder UseBrokerOutbox<TDbContext>(
    this IMassTransitEventHandlingBuilder builder,
    Action<MassTransitBrokerOutboxOptions> configure)
    where TDbContext : DbContext
{
    if (configure is null) throw new ArgumentNullException(nameof(configure));

    var opts = new MassTransitBrokerOutboxOptions();
    configure(opts);

    if (string.IsNullOrWhiteSpace(opts.DataStoreName))
        throw new InvalidOperationException(
            "UseBrokerOutbox requires OnDataStore(\"<name>\") to name the RCommon datastore that owns the broker outbox (AC-14).");
    if (opts.Provider == BrokerOutboxProvider.None)
        throw new InvalidOperationException(
            "UseBrokerOutbox requires a provider: call UsePostgres() or UseSqlServer(). RCommon cannot infer the provider from the datastore registration at configuration time.");

    // Record the (datastore -> DbContext type) binding for the startup co-location validation (Task 2).
    builder.Services.Configure<MassTransitBrokerOutboxRegistrationOptions>(
        o => o.Register(opts.DataStoreName!, typeof(TDbContext)));

    // Register MassTransit's native EF Core bus outbox (the proven recipe-2b wiring).
    builder.AddEntityFrameworkOutbox<TDbContext>(o =>
    {
        if (opts.Provider == BrokerOutboxProvider.Postgres) o.UsePostgres();
        else o.UseSqlServer();
        o.UseBusOutbox(opts.BusOutboxConfigure);
    });

    return builder;
}
```

> IMPLEMENTER — verified MassTransit 8.5.9 API facts (do not re-derive): `IEntityFrameworkOutboxConfigurator` exposes `UsePostgres()`, `UseSqlServer()`, and `UseBusOutbox(Action<IEntityFrameworkBusOutboxConfigurator> configure)`. NOTE the delegate is `Action<IEntityFrameworkBusOutboxConfigurator>`, NOT `Action<IBusOutboxConfigurator>`. Because `IEntityFrameworkBusOutboxConfigurator : IBusOutboxConfigurator`, passing an `Action<IBusOutboxConfigurator>` works via `Action<in T>` contravariance — which is exactly what the existing committed `MassTransitOutboxBuilder.UseBusOutbox(Action<IBusOutboxConfigurator>?)` does and compiles. So keeping `MassTransitBrokerOutboxOptions.BusOutboxConfigure` typed as `Action<IBusOutboxConfigurator>?` and passing it to `o.UseBusOutbox(...)` compiles fine. `AddEntityFrameworkOutbox<TDbContext>` is an extension on `IBusRegistrationConfigurator`, which `IMassTransitEventHandlingBuilder` derives from (the existing `AddOutbox<TDbContext>` already calls `builder.AddEntityFrameworkOutbox<TDbContext>`), so `builder.AddEntityFrameworkOutbox<TDbContext>(...)` compiles on the builder. All confirmed via a live reflection/compile probe of the 8.5.9 assemblies.

- [ ] **Step 4: Run to verify PASS**

Run: `dotnet test Tests/RCommon.MassTransit.Outbox.Tests/RCommon.MassTransit.Outbox.Tests.csproj --filter "FullyQualifiedName~UseBrokerOutboxTests"`
Expected: all PASS. If the mapping-recording test fails because building the provider tries to instantiate a MassTransit hosted service, change it to read the descriptor instead: assert `services` contains an `IConfigureOptions<MassTransitBrokerOutboxRegistrationOptions>` (i.e., the `Configure` ran) — but first try the `IOptions` read (BuildServiceProvider does not start hosted services, so it should work).

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.MassTransit.Outbox/ Tests/RCommon.MassTransit.Outbox.Tests/
git commit -m "feat(masstransit): add UseBrokerOutbox<TDbContext> native EF outbox wrapper (AC-14)"
```

---

## Task 2: Startup co-location validation (fail-loud)

**Files:**
- Create: `Src/RCommon.MassTransit.Outbox/BrokerOutboxDataStoreValidationHostedService.cs`
- Modify: `Src/RCommon.MassTransit.Outbox/MassTransitOutboxBuilderExtensions.cs` (register the validator idempotently inside `UseBrokerOutbox`)
- Test: `Tests/RCommon.MassTransit.Outbox.Tests/BrokerOutboxDataStoreValidationTests.cs`

The validator enforces the spike's caveat 1 at the config-time layer: the `TDbContext` passed to `UseBrokerOutbox` MUST be the concrete DbContext registered for the named datastore (via `ef.AddDbContext<TContext>("name", ...)`), otherwise the outbox interceptor is attached to a different DbContext than the one carrying business writes and atomicity is silently lost.

- [ ] **Step 1: Write the failing tests**

`BrokerOutboxDataStoreValidationTests.cs` — build the two options objects, construct the validator directly, and assert `StartAsync` behaviour. Mirror the assertion style of `Tests/RCommon.Persistence.Tests/DurableRouteOutboxValidationTests.cs`.

**Seeding `DataStoreFactoryOptions` (the tricky seam — do it this way):** do NOT use `Register<B,C>` (it constrains `B`/`C` to `IDataStore`, which a plain test `DbContext` is not). Instead add a `DataStoreValue` directly. The `DataStoreValue` ctor only validates `concreteType.BaseType == baseType` (it does NOT require `IDataStore`), and a test `class TestDbContext : DbContext` has `BaseType == typeof(DbContext)`. So:
```csharp
var dsOptions = new DataStoreFactoryOptions();
dsOptions.Values.Add(new DataStoreValue("Orders", typeof(DbContext), typeof(TestDbContext))); // matches
var brokerOptions = new MassTransitBrokerOutboxRegistrationOptions();
brokerOptions.Register("Orders", typeof(TestDbContext));
var validator = new BrokerOutboxDataStoreValidationHostedService(
    Microsoft.Extensions.Options.Options.Create(brokerOptions),
    Microsoft.Extensions.Options.Options.Create(dsOptions));
```
IMPLEMENTER: first confirm `DataStoreFactoryOptions.Values` is publicly addable and `DataStoreValue`'s ctor is public with the `(string name, Type baseType, Type concreteType)` signature. If `Values` is not publicly mutable, fall back to adding a `ProjectReference` to `RCommon.EfCore` in the TEST project only and seeding a real `RCommonDbContext`-derived context via `Register<RCommonDbContext, RealCtx>` — but the direct `Values.Add` above is preferred (no new reference). Assertions:

Assertions:
- match → `StartAsync` completes without throwing.
- name registered but concrete type mismatch → throws with a message naming the datastore + both types.
- name not registered at all → throws with a message naming the datastore.

- [ ] **Step 2: Run to verify fail** (compile failure — validator does not exist).

- [ ] **Step 3: Implement the validator + register it in `UseBrokerOutbox`**

`BrokerOutboxDataStoreValidationHostedService.cs`:

```csharp
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RCommon.Persistence;

namespace RCommon.MassTransit.Outbox;

/// <summary>
/// Startup fail-loud (MN-3): verifies every UseBrokerOutbox binding names a REGISTERED RCommon datastore
/// whose concrete DbContext type equals the TDbContext passed to UseBrokerOutbox. A mismatch means the
/// broker-outbox interceptor sits on a different DbContext than the one carrying business writes, silently
/// breaking recipe-2b atomicity.
/// </summary>
public sealed class BrokerOutboxDataStoreValidationHostedService : IHostedService
{
    private readonly IOptions<MassTransitBrokerOutboxRegistrationOptions> _brokerOutbox;
    private readonly IOptions<DataStoreFactoryOptions> _dataStores;

    public BrokerOutboxDataStoreValidationHostedService(
        IOptions<MassTransitBrokerOutboxRegistrationOptions> brokerOutbox,
        IOptions<DataStoreFactoryOptions> dataStores)
    {
        _brokerOutbox = brokerOutbox;
        _dataStores = dataStores;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var stores = _dataStores.Value.Values;
        foreach (var reg in _brokerOutbox.Value.Registrations)
        {
            var match = stores.FirstOrDefault(v =>
                string.Equals(v.Name, reg.DataStoreName, StringComparison.OrdinalIgnoreCase));

            if (match is null)
                throw new InvalidOperationException(
                    $"UseBrokerOutbox named datastore '{reg.DataStoreName}', but no EF Core datastore with that " +
                    $"name is registered. Call ef.AddDbContext<{reg.DbContextType.Name}>(\"{reg.DataStoreName}\", ...).");

            if (match.ConcreteType != reg.DbContextType)
                throw new InvalidOperationException(
                    $"UseBrokerOutbox<{reg.DbContextType.Name}> is bound to datastore '{reg.DataStoreName}', but that " +
                    $"datastore is registered with DbContext '{match.ConcreteType.Name}'. The broker outbox must use the " +
                    $"SAME DbContext that owns the datastore's business writes, or recipe-2b atomicity is lost.");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

In `UseBrokerOutbox` (before or after the `AddEntityFrameworkOutbox` call), register the validator idempotently:

```csharp
builder.Services.TryAddEnumerable(
    Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Singleton<
        Microsoft.Extensions.Hosting.IHostedService, BrokerOutboxDataStoreValidationHostedService>());
```
(`TryAddEnumerable` keyed on the implementation type ⇒ exactly one validator even across multiple `UseBrokerOutbox` calls.)

- [ ] **Step 4: Run to verify PASS.**

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.MassTransit.Outbox/ Tests/RCommon.MassTransit.Outbox.Tests/
git commit -m "feat(masstransit): fail-loud startup validation that UseBrokerOutbox DbContext matches its datastore (AC-14/MN-3)"
```

---

## Task 3: Wolverine `UseBrokerOutbox` guided `NotSupportedException`

**Files:**
- Create: `Src/RCommon.Wolverine.Outbox/WolverineBrokerOutboxOptions.cs`
- Modify: `Src/RCommon.Wolverine.Outbox/WolverineOutboxBuilderExtensions.cs`
- Test: `Tests/RCommon.Wolverine.Outbox.Tests/WolverineUseBrokerOutboxTests.cs`

Recipe 2b is NO-GO for Wolverine (Phase-0 spike: Wolverine's envelope write opens its own `DbTransaction`, suppressing ambient `System.Transactions` enlistment). Expose `UseBrokerOutbox` so a developer copying the MassTransit recipe gets a clear runtime redirect, not a cryptic compile error (per the chosen design). The options type exists only so the call compiles the same shape as MassTransit's; its methods are no-ops because the wrapper throws before invoking `configure`.

- [ ] **Step 1: Write the failing test**

```csharp
using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Wolverine;
using Xunit;

namespace RCommon.Wolverine.Outbox.Tests;

public class WolverineUseBrokerOutboxTests
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
    }

    [Fact]
    public void UseBrokerOutbox_throws_NotSupported_pointing_to_UseRCommonOutbox()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new RCommonBuilder(services);

        Action act = () => builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
            e.UseBrokerOutbox<TestDbContext>(o => o.OnDataStore("Orders").UsePostgres()));

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*UseRCommonOutbox*");
    }
}
```

(Confirm `WithEventHandling<WolverineEventHandlingBuilder>` is the correct entry — mirror how `Tests/RCommon.Wolverine.Tests` construct the builder; the 4a Wolverine tests use `new RCommonBuilder(services).WithEventHandling<WolverineEventHandlingBuilder>(...)`.)

- [ ] **Step 2: Run to verify fail** (compile failure).

- [ ] **Step 3: Implement**

`WolverineBrokerOutboxOptions.cs`:

```csharp
namespace RCommon.Wolverine.Outbox;

/// <summary>
/// Mirror-shape options so a developer copying a MassTransit recipe-2b call compiles against the Wolverine
/// builder — but <see cref="WolverineOutboxBuilderExtensions.UseBrokerOutbox{TDbContext}"/> always throws
/// (recipe 2b is NO-GO for Wolverine), so these methods are never invoked.
/// </summary>
public sealed class WolverineBrokerOutboxOptions
{
    public WolverineBrokerOutboxOptions OnDataStore(string dataStoreName) => this;
    public WolverineBrokerOutboxOptions UsePostgres() => this;
    public WolverineBrokerOutboxOptions UseSqlServer() => this;
}
```

Add to `WolverineOutboxBuilderExtensions.cs` (add `using Microsoft.EntityFrameworkCore;`, `using System;`):

```csharp
/// <summary>
/// NOT SUPPORTED for Wolverine. WolverineFx's native EF Core outbox writes its envelope on its OWN
/// DbTransaction, which suppresses ambient System.Transactions enlistment, so it cannot stage atomically
/// inside RCommon's UnitOfWork TransactionScope (verified in the Phase-0 coordination spike). Use
/// <c>UseRCommonOutbox("&lt;datastore&gt;")</c> (recipe 2a) instead: RCommon's own per-datastore outbox writes
/// the row atomically and a processor relays it via Wolverine.
/// </summary>
/// <exception cref="NotSupportedException">Always thrown.</exception>
public static IWolverineEventHandlingBuilder UseBrokerOutbox<TDbContext>(
    this IWolverineEventHandlingBuilder builder,
    Action<WolverineBrokerOutboxOptions> configure)
    where TDbContext : DbContext
    => throw new NotSupportedException(
        "UseBrokerOutbox is not supported for Wolverine: its native EF Core outbox commits its envelope on its " +
        "own DbTransaction, which cannot enlist in RCommon's UnitOfWork TransactionScope (recipe 2b NO-GO, " +
        "verified by the Phase-0 coordination spike). Use UseRCommonOutbox(\"<datastore>\") instead (recipe 2a): " +
        "RCommon's per-datastore outbox stages the row atomically and a processor relays it via Wolverine.");
```

- [ ] **Step 4: Run to verify PASS.**

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Wolverine.Outbox/ Tests/RCommon.Wolverine.Outbox.Tests/
git commit -m "feat(wolverine): UseBrokerOutbox throws guided NotSupportedException -> UseRCommonOutbox (recipe 2b NO-GO)"
```

---

## Task 4: Full fast-lane verification

- [ ] **Step 1: Release build** — `dotnet build Src/RCommon.sln -c Release`. Expected 0 errors; no new warnings beyond the pre-existing 217.
- [ ] **Step 2: Full fast lane** — `dotnet test Src/RCommon.sln --filter "Category!=Integration"`. Expected all PASS, 0 failures.
- [ ] **Step 3: (No commit)** — report results.

---

## Notes / out of scope for Phase 4b

- **AC-15 atomicity/rollback integration test (Podman + Postgres + MassTransit, recipe 2b)** is **Phase 4c** — it will use `UseBrokerOutbox<TDbContext>` from this phase in place of the raw spike wiring.
- **Provider inference from the datastore** is intentionally NOT done (not queryable at config time); the developer selects `UsePostgres()`/`UseSqlServer()`.
- **Signature deviation from the design doc:** the doc sketched `UseBrokerOutbox(o => o.OnDataStore("X"))` (no type param); the implementable, robust form is `UseBrokerOutbox<TDbContext>(...)` — see the Context section for the grounded rationale. Flag this in the completion report.
- The developer must still add `modelBuilder.AddTransactionalOutboxEntities()` to their DbContext's `OnModelCreating` — the wrapper cannot do this (it does not own the model). Documented in the recipe (Phase 5/6).
