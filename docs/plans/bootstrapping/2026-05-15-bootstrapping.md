# Modular Bootstrapper Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `services.AddRCommon()` and its fluent verbs composable across multiple modules in a single in-process .NET application without breaking existing single-call usage.

**Architecture:** Cache the `IRCommonBuilder` instance as a `ServiceDescriptor.ImplementationInstance` on the `IServiceCollection`. Add a `GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder>)` helper on `IRCommonBuilder` so every `WithX<T>` extension can route through the cache and skip the redundant `Activator.CreateInstance` call when `T` is already configured. Replace bare `_guidConfigured`/`_dateTimeConfigured` flags with `SingletonRegistration` structs that track impl type for same-type-idempotent / different-type-throw semantics. Wire the existing duplicate-descriptor scanner to run automatically at host startup via an internal `IHostedService`. Preserve `UnsupportedDataStoreException` for backward compat.

**Tech Stack:** .NET 8 / 9 / 10 multi-target; `Microsoft.Extensions.DependencyInjection`; `Microsoft.Extensions.Hosting.IHostedService`; `Microsoft.Extensions.Logging`. Test framework: xUnit + FluentAssertions (matches existing test conventions). Working branch: `feature/modular-bootstrapper`.

**Reference documents:**
- Domain spec: [`docs/specs/bootstrapping/bootstrapping.md`](../../specs/bootstrapping/bootstrapping.md) — testable contract.
- Design doc: [`docs/superpowers/specs/2026-05-15-modular-bootstrapper-design.md`](../../superpowers/specs/2026-05-15-modular-bootstrapper-design.md) — implementation specifics, conflict matrix, full call-site enumeration.

**TDD ordering (from spec):** core idempotency → cache helper → singleton tracker → datastore-name collision → soft-duplicate finalize → per-provider migrations.

---

## File Structure

### New files

| Path | Responsibility |
|---|---|
| `Src/RCommon.Core/SingletonRegistration.cs` | Mutable struct tracking `(Configured, ImplType)` for singleton-style verbs. |
| `Src/RCommon.Core/RCommonBootstrapDiagnosticsHostedService.cs` | `IHostedService` that runs the duplicate scanner once at startup. |
| `Tests/RCommon.Core.Tests/Bootstrapping/AddRCommonIdempotencyTests.cs` | Top-level `AddRCommon()` idempotency. |
| `Tests/RCommon.Core.Tests/Bootstrapping/SubBuilderCacheTests.cs` | `GetOrAddBuilder<T>` semantics. |
| `Tests/RCommon.Core.Tests/Bootstrapping/SingletonVerbConflictTests.cs` | Singleton-verb conflict / idempotency. |
| `Tests/RCommon.Core.Tests/Bootstrapping/SoftDuplicateDiagnosticsTests.cs` | Finalize warning emission. |
| `Tests/RCommon.Core.Tests/Bootstrapping/EventProducerDedupTests.cs` | `AddProducer<T>` descriptor-scan dedup. |
| `Tests/RCommon.Persistence.Tests/Bootstrapping/DataStoreFactoryOptionsRegisterTests.cs` | `Register<,>` collision detection. |
| `Tests/RCommon.EfCore.Tests/Bootstrapping/MultiModuleEFCoreTests.cs` | End-to-end multi-module persistence integration. |
| `Tests/RCommon.Mediatr.Tests/Bootstrapping/MultiModuleMediatRTests.cs` | End-to-end multi-module event-handling integration. |
| `Tests/RCommon.Json.Tests/Bootstrapping/JsonSerializationSingletonTests.cs` | `WithJsonSerialization<T>` singleton-style semantics. |

### Modified files (core)

| Path | Change |
|---|---|
| `Src/RCommon.Core/IRCommonBuilder.cs` | Add `GetOrAddBuilder<T>(Func<T>)` and `GetBootstrapDiagnostics()`. |
| `Src/RCommon.Core/RCommonBuilder.cs` | Sub-builder cache, `SingletonRegistration` for guid/datetime, relaxed verbs, diagnostics retrieval. |
| `Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs` | `AddRCommon()` cache-lookup-and-return, `IsRCommonInitialized()`, register hosted service. |
| `Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs` | `WithEventHandling<T>` routes through `GetOrAddBuilder`. `AddProducer<T>` descriptor-scan dedup. |
| `Src/RCommon.Persistence/PersistenceBuilderExtensions.cs` | `WithPersistence<T>` (4 overloads), `WithUnitOfWork<T>` route through `GetOrAddBuilder`. `WithEventTracking` switches to `TryAddScoped`. |
| `Src/RCommon.Persistence/DataStoreFactoryOptions.cs` | `Register<B,C>` accepts identical `(name, B, C)` as idempotent; throws on `(name, B)` with different `C`. |

### Modified files (per-provider migrations)

| Path | Verb(s) affected |
|---|---|
| `Src/RCommon.Mediator/MediatorBuilderExtensions.cs` | `WithMediator<T>` (2 overloads) |
| `Src/RCommon.Mediatr/MediatREventHandlingBuilderExtensions.cs` | `WithEventHandling<T>` (3 overloads) |
| `Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs` | `WithEventHandling<T>` (2 overloads) |
| `Src/RCommon.Caching/CachingBuilderExtensions.cs` | `WithMemoryCaching<T>` (2 overloads), `WithDistributedCaching<T>` (2 overloads) |
| `Src/RCommon.Json/JsonBuilderExtensions.cs` | `WithJsonSerialization<T>` (6 overloads) — singleton-style |
| `Src/RCommon.ApplicationServices/CqrsBuilderExtensions.cs` | `WithCQRS<T>` (2 overloads) |
| `Src/RCommon.ApplicationServices/ValidationBuilderExtensions.cs` | `WithValidation<T>` (2 overloads) |
| `Src/RCommon.FluentValidation/ValidationBuilderExtensions.cs` | `WithValidation<T>` (2 overloads) |
| `Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs` | `WithBlobStorage<T>` (2 overloads) |
| `Src/RCommon.MultiTenancy/MultiTenancyBuilderExtensions.cs` | `WithMultiTenancy<T>` |
| `Src/RCommon.Stateless/StatelessBuilderExtensions.cs` | `WithStatelessStateMachine` (TryAdd) |
| `Src/RCommon.MassTransit.StateMachines/MassTransitStateMachineBuilderExtensions.cs` | `WithMassTransitStateMachine` (TryAdd) |
| `Src/RCommon.Emailing/EmailingBuilderExtensions.cs` | `WithSmtpEmailServices` (TryAdd + singleton-style) |
| `Src/RCommon.SendGrid/SendGridEmailingConfigurationExtensions.cs` | `WithSendGridEmailServices` (TryAdd + singleton-style) |
| `Src/RCommon.Security/SecurityConfigurationExtensions.cs` | `WithClaimsAndPrincipalAccessor` (TryAdd) |
| `Src/RCommon.Web/WebConfigurationExtensions.cs` | `WithClaimsAndPrincipalAccessorForWeb` (TryAdd) |

### Modified existing tests

| Path | Reason |
|---|---|
| `Tests/RCommon.Core.Tests/RCommonBuilderTests.cs` | Replace `*CalledTwice_Throws*` tests for `WithSequentialGuidGenerator`, `WithSimpleGuidGenerator`, `WithDateTimeSystem` with idempotent assertions. Add same-type-idempotent and different-type-throws assertions. |

---

## Conventions

- All tests use **xUnit** with `[Fact]`/`[Theory]` and **FluentAssertions** (`x.Should().Be(...)`), matching existing test files.
- All tests live under `Tests/<source-project>.Tests/Bootstrapping/` subfolder (new) using the parent namespace.
- Run all tests from the repo root: `dotnet test`.
- Run a single test class: `dotnet test --filter "FullyQualifiedName~AddRCommonIdempotencyTests"`.
- Build the whole solution: `dotnet build Src/RCommon.sln`.
- Branch: `feature/modular-bootstrapper` (already checked out and contains the committed spec + design doc).

**Commit cadence:** one commit per red-green-refactor cycle. Commit message format: `<verb>: <short description>` (e.g., `feat: cache IRCommonBuilder on IServiceCollection`). Never use the Claude signature per CLAUDE.md.

---

## Task 1: AddRCommon idempotency — failing tests

**Files:**
- Create: `Tests/RCommon.Core.Tests/Bootstrapping/AddRCommonIdempotencyTests.cs`

- [ ] **Step 1.1: Create directory and test file**

```bash
mkdir -p Tests/RCommon.Core.Tests/Bootstrapping
```

Create `Tests/RCommon.Core.Tests/Bootstrapping/AddRCommonIdempotencyTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class AddRCommonIdempotencyTests
{
    [Fact]
    public void AddRCommon_CalledTwice_ReturnsSameBuilderInstance()
    {
        var services = new ServiceCollection();

        var first = services.AddRCommon();
        var second = services.AddRCommon();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void AddRCommon_CalledTwice_RegistersEventBusOnlyOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon();
        services.AddRCommon();

        services.Count(d => d.ServiceType == typeof(IEventBus)).Should().Be(1);
    }

    [Fact]
    public void AddRCommon_CalledTwice_RegistersEventSubscriptionManagerOnlyOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon();
        services.AddRCommon();

        services.Count(d => d.ServiceType == typeof(EventSubscriptionManager)).Should().Be(1);
    }

    [Fact]
    public void AddRCommon_CalledTwice_RegistersEventRouterOnlyOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon();
        services.AddRCommon();

        services.Count(d => d.ServiceType == typeof(IEventRouter)).Should().Be(1);
    }

    [Fact]
    public void AddRCommon_CalledOnce_HasIdenticalDescriptorCountToCalledOnce()
    {
        var servicesA = new ServiceCollection();
        var servicesB = new ServiceCollection();

        servicesA.AddRCommon();
        servicesB.AddRCommon();
        servicesB.AddRCommon();

        servicesB.Count.Should().Be(servicesA.Count);
    }

    [Fact]
    public void IsRCommonInitialized_BeforeAddRCommon_ReturnsFalse()
    {
        var services = new ServiceCollection();

        services.IsRCommonInitialized().Should().BeFalse();
    }

    [Fact]
    public void IsRCommonInitialized_AfterAddRCommon_ReturnsTrue()
    {
        var services = new ServiceCollection();

        services.AddRCommon();

        services.IsRCommonInitialized().Should().BeTrue();
    }
}
```

- [ ] **Step 1.2: Run tests, verify they fail to compile / fail with no `IsRCommonInitialized`**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~AddRCommonIdempotencyTests"
```

Expected: BUILD FAILURE — `IsRCommonInitialized` extension does not exist; second `AddRCommon` does not return the same instance.

## Task 2: AddRCommon idempotency — implementation

**Files:**
- Modify: `Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs`

- [ ] **Step 2.1: Update `AddRCommon` to cache and return existing builder**

Replace the `AddRCommon` method body with:

```csharp
public static IRCommonBuilder AddRCommon(this IServiceCollection services)
{
    var existing = services.FirstOrDefault(d => d.ServiceType == typeof(IRCommonBuilder));
    if (existing?.ImplementationInstance is IRCommonBuilder cached)
    {
        return cached;
    }

    var config = new RCommonBuilder(services);
    services.AddSingleton<IRCommonBuilder>(config);
    config.Configure(); // No-op in the base class (just returns Services); preserved for consistency with the existing API shape and in case a subclass overrides it.
    return config;
}
```

- [ ] **Step 2.2: Add `IsRCommonInitialized` extension**

Append to the same class (inside `ServiceCollectionExtensions`):

```csharp
/// <summary>
/// Returns <c>true</c> if <see cref="AddRCommon"/> has been invoked against this collection.
/// </summary>
public static bool IsRCommonInitialized(this IServiceCollection services)
{
    return services.Any(d => d.ServiceType == typeof(IRCommonBuilder));
}
```

- [ ] **Step 2.3: Run tests, verify they pass**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~AddRCommonIdempotencyTests"
```

Expected: All 7 tests PASS.

- [ ] **Step 2.4: Commit**

```bash
git add Tests/RCommon.Core.Tests/Bootstrapping/AddRCommonIdempotencyTests.cs Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(bootstrapping): cache IRCommonBuilder on IServiceCollection for idempotent AddRCommon"
```

---

## Task 3: GetOrAddBuilder helper — failing tests

**Files:**
- Create: `Tests/RCommon.Core.Tests/Bootstrapping/SubBuilderCacheTests.cs`

- [ ] **Step 3.1: Create test file**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class SubBuilderCacheTests
{
    [Fact]
    public void GetOrAddBuilder_FirstCall_InvokesFactoryOnce()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        var factoryCount = 0;

        builder.GetOrAddBuilder(() =>
        {
            factoryCount++;
            return new TestSubBuilder(services);
        });

        factoryCount.Should().Be(1);
    }

    [Fact]
    public void GetOrAddBuilder_SecondCallForSameType_DoesNotInvokeFactory()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        var factoryCount = 0;

        builder.GetOrAddBuilder(() => { factoryCount++; return new TestSubBuilder(services); });
        builder.GetOrAddBuilder(() => { factoryCount++; return new TestSubBuilder(services); });

        factoryCount.Should().Be(1);
    }

    [Fact]
    public void GetOrAddBuilder_SecondCallForSameType_ReturnsCachedInstance()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        var first = builder.GetOrAddBuilder(() => new TestSubBuilder(services));
        var second = builder.GetOrAddBuilder(() => new TestSubBuilder(services));

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetOrAddBuilder_DifferentTypes_ReturnsDistinctInstances()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        var subA = builder.GetOrAddBuilder(() => new TestSubBuilder(services));
        var subB = builder.GetOrAddBuilder(() => new OtherTestSubBuilder(services));

        ((object)subA).Should().NotBeSameAs(subB);
    }

    private sealed class TestSubBuilder
    {
        public TestSubBuilder(IServiceCollection services) { }
    }

    private sealed class OtherTestSubBuilder
    {
        public OtherTestSubBuilder(IServiceCollection services) { }
    }
}
```

- [ ] **Step 3.2: Run, verify build failure (GetOrAddBuilder doesn't exist)**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~SubBuilderCacheTests"
```

Expected: BUILD FAILURE — `GetOrAddBuilder` not defined on `IRCommonBuilder`.

## Task 4: GetOrAddBuilder helper — implementation

**Files:**
- Modify: `Src/RCommon.Core/IRCommonBuilder.cs`
- Modify: `Src/RCommon.Core/RCommonBuilder.cs`

- [ ] **Step 4.1: Add interface member**

Add to `IRCommonBuilder` (after existing members):

```csharp
/// <summary>
/// Returns the cached sub-builder for <typeparamref name="TSubBuilder"/> if one exists,
/// otherwise invokes <paramref name="factory"/>, caches the result, and returns it.
/// </summary>
/// <typeparam name="TSubBuilder">Concrete sub-builder type (e.g., <c>EFCorePerisistenceBuilder</c>).</typeparam>
/// <param name="factory">Parameterless factory invoked exactly once per <typeparamref name="TSubBuilder"/>
/// per <see cref="IRCommonBuilder"/>. Callers close over whichever constructor argument the sub-builder
/// requires — typically <c>builder.Services</c> or <c>builder</c> itself.</param>
TSubBuilder GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder> factory)
    where TSubBuilder : class;
```

- [ ] **Step 4.2: Add implementation in `RCommonBuilder`**

Add a private cache field:

```csharp
private readonly Dictionary<Type, object> _subBuilderCache = new();
```

(Add `using System.Collections.Generic;` if not already present.)

Add the method body:

```csharp
public TSubBuilder GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder> factory)
    where TSubBuilder : class
{
    if (_subBuilderCache.TryGetValue(typeof(TSubBuilder), out var cached))
    {
        return (TSubBuilder)cached;
    }

    var built = factory();
    _subBuilderCache[typeof(TSubBuilder)] = built;
    return built;
}
```

- [ ] **Step 4.3: Run tests, verify they pass**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~SubBuilderCacheTests"
```

Expected: All 4 tests PASS.

- [ ] **Step 4.4: Commit**

```bash
git add Src/RCommon.Core/IRCommonBuilder.cs Src/RCommon.Core/RCommonBuilder.cs Tests/RCommon.Core.Tests/Bootstrapping/SubBuilderCacheTests.cs
git commit -m "feat(bootstrapping): add GetOrAddBuilder helper for sub-builder caching"
```

---

## Task 5: SingletonRegistration tracker — failing tests

**Files:**
- Create: `Tests/RCommon.Core.Tests/Bootstrapping/SingletonVerbConflictTests.cs`

- [ ] **Step 5.1: Create test file**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class SingletonVerbConflictTests
{
    [Fact]
    public void WithSimpleGuidGenerator_CalledTwice_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithSimpleGuidGenerator();
        Action secondCall = () => builder.WithSimpleGuidGenerator();

        secondCall.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(IGuidGenerator)).Should().Be(1);
    }

    [Fact]
    public void WithSequentialGuidGenerator_CalledTwice_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithSequentialGuidGenerator(o => { });
        Action secondCall = () => builder.WithSequentialGuidGenerator(o => { });

        secondCall.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(IGuidGenerator)).Should().Be(1);
    }

    [Fact]
    public void WithSimpleGuidGenerator_AfterSequentialGuidGenerator_ThrowsRCommonBuilderException()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithSequentialGuidGenerator(o => { });

        Action act = () => builder.WithSimpleGuidGenerator();

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*SequentialGuidGenerator*SimpleGuidGenerator*");
    }

    [Fact]
    public void WithSequentialGuidGenerator_AfterSimpleGuidGenerator_ThrowsRCommonBuilderException()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithSimpleGuidGenerator();

        Action act = () => builder.WithSequentialGuidGenerator(o => { });

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*SimpleGuidGenerator*SequentialGuidGenerator*");
    }

    [Fact]
    public void WithDateTimeSystem_CalledTwice_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithDateTimeSystem(o => { });
        Action secondCall = () => builder.WithDateTimeSystem(o => { });

        secondCall.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(ISystemTime)).Should().Be(1);
    }
}
```

- [ ] **Step 5.2: Run tests, observe failures**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~SingletonVerbConflictTests"
```

Expected: Several tests FAIL — current behavior throws on any second call regardless of type.

## Task 6: SingletonRegistration tracker — implementation

**Files:**
- Create: `Src/RCommon.Core/SingletonRegistration.cs`
- Modify: `Src/RCommon.Core/RCommonBuilder.cs`

- [ ] **Step 6.1: Create the SingletonRegistration struct**

`Src/RCommon.Core/SingletonRegistration.cs`:

```csharp
using System;

namespace RCommon
{
    /// <summary>
    /// Tracks whether a singleton-style RCommon verb has been configured and which implementation
    /// type was chosen. Used by verbs like <c>WithSimpleGuidGenerator</c> and <c>WithDateTimeSystem</c>
    /// to enforce same-type-idempotent / different-type-throw semantics across modular calls.
    /// </summary>
    internal struct SingletonRegistration
    {
        public bool Configured;
        public Type? ImplementationType;
    }
}
```

- [ ] **Step 6.2: Replace bool flags in `RCommonBuilder` with `SingletonRegistration` fields**

In `Src/RCommon.Core/RCommonBuilder.cs`, replace:

```csharp
private bool _guidConfigured = false;
private bool _dateTimeConfigured = false;
```

with:

```csharp
private SingletonRegistration _guidRegistration;
private SingletonRegistration _dateTimeRegistration;
```

- [ ] **Step 6.3: Rewrite `WithSequentialGuidGenerator` for new semantics**

```csharp
public IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions)
{
    if (_guidRegistration.Configured)
    {
        if (_guidRegistration.ImplementationType == typeof(SequentialGuidGenerator))
        {
            // Same impl re-registered: idempotent; just append the options delegate
            this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
            return this;
        }
        throw new RCommonBuilderException(
            $"IGuidGenerator already configured as '{_guidRegistration.ImplementationType?.FullName}'; " +
            $"cannot reconfigure as '{typeof(SequentialGuidGenerator).FullName}'. " +
            "To configure multiple modules consistently, ensure all modules agree on the same IGuidGenerator implementation, " +
            "or designate a single composition root that performs this registration.");
    }

    this.Services.Configure<SequentialGuidGeneratorOptions>(actions);
    this.Services.AddTransient<IGuidGenerator, SequentialGuidGenerator>();
    _guidRegistration = new SingletonRegistration { Configured = true, ImplementationType = typeof(SequentialGuidGenerator) };
    return this;
}
```

- [ ] **Step 6.4: Rewrite `WithSimpleGuidGenerator` for new semantics**

```csharp
public IRCommonBuilder WithSimpleGuidGenerator()
{
    if (_guidRegistration.Configured)
    {
        if (_guidRegistration.ImplementationType == typeof(SimpleGuidGenerator))
        {
            return this;
        }
        throw new RCommonBuilderException(
            $"IGuidGenerator already configured as '{_guidRegistration.ImplementationType?.FullName}'; " +
            $"cannot reconfigure as '{typeof(SimpleGuidGenerator).FullName}'. " +
            "To configure multiple modules consistently, ensure all modules agree on the same IGuidGenerator implementation, " +
            "or designate a single composition root that performs this registration.");
    }

    this.Services.AddScoped<IGuidGenerator, SimpleGuidGenerator>();
    _guidRegistration = new SingletonRegistration { Configured = true, ImplementationType = typeof(SimpleGuidGenerator) };
    return this;
}
```

- [ ] **Step 6.5: Rewrite `WithDateTimeSystem` for new semantics**

```csharp
public IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions)
{
    if (_dateTimeRegistration.Configured)
    {
        // Only one impl type exists; always idempotent. Still append the options delegate so
        // additional configuration accumulates per Options pattern.
        this.Services.Configure<SystemTimeOptions>(actions);
        return this;
    }

    this.Services.Configure<SystemTimeOptions>(actions);
    this.Services.AddTransient<ISystemTime, SystemTime>();
    _dateTimeRegistration = new SingletonRegistration { Configured = true, ImplementationType = typeof(SystemTime) };
    return this;
}
```

- [ ] **Step 6.6: Run new tests, verify pass**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~SingletonVerbConflictTests"
```

Expected: All 5 tests PASS.

- [ ] **Step 6.7: Update existing `RCommonBuilderTests` to reflect relaxed semantics**

In `Tests/RCommon.Core.Tests/RCommonBuilderTests.cs`:

- Delete `WithSequentialGuidGenerator_CalledTwice_ThrowsRCommonBuilderException` (lines ~134-147).
- Delete `WithSimpleGuidGenerator_CalledTwice_ThrowsRCommonBuilderException` (lines ~186-198).
- Delete `WithDateTimeSystem_CalledTwice_ThrowsRCommonBuilderException` (lines ~273-286).
- Keep `WithSimpleGuidGenerator_AfterSequentialGuidGenerator_ThrowsRCommonBuilderException` — still valid.

- [ ] **Step 6.8: Run the full RCommon.Core.Tests project to confirm no regressions**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj
```

Expected: All tests PASS.

- [ ] **Step 6.9: Commit**

```bash
git add Src/RCommon.Core/SingletonRegistration.cs Src/RCommon.Core/RCommonBuilder.cs Tests/RCommon.Core.Tests/Bootstrapping/SingletonVerbConflictTests.cs Tests/RCommon.Core.Tests/RCommonBuilderTests.cs
git commit -m "feat(bootstrapping): same-type idempotent, different-type throw for singleton verbs"
```

---

## Task 7: DataStoreFactoryOptions collision detection — failing tests

**Files:**
- Create: `Tests/RCommon.Persistence.Tests/Bootstrapping/DataStoreFactoryOptionsRegisterTests.cs`

- [ ] **Step 7.1: Create test file**

```csharp
using System;
using System.Data.Common;
using System.Threading.Tasks;
using FluentAssertions;
using RCommon.Persistence;
using Xunit;

namespace RCommon.Persistence.Tests.Bootstrapping;

public class DataStoreFactoryOptionsRegisterTests
{
    [Fact]
    public void Register_SameName_SameBase_SameConcrete_IsIdempotent()
    {
        var options = new DataStoreFactoryOptions();

        options.Register<FakeBase, FakeConcreteA>("DataStoreA");
        Action secondCall = () => options.Register<FakeBase, FakeConcreteA>("DataStoreA");

        secondCall.Should().NotThrow();
        options.Values.Should().HaveCount(1);
    }

    [Fact]
    public void Register_SameName_SameBase_DifferentConcrete_Throws()
    {
        var options = new DataStoreFactoryOptions();
        options.Register<FakeBase, FakeConcreteA>("DataStoreA");

        Action act = () => options.Register<FakeBase, FakeConcreteB>("DataStoreA");

        act.Should().Throw<UnsupportedDataStoreException>()
            .WithMessage("*DataStoreA*FakeConcreteA*FakeConcreteB*");
    }

    [Fact]
    public void Register_DifferentNames_RegistersBoth()
    {
        var options = new DataStoreFactoryOptions();

        options.Register<FakeBase, FakeConcreteA>("DataStoreA");
        options.Register<FakeBase, FakeConcreteB>("DataStoreB");

        options.Values.Should().HaveCount(2);
    }

    // DataStoreValue's constructor validates concreteType.BaseType == baseType (CLR base class,
    // not implemented interface), so fakes must inherit through a concrete abstract class.
    public abstract class FakeBase : IDataStore
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public DbConnection GetDbConnection() => throw new NotSupportedException("Test fake");
    }
    public class FakeConcreteA : FakeBase { }
    public class FakeConcreteB : FakeBase { }
}
```

- [ ] **Step 7.2: Run, verify failure**

```bash
dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~DataStoreFactoryOptionsRegisterTests"
```

Expected: First test FAILS — current `Register` throws on any duplicate `(name, B)` regardless of `C`.

## Task 8: DataStoreFactoryOptions collision detection — implementation

**Files:**
- Modify: `Src/RCommon.Persistence/DataStoreFactoryOptions.cs`

- [ ] **Step 8.1: Update Register logic**

Replace the method body:

```csharp
public void Register<B, C>(string name)
    where B : IDataStore
    where C : IDataStore
{
    var existing = Values.FirstOrDefault(x => x.Name == name && x.BaseType == typeof(B));
    if (existing is null)
    {
        Values.Add(new DataStoreValue(name, typeof(B), typeof(C)));
        return;
    }

    if (existing.ConcreteType == typeof(C))
    {
        return;
    }

    throw new UnsupportedDataStoreException(
        $"Data store '{name}' for base type '{typeof(B).GetGenericTypeName()}' is already registered with concrete type " +
        $"'{existing.ConcreteType.GetGenericTypeName()}'; cannot reconfigure as '{typeof(C).GetGenericTypeName()}'.");
}
```

`DataStoreValue` exposes the concrete type via the `ConcreteType` property (see `Src/RCommon.Persistence/DataStoreValue.cs:50`).

- [ ] **Step 8.2: Run tests, verify pass**

```bash
dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~DataStoreFactoryOptionsRegisterTests"
```

Expected: All 3 tests PASS.

- [ ] **Step 8.3: Run full Persistence test project**

```bash
dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj
```

Expected: All tests PASS (or any failures are pre-existing — verify by checking `git stash; dotnet test ...; git stash pop`).

- [ ] **Step 8.4: Commit**

```bash
git add Src/RCommon.Persistence/DataStoreFactoryOptions.cs Tests/RCommon.Persistence.Tests/Bootstrapping/DataStoreFactoryOptionsRegisterTests.cs
git commit -m "feat(persistence): allow idempotent re-registration of identical datastore mappings"
```

---

## Task 9: AddProducer descriptor-scan dedup — failing tests

**Files:**
- Create: `Tests/RCommon.Core.Tests/Bootstrapping/EventProducerDedupTests.cs`

- [ ] **Step 9.1: Create test file**

```csharp
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class EventProducerDedupTests
{
    [Fact]
    public void AddProducer_SameTypeCalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithEventHandling<TestEventHandlingBuilder>(eh =>
        {
            eh.AddProducer<TestProducer>();
            eh.AddProducer<TestProducer>();
        });

        var producerDescriptors = services
            .Where(d => d.ServiceType == typeof(IEventProducer) && d.ImplementationType == typeof(TestProducer))
            .ToList();
        producerDescriptors.Should().HaveCount(1);
    }

    [Fact]
    public void AddProducer_DifferentTypes_RegistersBoth()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithEventHandling<TestEventHandlingBuilder>(eh =>
        {
            eh.AddProducer<TestProducer>();
            eh.AddProducer<OtherTestProducer>();
        });

        services.Count(d => d.ServiceType == typeof(IEventProducer)).Should().Be(2);
    }

    public class TestEventHandlingBuilder : IEventHandlingBuilder
    {
        public TestEventHandlingBuilder(IRCommonBuilder builder) { Services = builder.Services; }
        public IServiceCollection Services { get; }
    }

    public class TestProducer : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }

    public class OtherTestProducer : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }
}
```

> The `IEventProducer` signature is verified from `Src/RCommon.Core/EventHandling/Producers/IEventProducer.cs`: `Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : ISerializableEvent`.

- [ ] **Step 9.2: Run, verify failure**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~EventProducerDedupTests"
```

Expected: `AddProducer_SameTypeCalledTwice_RegistersOnce` FAILS — current `AddSingleton<IEventProducer, T>` registers twice.

## Task 10: AddProducer descriptor-scan dedup — implementation

**Files:**
- Modify: `Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs`

- [ ] **Step 10.1: Update `AddProducer<T>()` (the parameterless overload)**

Replace the method body:

```csharp
public static void AddProducer<T>(this IEventHandlingBuilder builder)
    where T : class, IEventProducer
{
    var alreadyRegistered = builder.Services.Any(d =>
        d.ServiceType == typeof(IEventProducer) && d.ImplementationType == typeof(T));
    if (!alreadyRegistered)
    {
        builder.Services.AddSingleton<IEventProducer, T>();
    }

    var subscriptionManager = builder.Services.GetSubscriptionManager();
    subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
}
```

- [ ] **Step 10.2: Update `AddProducer<T>(Func<IServiceProvider, T>)` (factory overload)**

For the factory overload, descriptor inspection by `ImplementationType` doesn't work (factory descriptors set `ImplementationFactory`, not `ImplementationType`). Instead, gate on the existing `EventSubscriptionManager`'s tracking — its `AddProducerForBuilder` already uses a `HashSet<Type>` per builder type (verified in `Src/RCommon.Core/EventHandling/Producers/EventSubscriptionManager.cs:27-34`), so we just need a `HasProducerForBuilder` lookup method.

```csharp
public static void AddProducer<T>(this IEventHandlingBuilder builder, Func<IServiceProvider, T> getProducer)
    where T : class, IEventProducer
{
    var subscriptionManager = builder.Services.GetSubscriptionManager();
    var alreadyTracked = subscriptionManager?.HasProducerForBuilder(builder.GetType(), typeof(T)) ?? false;

    if (!alreadyTracked)
    {
        builder.Services.AddSingleton(getProducer);
    }

    subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
}
```

Add a new method to `Src/RCommon.Core/EventHandling/Producers/EventSubscriptionManager.cs`:

```csharp
/// <summary>
/// Returns true if the given producer type has already been registered through the given builder type.
/// </summary>
public bool HasProducerForBuilder(Type builderType, Type producerType)
{
    if (_builderProducerMap.TryGetValue(builderType, out var producers))
    {
        lock (producers)
        {
            return producers.Contains(producerType);
        }
    }
    return false;
}
```

(`AddProducerForBuilder` is already set-based, so calling it twice with the same pair is naturally idempotent — no further change needed there.)

- [ ] **Step 10.3: Update `AddProducer<T>(T producer)` (instance overload)**

Already uses `TryAddSingleton`, which is correct here. Verify the producer-for-builder tracking is also idempotent:

```csharp
public static void AddProducer<T>(this IEventHandlingBuilder builder, T producer)
    where T : class, IEventProducer
{
    builder.Services.TryAddSingleton(producer);
    builder.Services.TryAddSingleton<IEventProducer>(sp => sp.GetRequiredService<T>());

    if (producer is IHostedService service)
    {
        builder.Services.TryAddSingleton(service);
    }

    var subscriptionManager = builder.Services.GetSubscriptionManager();
    subscriptionManager?.AddProducerForBuilder(builder.GetType(), typeof(T));
}
```

(No code change required if the existing version already uses `TryAddSingleton` consistently — confirm by reading the file.)

- [ ] **Step 10.4: Run new tests, verify pass**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~EventProducerDedupTests"
```

Expected: Both tests PASS.

- [ ] **Step 10.5: Run full Core tests**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj
```

Expected: All tests PASS.

- [ ] **Step 10.6: Commit**

```bash
git add Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs Src/RCommon.Core/EventHandling/Producers/EventSubscriptionManager.cs Tests/RCommon.Core.Tests/Bootstrapping/EventProducerDedupTests.cs
git commit -m "feat(event-handling): descriptor-scan dedup for AddProducer<T>"
```

---

## Task 11: Route core verbs through GetOrAddBuilder

**Files:**
- Modify: `Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs`
- Modify: `Src/RCommon.Persistence/PersistenceBuilderExtensions.cs`

- [ ] **Step 11.1: Update `WithEventHandling<T>` to use GetOrAddBuilder**

Replace the body of both overloads (parameterless and `Action<T>`):

```csharp
public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder)
    where T : IEventHandlingBuilder
{
    return WithEventHandling<T>(builder, x => { });
}

public static IRCommonBuilder WithEventHandling<T>(this IRCommonBuilder builder, Action<T> actions)
    where T : IEventHandlingBuilder
{
    var eventHandlingConfig = builder.GetOrAddBuilder<T>(
        () => (T)Activator.CreateInstance(typeof(T), new object[] { builder })!);
    actions(eventHandlingConfig);
    return builder;
}
```

- [ ] **Step 11.2: Update `WithPersistence<T>` (current overloads)**

Replace the body of `WithPersistence<TObjectAccess>(builder, Action<TObjectAccess>)`:

```csharp
public static IRCommonBuilder WithPersistence<TObjectAccess>(this IRCommonBuilder builder, Action<TObjectAccess> objectAccessActions)
    where TObjectAccess : IPersistenceBuilder
{
    builder.Services.TryAddTransient<ITenantIdAccessor, NullTenantIdAccessor>();

    var dataConfiguration = builder.GetOrAddBuilder<TObjectAccess>(
        () => (TObjectAccess)Activator.CreateInstance(typeof(TObjectAccess), new object[] { builder.Services })!);
    objectAccessActions(dataConfiguration);
    builder = WithEventTracking(builder);
    return builder;
}
```

- [ ] **Step 11.3: Update `WithUnitOfWork<TUnitOfWork>`**

```csharp
public static IRCommonBuilder WithUnitOfWork<TUnitOfWork>(this IRCommonBuilder builder, Action<TUnitOfWork> unitOfWorkActions)
    where TUnitOfWork : IUnitOfWorkBuilder
{
    var unitOfWorkConfiguration = builder.GetOrAddBuilder<TUnitOfWork>(
        () => (TUnitOfWork)Activator.CreateInstance(typeof(TUnitOfWork), new object[] { builder.Services })!);
    unitOfWorkActions(unitOfWorkConfiguration);
    return builder;
}
```

- [ ] **Step 11.4: Update `WithEventTracking` to use TryAdd**

```csharp
private static IRCommonBuilder WithEventTracking(this IRCommonBuilder builder)
{
    builder.Services.TryAddScoped<IEventRouter, InMemoryTransactionalEventRouter>();
    builder.Services.TryAddScoped<IEntityEventTracker, InMemoryEntityEventTracker>();
    return builder;
}
```

- [ ] **Step 11.5: Update the four deprecated `WithPersistence<T,U>` overloads**

Apply the same `GetOrAddBuilder` routing to all four `[Obsolete]` overloads (at `Src/RCommon.Persistence/PersistenceBuilderExtensions.cs` lines 94, 111, 129, 148) so legacy users don't regress.

- [ ] **Step 11.6: Build the solution to surface any consumers that broke**

```bash
dotnet build Src/RCommon.sln
```

Expected: BUILD SUCCESS. No new errors.

- [ ] **Step 11.7: Run full Core and Persistence test suites**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj
dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj
```

Expected: All tests PASS.

- [ ] **Step 11.8: Commit**

```bash
git add Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs Src/RCommon.Persistence/PersistenceBuilderExtensions.cs
git commit -m "feat(bootstrapping): route core verbs through GetOrAddBuilder cache"
```

---

## Task 12: WithJsonSerialization singleton-style — failing tests

**Files:**
- Create: `Tests/RCommon.Json.Tests/Bootstrapping/JsonSerializationSingletonTests.cs`

- [ ] **Step 12.1: Read existing test base and verify Json.Tests has access to JsonNetBuilder + TextJsonBuilder**

```bash
ls Tests/RCommon.JsonNet.Tests Tests/RCommon.SystemTextJson.Tests
```

If `RCommon.Json.Tests` doesn't reference both builders, place this test file in `Tests/RCommon.JsonNet.Tests/Bootstrapping/` instead and reference both packages as project references (add to the .csproj).

- [ ] **Step 12.2: Create test file**

```csharp
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.JsonNet;
using RCommon.SystemTextJson;
using Xunit;

namespace RCommon.JsonNet.Tests.Bootstrapping;

public class JsonSerializationSingletonTests
{
    [Fact]
    public void WithJsonSerialization_SameType_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithJsonSerialization<JsonNetBuilder>();
        Action secondCall = () => builder.WithJsonSerialization<JsonNetBuilder>();

        secondCall.Should().NotThrow();
    }

    [Fact]
    public void WithJsonSerialization_SameType_BothActionsApplied()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        var firstActionRan = false;
        var secondActionRan = false;

        builder.WithJsonSerialization<JsonNetBuilder>(b => firstActionRan = true);
        builder.WithJsonSerialization<JsonNetBuilder>(b => secondActionRan = true);

        firstActionRan.Should().BeTrue();
        secondActionRan.Should().BeTrue();
    }

    [Fact]
    public void WithJsonSerialization_DifferentType_Throws()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithJsonSerialization<JsonNetBuilder>();

        Action act = () => builder.WithJsonSerialization<TextJsonBuilder>();

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*JsonNetBuilder*TextJsonBuilder*");
    }
}
```

- [ ] **Step 12.3: Run, verify failure**

```bash
dotnet test Tests/RCommon.JsonNet.Tests/RCommon.JsonNet.Tests.csproj --filter "FullyQualifiedName~JsonSerializationSingletonTests"
```

Expected: Tests FAIL — current impl creates duplicate descriptors.

## Task 13: WithJsonSerialization singleton-style — implementation

**Files:**
- Modify: `Src/RCommon.Json/JsonBuilderExtensions.cs`
- Modify: `Src/RCommon.Core/RCommonBuilder.cs` (add `_jsonRegistration`)
- Modify: `Src/RCommon.Core/IRCommonBuilder.cs` (if a public helper is needed; otherwise keep internal via a static API on the builder)

- [ ] **Step 13.1: Add an internal `TryRegisterJsonImplementation(Type)` helper**

The cleanest way to expose singleton-tracking to non-core packages without adding to `IRCommonBuilder` is to use the `GetOrAddBuilder` cache itself as the singleton check (since each concrete `T` gets one cache slot). If a different `T` is registered, throwing requires detecting a conflict before `GetOrAddBuilder` is called.

Approach: Introduce a `WithJsonSerializationCheck<T>` private helper in `JsonBuilderExtensions.cs` that scans `builder.Services` for `IJsonSerializer`/`IJsonBuilder`-registered descriptors and throws if the concrete impl differs. Use `GetOrAddBuilder<T>` for the cache.

Actually simpler: use the existence of *any* cached `IJsonBuilder` implementation in the builder's sub-builder cache as the conflict signal. Add an internal method to `RCommonBuilder`:

```csharp
// In RCommonBuilder.cs
internal Type? TryGetCachedSubBuilderImplementing(Type interfaceType)
{
    foreach (var key in _subBuilderCache.Keys)
    {
        if (interfaceType.IsAssignableFrom(key))
        {
            return key;
        }
    }
    return null;
}
```

Expose via a static helper class in `Src/RCommon.Core/Bootstrapping/RCommonBuilderInternals.cs`:

```csharp
namespace RCommon.Bootstrapping
{
    /// <summary>
    /// Internal helpers for singleton-style WithX verbs that live outside RCommon.Core.
    /// Not intended for use by application code.
    /// </summary>
    public static class RCommonBuilderInternals
    {
        public static Type? FindCachedImplementationOf<TInterface>(IRCommonBuilder builder)
        {
            return (builder as RCommonBuilder)?.TryGetCachedSubBuilderImplementing(typeof(TInterface));
        }
    }
}
```

- [ ] **Step 13.2: Update `WithJsonSerialization<T>` (primary 4-arg overload)**

```csharp
public static IRCommonBuilder WithJsonSerialization<T>(this IRCommonBuilder builder,
    Action<JsonSerializeOptions> serializeOptions,
    Action<JsonDeserializeOptions> deSerializeOptions,
    Action<T> actions)
    where T : IJsonBuilder
{
    Guard.IsNotNull(serializeOptions, nameof(serializeOptions));
    Guard.IsNotNull(deSerializeOptions, nameof(deSerializeOptions));
    Guard.IsNotNull(actions, nameof(actions));

    var existing = RCommonBuilderInternals.FindCachedImplementationOf<IJsonBuilder>(builder);
    if (existing is not null && existing != typeof(T))
    {
        throw new RCommonBuilderException(
            $"IJsonBuilder already configured as '{existing.FullName}'; " +
            $"cannot reconfigure as '{typeof(T).FullName}'. " +
            "To configure multiple modules consistently, ensure all modules agree on the same JSON serialization implementation.");
    }

    builder.Services.Configure<JsonSerializeOptions>(serializeOptions);
    // NOTE: deSerializeOptions is intentionally not wired up here. The existing implementation
    // (Src/RCommon.Json/JsonBuilderExtensions.cs:112) also does not call Configure<JsonDeserializeOptions>.
    // Preserving that pre-existing behavior keeps this change scope-limited to the singleton-style
    // migration; the missing wiring is tracked separately (or in a follow-up).

    var jsonConfig = builder.GetOrAddBuilder<T>(
        () => (T)Activator.CreateInstance(typeof(T), new object[] { builder })!);
    actions(jsonConfig);
    return builder;
}
```

(The five other overloads delegate to this one, so they pick up the new behavior automatically.)

- [ ] **Step 13.3: Run tests, verify pass**

```bash
dotnet test Tests/RCommon.JsonNet.Tests/RCommon.JsonNet.Tests.csproj --filter "FullyQualifiedName~JsonSerializationSingletonTests"
```

Expected: All 3 tests PASS.

- [ ] **Step 13.4: Run all Json-related test projects**

```bash
dotnet test Tests/RCommon.Json.Tests/RCommon.Json.Tests.csproj
dotnet test Tests/RCommon.JsonNet.Tests/RCommon.JsonNet.Tests.csproj
dotnet test Tests/RCommon.SystemTextJson.Tests/RCommon.SystemTextJson.Tests.csproj
```

Expected: All PASS.

- [ ] **Step 13.5: Commit**

```bash
git add Src/RCommon.Json/JsonBuilderExtensions.cs Src/RCommon.Core/RCommonBuilder.cs Src/RCommon.Core/Bootstrapping/RCommonBuilderInternals.cs Tests/RCommon.JsonNet.Tests/Bootstrapping/JsonSerializationSingletonTests.cs
git commit -m "feat(json): singleton-style WithJsonSerialization; same type idempotent, different throws"
```

---

## Task 14: Per-provider migrations — mechanical edits

For each provider extension below, replace `Activator.CreateInstance(...)` with `builder.GetOrAddBuilder<T>(() => Activator.CreateInstance(...))`. The factory's constructor argument matches whatever the existing code passes (`builder.Services` for most, `builder` for event-handling-style packages — read each file to determine).

Each file gets its own commit so the history stays bisectable.

- [ ] **Step 14.1: `Src/RCommon.Mediator/MediatorBuilderExtensions.cs`** (both `WithMediator<T>` overloads)

Read the file, then route through `GetOrAddBuilder`. Run `dotnet build Src/RCommon.Mediator/RCommon.Mediator.csproj` and `dotnet test Tests/RCommon.Mediator.Tests/RCommon.Mediator.Tests.csproj` — confirm green. Commit:

```bash
git add Src/RCommon.Mediator/MediatorBuilderExtensions.cs
git commit -m "feat(mediator): route WithMediator through GetOrAddBuilder cache"
```

- [ ] **Step 14.2: `Src/RCommon.Mediatr/MediatREventHandlingBuilderExtensions.cs`** (3 `WithEventHandling<T>` overloads)

Same pattern. Verify tests, commit:

```bash
git commit -m "feat(mediatr): route WithEventHandling through GetOrAddBuilder cache"
```

- [ ] **Step 14.3: `Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs`** (2 overloads)

```bash
git commit -m "feat(masstransit): route WithEventHandling through GetOrAddBuilder cache"
```

- [ ] **Step 14.4: `Src/RCommon.Caching/CachingBuilderExtensions.cs`** (`WithMemoryCaching<T>` × 2 + `WithDistributedCaching<T>` × 2)

```bash
git commit -m "feat(caching): route WithMemoryCaching and WithDistributedCaching through cache"
```

- [ ] **Step 14.5: `Src/RCommon.ApplicationServices/CqrsBuilderExtensions.cs`** (2 overloads)

```bash
git commit -m "feat(cqrs): route WithCQRS through GetOrAddBuilder cache"
```

- [ ] **Step 14.6: `Src/RCommon.ApplicationServices/ValidationBuilderExtensions.cs`** + `Src/RCommon.FluentValidation/ValidationBuilderExtensions.cs`

```bash
git commit -m "feat(validation): route WithValidation through GetOrAddBuilder cache"
```

- [ ] **Step 14.7: `Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs`** (2 overloads)

```bash
git commit -m "feat(blobs): route WithBlobStorage through GetOrAddBuilder cache"
```

- [ ] **Step 14.8: `Src/RCommon.MultiTenancy/MultiTenancyBuilderExtensions.cs`** (1 overload)

```bash
git commit -m "feat(multitenancy): route WithMultiTenancy through GetOrAddBuilder cache"
```

After each commit, build the full solution and run relevant per-package tests:

```bash
dotnet build Src/RCommon.sln
dotnet test Tests/<package>.Tests/<package>.Tests.csproj
```

Stop and investigate any failure. Do not proceed to the next provider until the current one is green.

---

## Task 15: TryAdd hardening for parameterless verbs

For verbs that take no generic argument (no sub-builder cache slot needed), make their underlying registrations idempotent by switching `services.AddXxx<I, Impl>()` to `services.TryAddXxx<I, Impl>()`.

- [ ] **Step 15.1: `Src/RCommon.Stateless/StatelessBuilderExtensions.cs` (`WithStatelessStateMachine`)**

Read the file. For each `services.Add*` call, switch to `services.TryAdd*`. Build, test, commit:

```bash
git commit -m "feat(stateless): idempotent registrations for WithStatelessStateMachine"
```

- [ ] **Step 15.2: `Src/RCommon.MassTransit.StateMachines/MassTransitStateMachineBuilderExtensions.cs`**

```bash
git commit -m "feat(masstransit-sm): idempotent registrations for WithMassTransitStateMachine"
```

- [ ] **Step 15.3: `Src/RCommon.Emailing/EmailingBuilderExtensions.cs` + `Src/RCommon.SendGrid/SendGridEmailingConfigurationExtensions.cs`**

These are singleton-style (only one `IEmailService` makes sense). Switch to `TryAdd*` and add a singleton-conflict guard: if a different `IEmailService` impl already registered, throw `RCommonBuilderException`. Detect via descriptor scan.

```bash
git commit -m "feat(email): singleton-style With*EmailServices with conflict detection"
```

- [ ] **Step 15.4: `Src/RCommon.Security/SecurityConfigurationExtensions.cs` + `Src/RCommon.Web/WebConfigurationExtensions.cs`**

`TryAdd*` swap.

```bash
git commit -m "feat(security): idempotent registrations for principal-accessor verbs"
```

---

## Task 16: Soft-duplicate finalize hosted service — failing tests

**Files:**
- Create: `Tests/RCommon.Core.Tests/Bootstrapping/SoftDuplicateDiagnosticsTests.cs`

- [ ] **Step 16.1: Create test file**

```csharp
using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class SoftDuplicateDiagnosticsTests
{
    [Fact]
    public async Task HostedService_WithSoftDuplicates_EmitsSingleWarning()
    {
        var capturedWarnings = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(capturedWarnings));
        services.AddLogging();

        services.AddRCommon();
        // Inject a duplicate registration to trigger soft-duplicate detection
        services.AddTransient<IFakeService, FakeServiceImpl>();
        services.AddTransient<IFakeService, FakeServiceImpl>();

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        foreach (var hs in hostedServices)
        {
            await hs.StartAsync(CancellationToken.None);
        }

        capturedWarnings.Should().HaveCount(1);
        capturedWarnings[0].Should().Contain("FakeServiceImpl");
    }

    [Fact]
    public async Task HostedService_NoSoftDuplicates_EmitsNoWarning()
    {
        var capturedWarnings = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(capturedWarnings));
        services.AddLogging();

        services.AddRCommon();

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>();
        foreach (var hs in hostedServices)
        {
            await hs.StartAsync(CancellationToken.None);
        }

        capturedWarnings.Should().BeEmpty();
    }

    [Fact]
    public async Task HostedService_CalledTwice_OnlyRunsScannerOnce()
    {
        var capturedWarnings = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(new TestLoggerFactory(capturedWarnings));
        services.AddLogging();

        services.AddRCommon();
        services.AddTransient<IFakeService, FakeServiceImpl>();
        services.AddTransient<IFakeService, FakeServiceImpl>();

        var provider = services.BuildServiceProvider();
        var hostedServices = provider.GetServices<IHostedService>().ToList();
        foreach (var hs in hostedServices)
        {
            await hs.StartAsync(CancellationToken.None);
            await hs.StartAsync(CancellationToken.None);
        }

        capturedWarnings.Should().HaveCount(1);
    }

    public interface IFakeService { }
    public class FakeServiceImpl : IFakeService { }

    private sealed class TestLoggerFactory : ILoggerFactory
    {
        private readonly List<string> _warnings;
        public TestLoggerFactory(List<string> warnings) { _warnings = warnings; }
        public void AddProvider(ILoggerProvider provider) { }
        public ILogger CreateLogger(string categoryName) => new TestLogger(_warnings);
        public void Dispose() { }
    }

    private sealed class TestLogger : ILogger
    {
        private readonly List<string> _warnings;
        public TestLogger(List<string> warnings) { _warnings = warnings; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                _warnings.Add(formatter(state, exception));
            }
        }
    }
}
```

- [ ] **Step 16.2: Run, verify failure**

Expected: Tests FAIL — `IHostedService` for diagnostics doesn't exist yet.

## Task 17: Soft-duplicate finalize hosted service — implementation

**Files:**
- Create: `Src/RCommon.Core/RCommonBootstrapDiagnosticsHostedService.cs`
- Modify: `Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs` (register the hosted service in `AddRCommon`)
- Modify: `Src/RCommon.Core/RCommonBuilder.cs` (add `_diagnosticsRun` flag and `_bootstrapDiagnostics` field + `GetBootstrapDiagnostics()`)
- Modify: `Src/RCommon.Core/IRCommonBuilder.cs` (add `GetBootstrapDiagnostics()`)

- [ ] **Step 17.1: Add `_diagnosticsRun` / `_bootstrapDiagnostics` to `RCommonBuilder`**

```csharp
private bool _diagnosticsRun;
private string _bootstrapDiagnostics = string.Empty;

internal bool TrySetDiagnosticsRun()
{
    if (_diagnosticsRun) return false;
    _diagnosticsRun = true;
    return true;
}

internal void StashDiagnostics(string message) => _bootstrapDiagnostics = message;

public string GetBootstrapDiagnostics() => _bootstrapDiagnostics;
```

- [ ] **Step 17.2: Add to `IRCommonBuilder`**

```csharp
string GetBootstrapDiagnostics();
```

- [ ] **Step 17.3: Create the hosted service**

`Src/RCommon.Core/RCommonBootstrapDiagnosticsHostedService.cs`:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RCommon
{
    /// <summary>
    /// Runs the duplicate-registration scanner once at host startup and emits a single
    /// warning (or stashes the message on the builder) if soft duplicates are detected.
    /// </summary>
    internal sealed class RCommonBootstrapDiagnosticsHostedService : IHostedService
    {
        private readonly IServiceCollection _services;
        private readonly IRCommonBuilder _builder;
        private readonly ILoggerFactory? _loggerFactory;

        public RCommonBootstrapDiagnosticsHostedService(
            IServiceCollection services,
            IRCommonBuilder builder,
            ILoggerFactory? loggerFactory = null)
        {
            _services = services;
            _builder = builder;
            _loggerFactory = loggerFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_builder is not RCommonBuilder rb || !rb.TrySetDiagnosticsRun())
            {
                return Task.CompletedTask;
            }

            var report = _services.GeneratePossibleDuplicatesServiceDescriptorsString();
            if (string.IsNullOrWhiteSpace(report))
            {
                return Task.CompletedTask;
            }

            rb.StashDiagnostics(report);

            if (_loggerFactory is not null)
            {
                var logger = _loggerFactory.CreateLogger<IRCommonBuilder>();
                logger.LogWarning("RCommon bootstrap detected duplicate service registrations:\n{Report}", report);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
```

- [ ] **Step 17.4: Register the hosted service from `AddRCommon`**

In `ServiceCollectionExtensions.AddRCommon`, after creating the new builder (the first-time branch only), add:

```csharp
services.AddSingleton<IHostedService>(sp =>
    new RCommonBootstrapDiagnosticsHostedService(
        services,
        sp.GetRequiredService<IRCommonBuilder>(),
        sp.GetService<ILoggerFactory>()));
```

Note: registering the hosted service inside the first-time branch (after the cache check) ensures it's only registered once even across multiple `AddRCommon` calls.

- [ ] **Step 17.5: Run new tests, verify pass**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~SoftDuplicateDiagnosticsTests"
```

Expected: All 3 tests PASS.

- [ ] **Step 17.6: Run full Core tests**

```bash
dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj
```

Expected: All tests PASS.

- [ ] **Step 17.7: Commit**

```bash
git add Src/RCommon.Core/RCommonBootstrapDiagnosticsHostedService.cs Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs Src/RCommon.Core/RCommonBuilder.cs Src/RCommon.Core/IRCommonBuilder.cs Tests/RCommon.Core.Tests/Bootstrapping/SoftDuplicateDiagnosticsTests.cs
git commit -m "feat(bootstrapping): finalize hosted service emits warning on soft duplicates"
```

---

## Task 18: Multi-module EF Core integration test

**Files:**
- Create: `Tests/RCommon.EfCore.Tests/Bootstrapping/MultiModuleEFCoreTests.cs`

- [ ] **Step 18.1: Read existing EFCore test fixtures to understand DbContext setup**

```bash
ls Tests/RCommon.EfCore.Tests
```

Identify an in-memory DbContext fixture (e.g., something using `UseInMemoryDatabase` or SQLite in-memory). Reuse it if available, otherwise add a minimal test DbContext to the new test file.

- [ ] **Step 18.2: Create test file**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using Xunit;

namespace RCommon.EfCore.Tests.Bootstrapping;

public class MultiModuleEFCoreTests
{
    [Fact]
    public void TwoModules_DistinctDbContextsDistinctNames_BothResolvable()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // Module A
        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("DbA-" + Guid.NewGuid())));

        // Module B
        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextB>("DbB", o => o.UseInMemoryDatabase("DbB-" + Guid.NewGuid())));

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDataStoreFactory>();
        factory.Resolve<RCommonDbContext>("DbA").Should().BeOfType<TestDbContextA>();
        factory.Resolve<RCommonDbContext>("DbB").Should().BeOfType<TestDbContextB>();
    }

    [Fact]
    public void TwoModules_SameNameSameContext_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("X")));
        Action secondModule = () => services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("X")));

        secondModule.Should().NotThrow();
    }

    [Fact]
    public void TwoModules_SameNameDifferentContext_Throws()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("X")));
        Action secondModule = () => services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextB>("DbA", o => o.UseInMemoryDatabase("Y")));

        secondModule.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void TwoModules_RepositoryDescriptors_NotDuplicated()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextA>("DbA", o => o.UseInMemoryDatabase("X")));
        services.AddRCommon().WithPersistence<EFCorePerisistenceBuilder>(ef =>
            ef.AddDbContext<TestDbContextB>("DbB", o => o.UseInMemoryDatabase("Y")));

        // EFCorePerisistenceBuilder registers IReadOnlyRepository<>, IWriteOnlyRepository<>, etc. in its ctor.
        // With caching, the ctor runs once, so each descriptor should appear exactly once.
        services.Count(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IReadOnlyRepository<>))
            .Should().Be(1);
    }

    public class TestDbContextA : RCommonDbContext
    {
        public TestDbContextA(DbContextOptions<TestDbContextA> options) : base(options) { }
    }

    public class TestDbContextB : RCommonDbContext
    {
        public TestDbContextB(DbContextOptions<TestDbContextB> options) : base(options) { }
    }
}
```

> Verify the exact `IDataStoreFactory.Resolve` signature, `RCommonDbContext` constructor signature, and `IReadOnlyRepository<>` namespace by reading the source. Adjust the test accordingly.

- [ ] **Step 18.3: Run, verify all pass**

```bash
dotnet test Tests/RCommon.EfCore.Tests/RCommon.EfCore.Tests.csproj --filter "FullyQualifiedName~MultiModuleEFCoreTests"
```

Expected: All 4 tests PASS.

- [ ] **Step 18.4: Commit**

```bash
git add Tests/RCommon.EfCore.Tests/Bootstrapping/MultiModuleEFCoreTests.cs
git commit -m "test(efcore): multi-module integration covering merge, idempotent, and conflict"
```

---

## Task 19: Multi-module MediatR integration test

**Files:**
- Create: `Tests/RCommon.Mediatr.Tests/Bootstrapping/MultiModuleMediatRTests.cs`

- [ ] **Step 19.1: Create test file**

Skeleton (adjust types based on actual MediatR builder API in `Src/RCommon.Mediatr/`):

```csharp
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Mediatr.Tests.Bootstrapping;

public class MultiModuleMediatRTests
{
    [Fact]
    public void TwoModules_DistinctProducers_BothRegister()
    {
        var services = new ServiceCollection();

        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerA>());
        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerB>());

        services.Count(d => d.ServiceType == typeof(IEventProducer)).Should().Be(2);
    }

    [Fact]
    public void TwoModules_SameProducer_RegistersOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerA>());
        services.AddRCommon().WithEventHandling<MediatREventHandlingBuilder>(eh =>
            eh.AddProducer<TestProducerA>());

        services.Count(d =>
            d.ServiceType == typeof(IEventProducer) && d.ImplementationType == typeof(TestProducerA))
            .Should().Be(1);
    }

    public class TestProducerA : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }

    public class TestProducerB : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }
}
```

- [ ] **Step 19.2: Run, verify pass**

```bash
dotnet test Tests/RCommon.Mediatr.Tests/RCommon.Mediatr.Tests.csproj --filter "FullyQualifiedName~MultiModuleMediatRTests"
```

Expected: Both tests PASS.

- [ ] **Step 19.3: Commit**

```bash
git add Tests/RCommon.Mediatr.Tests/Bootstrapping/MultiModuleMediatRTests.cs
git commit -m "test(mediatr): multi-module integration for event producers"
```

---

## Task 20: Final full-suite verification

- [ ] **Step 20.1: Build the whole solution**

```bash
dotnet build Src/RCommon.sln
```

Expected: BUILD SUCCESS with no new errors or warnings.

- [ ] **Step 20.2: Run every test project**

```bash
dotnet test Src/RCommon.sln
```

Expected: All tests PASS. Investigate any failure.

- [ ] **Step 20.3: Rebase / squash interim commits if desired**

Per CLAUDE.md: "Before finishing a feature/session, rebase and squash interim commits into a single meaningful commit summarizing the changes." This is at the user's discretion; do not squash without confirming.

- [ ] **Step 20.4: Open a PR**

```bash
gh pr create --title "feat(bootstrapping): modular composition of AddRCommon across in-process modules" --body "$(cat <<'EOF'
## Summary

Makes `services.AddRCommon()` and its fluent verbs composable across multiple modules in a single in-process .NET application without breaking existing single-call usage. Modules can each call `services.AddRCommon()....WithX(...)` and produce a coherent, deduplicated registration set.

Implements spec [`docs/specs/bootstrapping/bootstrapping.md`](docs/specs/bootstrapping/bootstrapping.md) per design [`docs/superpowers/specs/2026-05-15-modular-bootstrapper-design.md`](docs/superpowers/specs/2026-05-15-modular-bootstrapper-design.md).

Key changes:
- `IRCommonBuilder` cached as a `ServiceDescriptor.ImplementationInstance` on the `IServiceCollection`; repeated `AddRCommon()` returns the same instance.
- New `GetOrAddBuilder<T>(Func<T>)` helper on `IRCommonBuilder` for sub-builder caching; all `WithX<T>` extensions across the codebase route through it.
- Singleton-style verbs (`WithSimpleGuidGenerator`, `WithSequentialGuidGenerator`, `WithDateTimeSystem`, `WithJsonSerialization<T>`): same impl re-registered is idempotent, different impl throws `RCommonBuilderException` with both type names.
- `DataStoreFactoryOptions.Register<,>`: same `(name, base, concrete)` is idempotent, different concrete under same name throws `UnsupportedDataStoreException` (exception type preserved).
- `AddProducer<T>`: descriptor-scan dedup so identical producer types register once; distinct producer types still coexist.
- New `IHostedService` runs the existing duplicate-descriptor scanner at host startup and emits a single warning on soft duplicates.
- Strictly additive public API: `IRCommonBuilder.GetOrAddBuilder<T>`, `IRCommonBuilder.GetBootstrapDiagnostics()`, `ServiceCollectionExtensions.IsRCommonInitialized()`.

## Test plan

- [ ] All existing tests pass
- [ ] New `AddRCommonIdempotencyTests` cover top-level cache behavior
- [ ] New `SubBuilderCacheTests` cover `GetOrAddBuilder<T>` semantics
- [ ] New `SingletonVerbConflictTests` cover singleton-verb idempotency / conflict
- [ ] New `EventProducerDedupTests` cover `AddProducer<T>` dedup
- [ ] New `JsonSerializationSingletonTests` cover `WithJsonSerialization<T>` singleton behavior
- [ ] New `DataStoreFactoryOptionsRegisterTests` cover datastore-name collision
- [ ] New `SoftDuplicateDiagnosticsTests` cover hosted-service warning emission
- [ ] New `MultiModuleEFCoreTests` cover end-to-end EF Core multi-module integration
- [ ] New `MultiModuleMediatRTests` cover end-to-end MediatR multi-module integration
EOF
)"
```

(Only do this step when the user explicitly authorizes opening a PR.)

---

## Out of Scope

- Performance tuning of `GetOrAddBuilder` lookup.
- Concurrent bootstrap (explicitly out of contract per spec).
- An `IRCommonModule` abstraction.
- Migration of third-party `WithX` extensions — they continue to work uncached until they opt in.
- Documentation README updates beyond what's currently in the spec — those are tracked as "Nice to Have" and can be a follow-up PR.

## Open Items for the Engineer to Resolve at Implementation Time

- **`DataStoreValue.ConcreteType` property** — confirmed at `Src/RCommon.Persistence/DataStoreValue.cs:50`; Task 8 uses this name.
- **Exact `IEventProducer` method signature** — confirmed by reading `Src/RCommon.Core/EventHandling/Producers/IEventProducer.cs` before Task 9.
- **Wolverine `WithEventHandling<WolverineEventHandlingBuilder>` already routed via core** — no separate provider migration task needed for Wolverine.
- **`EventSubscriptionManager.HasProducerForBuilder` may already exist** under another name — read the file before Task 10 step 10.2 and reuse if possible.
