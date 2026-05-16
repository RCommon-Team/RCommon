# Modular Bootstrapper: Composable `AddRCommon` Across Modules

**Date:** 2026-05-15
**Status:** Draft
**Breaking Change:** No (strictly additive)

## Problem

`services.AddRCommon()` and its fluent verbs are designed for a single composition root. In modular host applications where multiple independent modules each want to register their own RCommon configuration (`Module A` configures EF Core + MediatR, `Module B` configures additional DbContexts + a different event producer), the current bootstrap produces broken or duplicated registrations:

- `AddRCommon()` creates a fresh `RCommonBuilder` per call. A second call re-registers `EventSubscriptionManager`, `IEventBus`, and `IEventRouter` as duplicate singletons, leaving the second registration's instance unused but resident.
- Instance flags `_guidConfigured` / `_dateTimeConfigured` on `RCommonBuilder` only guard within a single builder. Two `AddRCommon()` calls produce two builders, each with independent flags; the throw-on-second-call protection silently disappears.
- `WithPersistence<T>` and similar verbs `Activator.CreateInstance` a fresh sub-builder on every call. The sub-builder's constructor side-effects (e.g., `services.AddTransient(typeof(IReadOnlyRepository<>), typeof(EFCoreRepository<>))` in [`EFCorePerisistenceBuilder`](../../Src/RCommon.EfCore/EFCorePerisistenceBuilder.cs)) fire repeatedly, producing duplicate transient/scoped descriptors.
- Datastore-name registration via `DataStoreFactoryOptions.Register<,>` accumulates silently. Two modules registering the same name with different `TDbContext` types corrupt the registry without warning.

The existing [`GeneratePossibleDuplicatesServiceDescriptorsString`](../../Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs#L86-L123) diagnostic acknowledges the problem but is opt-in and runs after the fact.

## Goal

Make RCommon bootstrap **composable across modules** in a single process, such that multiple modules can each call `services.AddRCommon()...` and produce a coherent, deduplicated registration set — without changing any public API signatures or breaking existing single-call usage.

## Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Cross-call state mechanism | Cache `IRCommonBuilder` as a `ServiceDescriptor.ImplementationInstance` on the `IServiceCollection` | Idiomatic .NET pattern (MassTransit, MediatR, ASP.NET Identity); zero hidden static state; locally inspectable |
| Sub-builder cache key | Concrete sub-builder type (`typeof(EFCorePerisistenceBuilder)`) | Matches existing generic constraint shape; allows valid coexistence of alternative implementations; deterministic collision rule |
| Same-provider, same verb (e.g., `WithPersistence<EFCorePerisistenceBuilder>` ×2) | Merge — sub-builder cached, constructor runs once, each call's `Action<TSubBuilder>` runs against the cached instance | Module composition requires accumulation of configuration |
| Different-provider, same verb (e.g., `WithPersistence<EFCorePerisistenceBuilder>` + `WithPersistence<DapperPersistenceBuilder>`) | Allow both — each sub-builder cached independently | Mixed-provider scenarios are already a supported real-world pattern |
| Same datastore name, different `TDbContext` | Throw `RCommonBuilderException` from `DataStoreFactoryOptions.Register<,>` | Domain-level invariant: a datastore name must resolve to exactly one DbContext |
| Singleton-style verbs (`WithSimpleGuidGenerator`, `WithSequentialGuidGenerator`, `WithDateTimeSystem`, `WithSerialization<T>`), same impl re-registered | Idempotent no-op | Realistic modular scenario; matches Options pattern's natural behavior for delegate accumulation |
| Singleton-style verbs, different impl | Throw `RCommonBuilderException` | Today's flag-based protection preserved; ambiguous which module's choice should win |
| `AddProducer<T>` with same producer type repeated | Idempotent — single `IEventProducer` descriptor | No real scenario benefits from N instances of the same producer |
| `AddProducer<T>` with different producer types | Coexist | Already the intended design |
| Soft-duplicate detection | Eager hard-throws + auto-warning at finalize via existing duplicate scanner | Hard conflicts fail at the call site; soft duplicates surface visibly but non-fatally |
| Thread-safety contract | Bootstrap is single-threaded; documented, not enforced | Matches .NET-wide assumption about `IServiceCollection` |

## Design

### Architecture

The bootstrap pipeline gains one concept: **shared bootstrap state**, scoped to one `IServiceCollection`, materialized as the cached `IRCommonBuilder` instance.

```
IServiceCollection
  │
  └─[ServiceDescriptor.Singleton(IRCommonBuilder, instance)]──► IRCommonBuilder
                                                                  ├─ Services (the IServiceCollection)
                                                                  ├─ subBuilderCache  : Dictionary<Type, object>
                                                                  ├─ guidRegistration : SingletonRegistration
                                                                  ├─ dateTimeRegistration : SingletonRegistration
                                                                  ├─ serializerRegistration : SingletonRegistration
                                                                  └─ bootstrapDiagnostics : RCommonBootstrapDiagnostics
```

**`AddRCommon()` flow:**
1. Look for a `ServiceDescriptor` with `ServiceType == typeof(IRCommonBuilder)` in `services`.
2. Found → return `(IRCommonBuilder)descriptor.ImplementationInstance`. No core registrations re-run.
3. Not found → construct `RCommonBuilder`, register via `services.AddSingleton<IRCommonBuilder>(builder)`, run one-time core registrations (`EventSubscriptionManager`, `IEventBus`, `IEventRouter`, `CachingOptions`) — these existing calls switch to `TryAdd*` variants for additional defense.

**`WithX<TSubBuilder>(...)` flow** (where `WithX` is `WithPersistence`, `WithUnitOfWork`, `WithMediator`, `WithEventHandling`, `WithCaching`, etc.):
1. Call `builder.GetOrAddBuilder<TSubBuilder>(services => new TSubBuilder(services))`.
2. First call for `TSubBuilder`: helper instantiates via the factory (sub-builder's constructor runs and registers its services exactly once), caches under `typeof(TSubBuilder)`.
3. Subsequent calls for same `TSubBuilder`: helper returns the cached instance without re-invoking the factory.
4. Caller runs the user-supplied `Action<TSubBuilder>` against the returned instance — accumulation is automatic.

**Singleton-style verb flow** (e.g., `WithSimpleGuidGenerator`):
1. Inspect builder's `guidRegistration` (a `SingletonRegistration` struct: `(bool Configured, Type? ImplType)`).
2. Not configured → register the implementation, record `(true, typeof(SimpleGuidGenerator))`.
3. Configured with same `ImplType` → no-op (the Options pattern naturally accumulates `Action<TOptions>` delegates if one is supplied).
4. Configured with different `ImplType` → throw `RCommonBuilderException` with both type names and remediation hint.

**Finalize flow** (`Configure()` or first resolve of `IRCommonBuilder`):
1. Run `GeneratePossibleDuplicatesServiceDescriptorsString` once.
2. Non-empty result + `ILoggerFactory` resolvable → log as `Warning`.
3. Non-empty result + no logger → stash on `bootstrapDiagnostics` for retrieval via `builder.GetBootstrapDiagnostics()`.

### New public surface

```csharp
namespace RCommon;

public interface IRCommonBuilder
{
    // existing members preserved unchanged

    /// <summary>
    /// Returns the cached sub-builder for <typeparamref name="TSubBuilder"/> if one exists,
    /// otherwise invokes <paramref name="factory"/>, caches the result, and returns it.
    /// </summary>
    /// <typeparam name="TSubBuilder">Concrete sub-builder type (e.g., <c>EFCorePerisistenceBuilder</c>).</typeparam>
    /// <param name="factory">Factory invoked exactly once per <typeparamref name="TSubBuilder"/> per <see cref="IRCommonBuilder"/>.</param>
    TSubBuilder GetOrAddBuilder<TSubBuilder>(Func<IServiceCollection, TSubBuilder> factory)
        where TSubBuilder : class;

    /// <summary>
    /// Returns soft-duplicate diagnostic output stashed at finalize when no <see cref="ILoggerFactory"/>
    /// was available. Empty string if no duplicates were detected.
    /// </summary>
    string GetBootstrapDiagnostics();
}

public static class ServiceCollectionExtensions
{
    // existing members preserved unchanged

    /// <summary>
    /// Returns <c>true</c> if <see cref="AddRCommon"/> has been invoked against this collection.
    /// </summary>
    public static bool IsRCommonInitialized(this IServiceCollection services);
}
```

No removals, no signature changes.

### Modified call sites

| File | Change |
|---|---|
| [`Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs`](../../Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs) | `AddRCommon()` looks up the cached `IRCommonBuilder` descriptor; returns cached if present, otherwise constructs and registers as singleton instance. Adds `IsRCommonInitialized()`. |
| [`Src/RCommon.Core/RCommonBuilder.cs`](../../Src/RCommon.Core/RCommonBuilder.cs) | Core registrations (`EventSubscriptionManager`, `IEventBus`, `IEventRouter`) switch to `TryAddSingleton`/`TryAddScoped`. Bare `_guidConfigured`/`_dateTimeConfigured` bools replaced with `SingletonRegistration` records that track impl type. Implements `GetOrAddBuilder<T>` and `GetBootstrapDiagnostics()`. |
| [`Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs`](../../Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs) | `WithEventHandling<T>` routes through `GetOrAddBuilder<T>`. `AddProducer<T>` (all three overloads) switches to `TryAddSingleton` so the same producer type registers once. `EventSubscriptionManager.AddProducerForBuilder` becomes set-based to ignore repeated identical associations. |
| [`Src/RCommon.Persistence/PersistenceBuilderExtensions.cs`](../../Src/RCommon.Persistence/PersistenceBuilderExtensions.cs) | `WithPersistence<T>` and `WithUnitOfWork<T>` route through `GetOrAddBuilder<T>`. `WithEventTracking` switches `AddScoped` → `TryAddScoped`. Deprecated `WithPersistence<T,U>` overloads receive the same routing. |
| `Src/RCommon.Mediator/MediatorBuilderExtensions.cs` | `WithMediator<T>` routes through `GetOrAddBuilder<T>`. |
| `Src/RCommon.Mediatr/MediatREventHandlingBuilderExtensions.cs` | Nested registrations switch to `TryAdd*` where appropriate. |
| `Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs` | Same pattern. |
| `Src/RCommon.Wolverine/` (event-handling & mediator builders) | Same pattern. |
| `Src/RCommon.MemoryCache/` (caching builder extension) | Route `WithCaching<MemoryCachingBuilder>` through `GetOrAddBuilder`. |
| `Src/RCommon.RedisCache/` (caching builder extension) | Same. |
| `Src/RCommon.JsonNet/`, `Src/RCommon.SystemTextJson/` | `WithSerialization<T>` becomes singleton-style — same type idempotent, different type throws via the builder's `serializerRegistration` slot. |
| `Src/RCommon.EfCore/` `DataStoreFactoryOptions.Register<TBase, TConcrete>(string name)` | Adds duplicate-name detection: same name + same `TConcrete` is idempotent; same name + different `TConcrete` throws `RCommonBuilderException`. |

**Unchanged:** all sub-builder constructors (caching prevents re-entry), all public fluent surface signatures, the `RCommonBuilder` constructor's external behavior, all existing test fixtures for single-call usage.

### Conflict matrix (authoritative)

| Verb / scenario | Same impl | Different impl | Notes |
|---|---|---|---|
| `AddRCommon()` | Idempotent (returns cached builder) | n/a | Core registrations fire exactly once |
| `WithPersistence<T>` | Merge (cached sub-builder, action accumulates) | Both register | |
| `WithUnitOfWork<T>` | Merge | Both register | |
| `WithMediator<T>` | Merge | Both register | |
| `WithEventHandling<T>` | Merge | Both register | |
| `WithCaching<T>` | Merge | Both register | |
| `WithSerialization<T>` | Idempotent | Throws | Singleton-style |
| `WithSequentialGuidGenerator` / `WithSimpleGuidGenerator` | Idempotent | Throws | Singleton-style |
| `WithDateTimeSystem` | Idempotent (options actions accumulate) | n/a (one impl type) | |
| `WithCommonFactory<TService, TImpl>` | Idempotent (`TryAdd`) | Last-wins (standard DI) + soft-dup warning | Not policed by RCommon |
| `AddProducer<T>` | Idempotent | Coexist | |
| `IEFCorePersistenceBuilder.AddDbContext<TDbContext>(name, ...)` | Idempotent (same name + same `TDbContext`) | Throws (same name + different `TDbContext`) | Enforced in `DataStoreFactoryOptions.Register<,>` |

### Error message shape

Every `RCommonBuilderException` thrown by these rules includes:
- The conflicting verb name (e.g., `"WithSimpleGuidGenerator"`)
- The previously-recorded implementation type's full name
- The newly-attempted implementation type's full name
- A short remediation hint: `"To configure multiple modules consistently, ensure all modules agree on the same X implementation, or designate a single composition root that performs this registration."`

No structured error metadata beyond the message. Stack trace identifies the offending call site.

### Lifetime & thread safety

- The cached `IRCommonBuilder` is held by the `IServiceCollection` via its singleton descriptor and transitions into the `ServiceProvider` on `BuildServiceProvider()`. Heap retention: a few kilobytes per provider for the lifetime of the host.
- The builder holds no unmanaged resources; no `IDisposable` contract.
- Bootstrap is **not thread-safe**. Documented contract: all `AddRCommon()` and `With*` invocations must be serialized at the host level. Matches the `IServiceCollection`-wide convention.

### Diagnostics

The existing [`GeneratePossibleDuplicatesServiceDescriptorsString`](../../Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs#L86-L123) scanner is wired to run automatically at finalize:
- `IRCommonBuilder.Configure()` invocation, or
- First resolve of `IRCommonBuilder` from the built provider (hooked via an internal `IHostedService` or `IPostConfigureOptions`).

Emission:
- `ILoggerFactory` resolvable → single `LogLevel.Warning` containing the report.
- Not resolvable → message stashed; `builder.GetBootstrapDiagnostics()` returns it.

The scanner runs at most once per builder instance.

## Migration & backward compatibility

**Source compatibility:** strictly additive. No existing signature changes, no member removals. Existing single-call code recompiles without diff.

**Behavioral compatibility:**
- Single `AddRCommon()` + single set of `With*` calls: bit-identical final `IServiceCollection` contents (the new `TryAdd` paths have nothing to deduplicate).
- Two `AddRCommon()` calls with no `With*` between: today, two `RCommonBuilder` instances are created but only the second's state is reachable to the caller and the first's redundant core descriptors sit unused. After the change, both calls return the same builder with a single set of core descriptors. **Net registration outcome is functionally equivalent** for any code that wasn't broken.
- `_guidConfigured` / `_dateTimeConfigured` exception relaxation: same-type re-registration becomes idempotent (was unconditional throw). Pure relaxation — code that wasn't throwing won't start throwing.

**Third-party `WithX` extensions:** continue to compile and run, but won't benefit from the cache until migrated to `GetOrAddBuilder<T>`. Migration is mechanical:

```csharp
// Before
var sub = (TSub)Activator.CreateInstance(typeof(TSub), new object[] { builder.Services })!;
actions(sub);

// After
var sub = builder.GetOrAddBuilder<TSub>(services => new TSub(services));
actions(sub);
```

**Deprecated overloads:** the `[Obsolete]` `WithPersistence<TObjectAccess, TUnitOfWork>` overloads receive the same `GetOrAddBuilder` routing to prevent legacy users from regressing.

## Failure modes

| Failure | Surface |
|---|---|
| Conflicting singleton types across modules | `RCommonBuilderException` thrown synchronously from the second `With*` call |
| Same datastore name with different `TDbContext` | `RCommonBuilderException` from `DataStoreFactoryOptions.Register<,>` (eager — fires at the `AddDbContext` call) |
| Soft duplicates in service descriptors | Single `LogLevel.Warning` at finalize, or stashed in `GetBootstrapDiagnostics()` |
| `null` `IServiceCollection` | Existing `Guard.Against<NullReferenceException>` continues to throw |
| Sub-builder construction failure | Bubbles from `Activator.CreateInstance` / `new TSubBuilder(services)` as today |

## Testing

### Layout

```
Tests/
  RCommon.Tests/Bootstrapping/
    AddRCommonIdempotencyTests.cs
    SubBuilderCacheTests.cs
    SingletonVerbConflictTests.cs
    SoftDuplicateDiagnosticsTests.cs
  RCommon.EfCore.Tests/Bootstrapping/
    MultiModuleEFCoreTests.cs
  RCommon.Mediatr.Tests/Bootstrapping/
    MultiModuleMediatRTests.cs
```

### Coverage

**`AddRCommonIdempotencyTests`**
- Two `AddRCommon()` calls return the reference-equal `IRCommonBuilder`.
- After two calls, descriptor count for `EventSubscriptionManager`, `IEventBus`, `IEventRouter`, `CachingOptions` is 1 each.
- After two no-`With*` calls, descriptor count is identical to one call.
- `IsRCommonInitialized()` is `false` before, `true` after.

**`SubBuilderCacheTests`**
- `WithPersistence<T>` twice → sub-builder constructor invoked once (counter in test-only sub-builder).
- Each call's `Action<TSubBuilder>` runs against the same cached instance.
- `WithPersistence<EFCorePerisistenceBuilder>` and `WithPersistence<DapperPersistenceBuilder>` produce two distinct cached sub-builders.
- `GetOrAddBuilder<T>` invokes the factory exactly once across repeated lookups.

**`SingletonVerbConflictTests`**
- `WithSimpleGuidGenerator()` twice → no throw, single `IGuidGenerator` descriptor.
- `WithSimpleGuidGenerator()` then `WithSequentialGuidGenerator(...)` → throws; message names both types.
- Symmetric: reverse order → throws with equivalent message.
- `WithDateTimeSystem(...)` twice with the same options action → no throw.
- `WithSerialization<NewtonsoftJsonBuilder>` then `WithSerialization<SystemTextJsonBuilder>` → throws.

**`SoftDuplicateDiagnosticsTests`**
- Manually injecting a duplicate `(IFoo, FooImpl)` after `AddRCommon` → at finalize, one warning logged through a test `ILoggerFactory`.
- Zero soft duplicates → no warning emitted.
- Warning emitted at most once across repeated `Configure()` calls.
- `GetBootstrapDiagnostics()` returns the same message when no logger factory is registered.

**`MultiModuleEFCoreTests`**
- Two simulated modules each call `AddRCommon().WithPersistence<EFCorePerisistenceBuilder>` registering distinct DbContexts under distinct names. Built provider resolves `IDataStoreFactory` and obtains both DbContexts.
- Identical `AddDbContext<DbContextA>("DbA", ...)` from two modules → no throw, single registration.
- Same name + different `TDbContext` across two modules → throws.
- Two-module scenario produces exactly 1 descriptor for each repository interface (`IReadOnlyRepository<>`, `IWriteOnlyRepository<>`, etc.), proving constructor side-effects ran once.

**`MultiModuleMediatRTests`**
- Two modules each call `WithEventHandling<MediatREventHandlingBuilder>` and `AddProducer<DistinctProducerA>` / `AddProducer<DistinctProducerB>` → both producers register, both receive events.
- Two modules both call `AddProducer<SameProducer>` → exactly one descriptor.

### TDD ordering

1. `AddRCommonIdempotencyTests` → drives the cached-builder mechanic in `ServiceCollectionExtensions`.
2. `SubBuilderCacheTests` → drives `GetOrAddBuilder<T>` on `IRCommonBuilder`.
3. `SingletonVerbConflictTests` → drives the `SingletonRegistration` tracker.
4. `MultiModuleEFCoreTests` → drives `DataStoreFactoryOptions.Register<,>` collision detection plus full integration.
5. `SoftDuplicateDiagnosticsTests` → drives the finalize-time scanner.
6. Provider migrations come last; each is a small mechanical edit + smoke test.

## Out of scope

- Each provider's own functional correctness (already covered by existing test suites).
- Concurrent bootstrap. Explicitly out of contract.
- Performance tuning of `GetOrAddBuilder` lookup (`Dictionary<Type, object>` is O(1); bootstrap path is one-time-at-startup).
- A separate `IRCommonModule` abstraction. The user-facing pattern remains "modules call `services.AddRCommon()`".
