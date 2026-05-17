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
| Same datastore name, different concrete `TDbContext` | Throw `UnsupportedDataStoreException` from `DataStoreFactoryOptions.Register<,>` | Domain-level invariant: a datastore name must resolve to exactly one DbContext. Preserves the existing exception type so caller `catch` blocks aren't broken |
| Same datastore name, same `TDbContext` (modular re-registration) | Idempotent no-op | Relaxes today's unconditional throw on duplicate name+base — required for modular composition |
| Singleton-style verbs (`WithSimpleGuidGenerator`, `WithSequentialGuidGenerator`, `WithDateTimeSystem`, `WithJsonSerialization<T>`), same impl re-registered | Idempotent no-op | Realistic modular scenario; matches Options pattern's natural behavior for delegate accumulation |
| Singleton-style verbs, different impl | Throw `RCommonBuilderException` | Today's flag-based protection preserved; ambiguous which module's choice should win |
| `AddProducer<T>` with same producer type repeated | Idempotent — single `IEventProducer` descriptor for that concrete `T`. Implemented by scanning `services` for an existing `ServiceDescriptor` where `ServiceType == typeof(IEventProducer) && ImplementationType == typeof(T)` (or equivalent factory inspection) before adding — **not** `TryAddSingleton<IEventProducer, T>`, which would also block subsequent distinct producer types | No real scenario benefits from N instances of the same producer |
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
2. Found → return `(IRCommonBuilder)descriptor.ImplementationInstance`. **Return immediately — no further registrations run.**
3. Not found → construct `RCommonBuilder`, register it via `services.AddSingleton<IRCommonBuilder>(builder)`, then run the one-time core registrations (`EventSubscriptionManager`, `IEventBus`, `IEventRouter`, `CachingOptions`).

The cache-lookup-and-early-return in step 2 is the **primary** dedup mechanism. The core registrations in step 3 keep their current call shape (`AddSingleton`, `AddScoped`) — they are unreachable on the second call because step 2 already returned. No `TryAdd*` defensive layer is needed here, and adding one would be misleading (it would suggest the path is reachable when it isn't).

**`WithX<TSubBuilder>(...)` flow** (where `WithX` is `WithPersistence`, `WithUnitOfWork`, `WithMediator`, `WithEventHandling`, `WithMemoryCaching`, `WithDistributedCaching`, `WithJsonSerialization`, `WithCQRS`, `WithValidation`, `WithBlobStorage`, `WithMultiTenancy`):
1. Call `builder.GetOrAddBuilder<TSubBuilder>(() => new TSubBuilder(/* builder.Services or builder, per the sub-builder's ctor */))`.
2. First call for `TSubBuilder`: helper invokes the factory (sub-builder's constructor runs and registers its services exactly once), caches under `typeof(TSubBuilder)`.
3. Subsequent calls for same `TSubBuilder`: helper returns the cached instance without re-invoking the factory.
4. Caller runs the user-supplied `Action<TSubBuilder>` against the returned instance — accumulation is automatic.

The factory is parameterless because sub-builder constructors in the existing codebase take heterogeneous arguments: persistence sub-builders take `IServiceCollection`, event-handling and JSON sub-builders take `IRCommonBuilder`. The caller closes over whichever it needs.

**Singleton-style verb flow** (e.g., `WithSimpleGuidGenerator`):
1. Inspect builder's `guidRegistration` — a small mutable struct with two fields: `bool Configured` and `Type? ImplType`.
2. Not configured → register the implementation, set `Configured = true`, `ImplType = typeof(SimpleGuidGenerator)`.
3. Configured with same `ImplType` → no-op (the Options pattern naturally accumulates `Action<TOptions>` delegates if one is supplied).
4. Configured with different `ImplType` → throw `RCommonBuilderException` with both type names and remediation hint.

(Using a mutable struct rather than a `record` because the field gets reassigned in place during the verb call; `record` semantics would require constant reconstruction.)

**Finalize flow:**

The finalize step has a single source of truth: an internal `IHostedService` registered automatically during the first `AddRCommon()` call. It runs once on host startup (after the full DI container is built and all modules have completed their `With*` calls), invokes the scanner once, emits the warning or stashes the message, and then sets a `_diagnosticsRun` flag on the builder so re-entrance is a no-op.

`IRCommonBuilder.Configure()` (the existing method that returns `IServiceCollection`) does **not** trigger the scanner. It continues to return `Services` unchanged, preserving today's semantics for callers that invoke it explicitly mid-pipeline.

1. Hosted service `StartAsync` fires.
2. If `_diagnosticsRun` is `true`, return. Otherwise set it to `true`.
3. Run `GeneratePossibleDuplicatesServiceDescriptorsString`.
4. Non-empty result + `ILoggerFactory` resolvable → log as `Warning`.
5. Non-empty result + no logger → stash on `bootstrapDiagnostics` for retrieval via `builder.GetBootstrapDiagnostics()`.

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
    /// <param name="factory">Parameterless factory invoked exactly once per <typeparamref name="TSubBuilder"/> per <see cref="IRCommonBuilder"/>. Callers close over whatever constructor argument they need — typically <c>builder.Services</c> or <c>builder</c> itself, depending on the sub-builder's constructor.</param>
    TSubBuilder GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder> factory)
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
| [`Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs`](../../Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs) | `AddRCommon()` looks up the cached `IRCommonBuilder` descriptor; returns cached if present, otherwise constructs and registers as singleton instance. Registers the finalize `IHostedService`. Adds `IsRCommonInitialized()`. |
| [`Src/RCommon.Core/RCommonBuilder.cs`](../../Src/RCommon.Core/RCommonBuilder.cs) | Bare `_guidConfigured` (current text: `"Guid Generator has already been configured once. You cannot configure multiple times"`) and `_dateTimeConfigured` (current text: `"Date/Time System has already been configured once. You cannot configure multiple times"`) replaced with `SingletonRegistration` structs tracking impl type. Verbs relaxed to idempotent on same-type, throw on different-type. Implements `GetOrAddBuilder<T>` and `GetBootstrapDiagnostics()`. |
| [`Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs`](../../Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs) | `WithEventHandling<T>` routes through `GetOrAddBuilder<T>`. `AddProducer<T>` (all three overloads) scans `services` for an existing `(IEventProducer, T)` descriptor and skips if present; **does not** use `TryAddSingleton` (which would also block distinct producer types). `EventSubscriptionManager.AddProducerForBuilder` becomes set-based to ignore repeated identical associations. |
| [`Src/RCommon.Persistence/PersistenceBuilderExtensions.cs`](../../Src/RCommon.Persistence/PersistenceBuilderExtensions.cs) | `WithPersistence<T>` and `WithUnitOfWork<T>` route through `GetOrAddBuilder<T>`. `WithEventTracking` switches `AddScoped` → `TryAddScoped`. Deprecated `WithPersistence<T,U>` overloads receive the same routing. |
| [`Src/RCommon.Persistence/DataStoreFactoryOptions.cs`](../../Src/RCommon.Persistence/DataStoreFactoryOptions.cs) | `Register<B, C>(string name)` semantics changed: (`name`, `B`, `C`) identical = idempotent no-op; (`name`, `B`) identical but `C` differs = throws `UnsupportedDataStoreException` with both concrete-type names. Exception type **preserved** — does not change to `RCommonBuilderException`. |
| [`Src/RCommon.Mediator/MediatorBuilderExtensions.cs`](../../Src/RCommon.Mediator/MediatorBuilderExtensions.cs) | `WithMediator<T>` (both overloads) routes through `GetOrAddBuilder<T>`. |
| [`Src/RCommon.Mediatr/MediatREventHandlingBuilderExtensions.cs`](../../Src/RCommon.Mediatr/MediatREventHandlingBuilderExtensions.cs) | `WithEventHandling<T>` (all three overloads) routes through `GetOrAddBuilder<T>`. Internal MediatR `services.AddMediatR` accumulates naturally and does not need additional dedup. |
| [`Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs`](../../Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs) | `WithEventHandling<T>` (both overloads) routes through `GetOrAddBuilder<T>`. |
| [`Src/RCommon.Wolverine/`](../../Src/RCommon.Wolverine/) | Any `WithMediator<WolverineMediatorBuilder>` / `WithEventHandling<WolverineEventHandlingBuilder>` extensions in this package route through `GetOrAddBuilder<T>`. (Exact filenames to be enumerated during implementation; pattern is uniform.) |
| [`Src/RCommon.Caching/CachingBuilderExtensions.cs`](../../Src/RCommon.Caching/CachingBuilderExtensions.cs) | `WithMemoryCaching<T>` and `WithDistributedCaching<T>` (both overloads each) route through `GetOrAddBuilder<T>`. |
| [`Src/RCommon.MemoryCache/`](../../Src/RCommon.MemoryCache/) | Sub-builder side of `WithMemoryCaching<MemoryCachingBuilder>` — no code change needed if it follows the constructor-side-effect pattern; caching at the parent prevents re-entry. |
| [`Src/RCommon.RedisCache/`](../../Src/RCommon.RedisCache/) | Same. |
| [`Src/RCommon.Json/JsonBuilderExtensions.cs`](../../Src/RCommon.Json/JsonBuilderExtensions.cs) | `WithJsonSerialization<T>` becomes singleton-style across all **six** existing overloads (parameterless, serialize-only, deserialize-only, both, action-only, full). The builder's `jsonRegistration` slot tracks the impl type; same `T` = idempotent (the `Action<T>` is still applied to the cached instance via `GetOrAddBuilder<T>`); different `T` = throw `RCommonBuilderException`. |
| [`Src/RCommon.JsonNet/`](../../Src/RCommon.JsonNet/), [`Src/RCommon.SystemTextJson/`](../../Src/RCommon.SystemTextJson/) | Concrete builder types are `JsonNetBuilder` and `TextJsonBuilder`. No constructor changes needed. |
| [`Src/RCommon.ApplicationServices/CqrsBuilderExtensions.cs`](../../Src/RCommon.ApplicationServices/CqrsBuilderExtensions.cs) | `WithCQRS<T>` (both overloads) routes through `GetOrAddBuilder<T>`. |
| [`Src/RCommon.ApplicationServices/ValidationBuilderExtensions.cs`](../../Src/RCommon.ApplicationServices/ValidationBuilderExtensions.cs), [`Src/RCommon.FluentValidation/ValidationBuilderExtensions.cs`](../../Src/RCommon.FluentValidation/ValidationBuilderExtensions.cs) | `WithValidation<T>` (both overloads each) routes through `GetOrAddBuilder<T>`. |
| [`Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs`](../../Src/RCommon.Blobs/BlobStorageBuilderExtensions.cs) | `WithBlobStorage<T>` (both overloads) routes through `GetOrAddBuilder<T>`. |
| [`Src/RCommon.MultiTenancy/MultiTenancyBuilderExtensions.cs`](../../Src/RCommon.MultiTenancy/MultiTenancyBuilderExtensions.cs) | `WithMultiTenancy<TBuilder>` routes through `GetOrAddBuilder<TBuilder>`. |
| [`Src/RCommon.Stateless/StatelessBuilderExtensions.cs`](../../Src/RCommon.Stateless/StatelessBuilderExtensions.cs), [`Src/RCommon.MassTransit.StateMachines/`](../../Src/RCommon.MassTransit.StateMachines/) | `WithStatelessStateMachine` / `WithMassTransitStateMachine` underlying registrations made idempotent via `TryAdd*` where they currently use plain `Add*`. These verbs take no generic argument, so no sub-builder cache slot is needed. |
| [`Src/RCommon.Emailing/EmailingBuilderExtensions.cs`](../../Src/RCommon.Emailing/EmailingBuilderExtensions.cs), [`Src/RCommon.SendGrid/SendGridEmailingConfigurationExtensions.cs`](../../Src/RCommon.SendGrid/SendGridEmailingConfigurationExtensions.cs) | `WithSmtpEmailServices` / `WithSendGridEmailServices` underlying registrations made idempotent via `TryAdd*`. Singleton-style: only one `IEmailService` impl makes sense per app; mixing throws via a new `emailRegistration` slot on the builder. |
| [`Src/RCommon.Security/SecurityConfigurationExtensions.cs`](../../Src/RCommon.Security/SecurityConfigurationExtensions.cs), [`Src/RCommon.Web/WebConfigurationExtensions.cs`](../../Src/RCommon.Web/WebConfigurationExtensions.cs) | `WithClaimsAndPrincipalAccessor` / `WithClaimsAndPrincipalAccessorForWeb` registrations switch to `TryAdd*`. Singleton-style in spirit but no generic arg — the impl type is fixed per overload, so `TryAdd*` is sufficient. |

**Unchanged:** all sub-builder constructors (caching prevents re-entry), all public fluent surface signatures, the `RCommonBuilder` constructor's external behavior, all existing test fixtures for single-call usage.

### Conflict matrix (authoritative)

| Verb / scenario | Same impl | Different impl | Notes |
|---|---|---|---|
| `AddRCommon()` | Idempotent (returns cached builder) | n/a | Core registrations fire exactly once |
| `WithPersistence<T>` | Merge (cached sub-builder, action accumulates) | Both register | |
| `WithUnitOfWork<T>` | Merge | Both register | |
| `WithMediator<T>` | Merge | Both register | |
| `WithEventHandling<T>` | Merge | Both register | |
| `WithMemoryCaching<T>` / `WithDistributedCaching<T>` | Merge | Both register | |
| `WithJsonSerialization<T>` | Idempotent (action still applied to cached instance) | Throws (different concrete `T`) | Singleton-style. All six overloads share one cache slot |
| `WithCQRS<T>` | Merge | Both register | |
| `WithValidation<T>` | Merge | Both register | |
| `WithBlobStorage<T>` | Merge | Both register | |
| `WithMultiTenancy<TBuilder>` | Merge | Both register | |
| `WithSmtpEmailServices` / `WithSendGridEmailServices` | Idempotent (`TryAdd*`) | Throws (different email-impl slot) | Singleton-style |
| `WithStatelessStateMachine` / `WithMassTransitStateMachine` | Idempotent (`TryAdd*`) | n/a (single impl per verb) | |
| `WithClaimsAndPrincipalAccessor` / `WithClaimsAndPrincipalAccessorForWeb` | Idempotent (`TryAdd*`) | n/a (single impl per verb) | |
| `WithSequentialGuidGenerator` / `WithSimpleGuidGenerator` | Idempotent | Throws | Singleton-style |
| `WithDateTimeSystem` | Idempotent (options actions accumulate) | n/a (one impl type) | |
| `WithCommonFactory<TService, TImpl>` | Idempotent (`TryAdd`) | Last-wins (standard DI) + soft-dup warning | Not policed by RCommon |
| `AddProducer<T>` | Idempotent (same `(IEventProducer, T)` descriptor detected and skipped) | Coexist | |
| `IEFCorePersistenceBuilder.AddDbContext<TDbContext>(name, ...)` | Idempotent (same name + same `TDbContext`) | Throws `UnsupportedDataStoreException` (same name + different `TDbContext`) | Enforced in `DataStoreFactoryOptions.Register<,>` — exception type unchanged from today |

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

The existing [`GeneratePossibleDuplicatesServiceDescriptorsString`](../../Src/RCommon.Core/Extensions/ServiceCollectionExtensions.cs#L86-L123) scanner is wired to run automatically via the finalize `IHostedService` described in the Finalize flow above. `IRCommonBuilder.Configure()` does **not** trigger the scanner.

Emission:
- `ILoggerFactory` resolvable → single `LogLevel.Warning` containing the report.
- Not resolvable → message stashed; `builder.GetBootstrapDiagnostics()` returns it.

The scanner runs at most once per builder instance, guarded by the `_diagnosticsRun` flag.

## Migration & backward compatibility

**Source compatibility:** strictly additive. No existing signature changes, no member removals. Existing single-call code recompiles without diff.

**Behavioral compatibility:**
- Single `AddRCommon()` + single set of `With*` calls: bit-identical final `IServiceCollection` contents (the new `TryAdd` paths have nothing to deduplicate).
- Two `AddRCommon()` calls with no `With*` between: today, two `RCommonBuilder` instances are created but only the second's state is reachable to the caller, and the first's redundant core descriptors sit unused. After the change, both calls return the same builder with a single set of core descriptors. **Net registration outcome is functionally equivalent** for any code that wasn't broken.
- **Three deliberate throw-relaxations** (all are pure relaxations — code that wasn't throwing won't start throwing):
  - `_guidConfigured`: today throws `RCommonBuilderException("Guid Generator has already been configured once. You cannot configure multiple times")` on any second call regardless of impl type. After change: same impl type = idempotent, different impl type = throws.
  - `_dateTimeConfigured`: today throws `RCommonBuilderException("Date/Time System has already been configured once. You cannot configure multiple times")` on any second call. After change: idempotent (only one impl type exists).
  - `DataStoreFactoryOptions.Register<B, C>(name)`: today throws `UnsupportedDataStoreException` on any duplicate `(name, B)` regardless of `C`. After change: same `(name, B, C)` = idempotent, different `(name, B, C')` = throws with the **same exception type**. Caller `catch (UnsupportedDataStoreException)` blocks continue to work.
- No existing exception types change. `RCommonBuilderException` is the type used by all *new* throws (singleton-conflict cases), preserving existing exception contracts.

**Third-party `WithX` extensions:** continue to compile and run, but won't benefit from the cache until migrated to `GetOrAddBuilder<T>`. Migration is mechanical:

```csharp
// Before (constructor takes IServiceCollection — e.g., persistence sub-builders)
var sub = (TSub)Activator.CreateInstance(typeof(TSub), new object[] { builder.Services })!;
actions(sub);

// After
var sub = builder.GetOrAddBuilder<TSub>(() => (TSub)Activator.CreateInstance(typeof(TSub), new object[] { builder.Services })!);
actions(sub);

// Before (constructor takes IRCommonBuilder — e.g., event-handling and json sub-builders)
var sub = (TSub)Activator.CreateInstance(typeof(TSub), new object[] { builder })!;
actions(sub);

// After
var sub = builder.GetOrAddBuilder<TSub>(() => (TSub)Activator.CreateInstance(typeof(TSub), new object[] { builder })!);
actions(sub);
```

**Deprecated overloads:** the `[Obsolete]` `WithPersistence<TObjectAccess, TUnitOfWork>` overloads receive the same `GetOrAddBuilder` routing to prevent legacy users from regressing.

## Failure modes

| Failure | Surface |
|---|---|
| Conflicting singleton types across modules | `RCommonBuilderException` thrown synchronously from the second `With*` call |
| Same datastore name with different `TDbContext` | `UnsupportedDataStoreException` from `DataStoreFactoryOptions.Register<,>` (eager — fires at the `AddDbContext` call) |
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
- `WithSimpleGuidGenerator()` twice → no throw, single `IGuidGenerator` descriptor. (Today this throws unconditionally — test must replace the existing throw-assertion fixture.)
- `WithSimpleGuidGenerator()` then `WithSequentialGuidGenerator(...)` → throws `RCommonBuilderException`; message names both types.
- Symmetric: reverse order → throws with equivalent message.
- `WithDateTimeSystem(...)` twice → no throw. (Today this throws unconditionally — replaces existing assertion.)
- `WithJsonSerialization<JsonNetBuilder>()` then `WithJsonSerialization<TextJsonBuilder>()` → throws.
- `WithJsonSerialization<JsonNetBuilder>(serializeOpts1)` then `WithJsonSerialization<JsonNetBuilder>(serializeOpts2)` → no throw; both options actions applied to the cached builder.

**`SoftDuplicateDiagnosticsTests`**
- Manually injecting a duplicate `(IFoo, FooImpl)` after `AddRCommon` → at host startup, the finalize `IHostedService` runs and emits one warning through a test `ILoggerFactory`.
- Zero soft duplicates → no warning emitted.
- Warning emitted at most once across repeated host starts within a single test context.
- `Configure()` invocation does **not** trigger the scanner (preserves existing semantics).
- `GetBootstrapDiagnostics()` returns the message when no logger factory is registered.

**`MultiModuleEFCoreTests`**
- Two simulated modules each call `AddRCommon().WithPersistence<EFCorePerisistenceBuilder>` registering distinct DbContexts under distinct names. Built provider resolves `IDataStoreFactory` and obtains both DbContexts.
- Identical `AddDbContext<DbContextA>("DbA", ...)` from two modules → no throw, single registration. (Today this throws `UnsupportedDataStoreException` — test replaces the existing throw-assertion.)
- Same name + different `TDbContext` across two modules → throws `UnsupportedDataStoreException` (exception type unchanged).
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
