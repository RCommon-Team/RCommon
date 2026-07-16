# Bootstrapping

## Overview

The bootstrapping domain governs how RCommon's `IServiceCollection` extension API (`AddRCommon()` and its fluent verbs) composes across multiple independent modules in a single in-process .NET application. The contract guarantees that any combination of modules can each invoke `services.AddRCommon()....WithX(...)` and produce a coherent, deduplicated registration set without breaking, throwing on benign repetition, or silently corrupting shared state.

## Personas

- **Library consumer (single-module app)** — Calls `services.AddRCommon()` once in `Program.cs`. Expects unchanged behavior from prior RCommon versions.
- **Library consumer (modular app)** — Composes multiple modules where each module's `AddServices(IServiceCollection)` method calls `services.AddRCommon()....WithX(...)`. Expects modules to be order-independent and to fail loudly on genuine misconfiguration, silently on benign repetition.
- **Third-party builder author** — Writes a `WithX<T>` extension method that registers a custom sub-builder. Expects a public, documented helper for participating in the caching contract.
- **RCommon contributor** — Maintains the `AddRCommon()` surface and provider packages. Expects clear conflict semantics per verb type so behavior changes don't silently regress.

## Core Requirements

### Must Have

- `services.AddRCommon()` invoked N times against the same `IServiceCollection` returns the same `IRCommonBuilder` instance (reference equality). Core registrations (`EventSubscriptionManager`, `IEventBus`, `IEventRouter`, `CachingOptions`) fire exactly once.
- A sub-builder verb (`WithPersistence<T>`, `WithMediator<T>`, `WithEventHandling<T>`, `WithMemoryCaching<T>`, `WithDistributedCaching<T>`, `WithJsonSerialization<T>`, `WithCQRS<T>`, `WithValidation<T>`, `WithBlobStorage<T>`, `WithMultiTenancy<T>`, `WithUnitOfWork<T>`) invoked twice with the same concrete `T` causes the sub-builder's constructor to run exactly once. The user's `Action<T>` configuration delegate runs once per call, against the shared cached instance, so configuration accumulates.
- Two sub-builder verbs of the same verb name but **different** concrete `T` register both sub-builders independently. Each has its own cache slot.
- Singleton-style verbs (`WithSimpleGuidGenerator`, `WithSequentialGuidGenerator`, `WithDateTimeSystem`) called twice with the **same** implementation type are idempotent. Called with a **different** implementation type they throw `RCommonBuilderException` naming both types and offering a remediation hint.
- `WithJsonSerialization<T>` follows singleton-style rules across all six overloads — one shared cache slot per `IRCommonBuilder`.
- `IEFCorePersistenceBuilder.AddDbContext<TDbContext>(name, options)` invoked twice with the same `(name, TDbContext)` triple is idempotent. Invoked with the same `name` but different `TDbContext` throws `UnsupportedDataStoreException` (existing exception type preserved).
- `AddProducer<T>` with the same concrete `T` is idempotent — at most one `IEventProducer` descriptor exists per concrete producer type. `AddProducer<T>` with distinct concrete types each register and coexist.
- Soft duplicates in the final `IServiceCollection` (any duplicate `ServiceDescriptor` not policed by the rules above) surface as a single `LogLevel.Warning` at host startup via the existing duplicate-scanner machinery. If no `ILoggerFactory` is available, the warning text is retrievable via `IRCommonBuilder.GetBootstrapDiagnostics()`.
- `services.IsRCommonInitialized()` is publicly available and returns `true` if and only if `AddRCommon()` has been invoked against that collection.
- `IRCommonBuilder.GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder> factory)` is publicly available and is the canonical way for `WithX<T>` extensions (including third-party) to participate in the caching contract.

### Must Not Do

- Must not change any existing public method or interface signature.
- Must not change any existing exception type thrown by an existing API.
- Must not introduce concurrency-safety guarantees. Bootstrap is single-threaded by contract; modules that initialize in parallel are responsible for synchronizing externally.
- Must not introduce a separate `IRCommonModule` abstraction or any new contract that modules are required to implement. Modules continue to call `services.AddRCommon()` directly.
- Must not require third-party sub-builders to migrate. Pre-existing `WithX` extensions using `Activator.CreateInstance` continue to compile and run; they just don't benefit from the cache until they opt in to `GetOrAddBuilder<T>`.
- Must not silently swallow registration conflicts that indicate genuine module disagreement. Hard conflicts (singleton-type mismatch, datastore-name conflict) throw at the offending call site.

### Nice to Have

- A migration documentation snippet in the JsonNet / EfCore / Mediatr README files showing the modular-composition pattern.

## Technical Constraints

- .NET 8, .NET 9, .NET 10 targets (inherited from RCommon's existing target frameworks).
- `Microsoft.Extensions.DependencyInjection` API surface (`IServiceCollection`, `ServiceDescriptor`, `TryAdd*` helpers). No third-party DI container.
- `Microsoft.Extensions.Hosting.IHostedService` for the finalize hook.
- `Microsoft.Extensions.Logging.ILoggerFactory` for diagnostic warning emission.
- No new package dependencies introduced.
- TDD per CLAUDE.md — tests precede implementation; red-green-refactor with commits per cycle. *[from: user CLAUDE.md]*

## Resilience

No external dependencies. Bootstrap is a one-time in-process configuration step against `IServiceCollection`. No network calls, no I/O, no remote services. **No resilience strategy required.**

The closest equivalent failure mode — a module raising an exception during its `Action<TSubBuilder>` callback — surfaces as an unhandled exception at startup, which is the correct behavior. RCommon does not catch, suppress, or retry user configuration errors.

## Observability

- **Logged:** Soft-duplicate registrations detected at host startup. Emitted once per `IRCommonBuilder` instance as a single `LogLevel.Warning` through `ILoggerFactory.CreateLogger<IRCommonBuilder>()`.
- **Not logged at this layer:** Per-verb registration calls (too noisy for a library bootstrap); successful idempotent reuses (zero signal value); the conflict-throw cases (already surface as exceptions with full type info in the message).
- **Metrics:** None. Bootstrap is one-shot at startup; there is no steady-state behavior to measure.
- **Alerting:** None at the library layer. Hosts can wire up their own log-based alerts on the warning if desired.
- **Retrieval API:** `IRCommonBuilder.GetBootstrapDiagnostics()` returns the stashed warning text for callers who want programmatic access (typically tests or hosts without an `ILoggerFactory`).

## Security

- **Attack surface:** None. This is .NET DI configuration plumbing invoked at process startup by trusted application code. No user input crosses the bootstrapping boundary.
- **Data protection:** None handled.
- **Auth/authz:** N/A.
- **Compliance:** N/A.

Security review at this layer is limited to ensuring the `RCommonBuilderException` and `UnsupportedDataStoreException` messages do not leak sensitive type names or configuration secrets — both are constrained to type-name reporting, which is non-sensitive.

## Performance & Scalability

- **Targets:** Bootstrap completes in O(N) where N is the number of `WithX` calls. Cache lookup is O(1) via `Dictionary<Type, object>`. No performance budget beyond "negligible relative to host startup time."
- **Testing:** None at the performance layer. Functional tests cover the bootstrap path; provider tests cover the resolved-service behavior. There is no load-test scenario applicable to startup configuration.
- **Scaling strategy:** Not applicable. Bootstrap runs once per process.
- **Growth projections:** Not applicable.

---

## Addendum (2026-07-15): Default Data Store Inference & Improved Failure Messaging

**Branch:** bugfix/consumer-feedback-hardening
**Status:** Approved
**Breaking Change:** No

### Problem

Without a call to `SetDefaultDataStore(options => options.DefaultDataStoreName = "...")`, `LinqRepositoryBase<TEntity>`'s constructor (`Src/RCommon.Persistence/Crud/LinqRepositoryBase.cs:47-65`) leaves `DataStoreName` at `null` if the caller also never sets it explicitly. DI registration and host startup succeed with no signal that anything is wrong. The failure only surfaces the first time a repository call actually resolves its data store, via `DataStoreFactory.Resolve<B>(name)` (`Src/RCommon.Persistence/DataStoreFactory.cs:49-59`) throwing `DataStoreNotFoundException` — a real, typed exception, but with an unhelpful message when `name` is empty (`"DataStore with name of  and base type of RCommonDbContext not found"`, literal double space, no mention of `SetDefaultDataStore` as the fix).

### Investigation: why a startup-time hard-fail is the wrong shape for this fix

The obvious-looking fix — fail fast at host startup whenever a data store is registered but no default is set — was rejected after checking it against RCommon's own documented usage. `website/docs/persistence/repository-pattern.mdx:48-61` shows, as a first-class documented pattern, a consumer with multiple registered data stores who sets `DataStoreName` explicitly on every repository instance and *never* calls `SetDefaultDataStore` at all:

```csharp
public CreateLeaveTypeCommandHandler(IGraphRepository<LeaveType> repository)
{
    _repository = repository;
    _repository.DataStoreName = "LeaveManagement"; // matches the name used in AddDbContext
}
```

For this consumer, "no default configured" is correct and intentional, not a misconfiguration — RCommon cannot distinguish this from the actual bug scenario (a consumer who forgot to set a default *and* forgot to set `DataStoreName` per call site) until a repository is actually resolved and used without an explicit name. That is exactly the point at which the current deferred-exception behavior already fires. A hard fail at startup for "N stores, no default" would be a false positive against this legitimate pattern. The fix is narrower than "fail fast for every ambiguous case" — it's two independent, additive pieces that don't touch the genuinely-ambiguous case at all.

### Design Decision 1: auto-infer the default when exactly one data store is registered

This is the one case with zero ambiguity: if there's only one registered data store and no explicit default, "use it" can never be wrong — and it directly resolves the original bug report's scenario (single-database app, `SetDefaultDataStore` never called). Implemented as a standard `IPostConfigureOptions<DefaultDataStoreOptions>`, not a hosted-service side-channel, so it runs lazily via the normal options pipeline the first time `IOptions<DefaultDataStoreOptions>.Value` is resolved (by which point every `WithPersistence`/`AddDbContext` call across every module has already run, since those all happen synchronously during host configuration):

```csharp
internal sealed class DefaultDataStoreOptionsPostConfigure : IPostConfigureOptions<DefaultDataStoreOptions>
{
    private readonly IOptions<DataStoreFactoryOptions> _dataStoreFactoryOptions;
    private readonly ILogger<DefaultDataStoreOptions>? _logger;

    public DefaultDataStoreOptionsPostConfigure(
        IOptions<DataStoreFactoryOptions> dataStoreFactoryOptions,
        ILogger<DefaultDataStoreOptions>? logger = null)
    {
        _dataStoreFactoryOptions = dataStoreFactoryOptions;
        _logger = logger;
    }

    public void PostConfigure(string? name, DefaultDataStoreOptions options)
    {
        if (!string.IsNullOrEmpty(options.DefaultDataStoreName))
        {
            return; // consumer explicitly set a default -- always respected, never overridden
        }

        var registered = _dataStoreFactoryOptions.Value.Values
            .Select(v => v.Name).Distinct().ToList();

        if (registered.Count == 1)
        {
            options.DefaultDataStoreName = registered[0];
            _logger?.LogInformation(
                "RCommon inferred '{DataStoreName}' as the default data store because it is the only one " +
                "registered. Call SetDefaultDataStore(...) explicitly to set this yourself and silence this message.",
                registered[0]);
        }
        else if (registered.Count > 1)
        {
            _logger?.LogInformation(
                "RCommon found {Count} registered data stores ({Names}) and no default was set via " +
                "SetDefaultDataStore(...). This is expected if every repository sets DataStoreName explicitly; " +
                "otherwise, repository calls that don't specify DataStoreName will throw DataStoreNotFoundException. " +
                "Call SetDefaultDataStore(...) to resolve.",
                registered.Count, string.Join(", ", registered));
        }
    }
}
```

Registered once via `TryAddEnumerable` in `PersistenceBuilderExtensions.WithPersistence<TObjectAccess>` (`Src/RCommon.Persistence/PersistenceBuilderExtensions.cs:43-54`) — the single entry point every provider's fluent registration (`EFCorePersistenceBuilder`, `DapperPersistenceBuilder`, `Linq2DbPersistenceBuilder`) already routes through, so this covers all three providers with one registration site, and is idempotent across repeated `WithPersistence` calls from multiple modules.

### Design Decision 2: multi-store, no-default case gets an informational log, not an error

Per the investigation above, this case is only *possibly* a misconfiguration. `LogInformation` (not `LogWarning`) documents the ambiguity for anyone reading startup logs without asserting that something is wrong — consistent with this doc's existing Observability section, which already reserves `LogWarning` for genuine soft-duplicate conflicts. No exception is thrown here; the existing deferred-to-first-use `DataStoreNotFoundException` remains the actual enforcement point for the real misconfiguration, improved next.

### Design Decision 3: improve `DataStoreNotFoundException`'s message

**Location:** `Src/RCommon.Persistence/DataStoreFactory.cs:49-59`. Current message renders as `"DataStore with name of  and base type of RCommonDbContext not found"` when `name` is null/empty — the actual complaint from the field report. New message distinguishes the two failure shapes and lists what *is* registered, since `DataStoreFactory` already holds `_values` in scope:

```csharp
public B Resolve<B>(string name) where B : IDataStore
{
    if (_values.Any(x => x.Name == name && x.BaseType == typeof(B)))
    {
        return (B)_provider.GetRequiredService(_values.First(x => x.Name == name && x.BaseType == typeof(B)).ConcreteType);
    }

    var registered = _values.Select(v => v.Name).Distinct().ToList();
    var registeredList = registered.Count == 0 ? "(none)" : string.Join(", ", registered);

    if (string.IsNullOrEmpty(name))
    {
        throw new DataStoreNotFoundException(
            $"No DataStoreName was specified for this repository, and no default data store has been " +
            $"configured. Either set the repository's DataStoreName property explicitly, or call " +
            $"SetDefaultDataStore(...) during WithPersistence<T> setup. Registered data stores: {registeredList}.");
    }

    throw new DataStoreNotFoundException(
        $"DataStore with name of '{name}' and base type of '{typeof(B).GetGenericTypeName()}' not found. " +
        $"Registered data stores: {registeredList}.");
}
```

Existing exception *type* (`DataStoreNotFoundException`) is unchanged — only the message text changes, so this doesn't violate the "must not change any existing exception type" rule above; it's a message-quality fix, not a contract change.

### Extends Core Requirements — Must Have

- When exactly one data store is registered across all `WithPersistence<T>`/`AddDbContext<T>` calls and no explicit `SetDefaultDataStore` was called, `DefaultDataStoreOptions.DefaultDataStoreName` is automatically set to that one registration's name, logged at `LogInformation`.
- When two or more data stores are registered and no explicit default is set, no default is inferred (ambiguous), and a single `LogInformation` note is emitted describing the registered names and the two ways to resolve ambiguity (explicit `DataStoreName` per repository, or `SetDefaultDataStore`).
- `DataStoreNotFoundException`'s message always lists currently-registered data store names and, when the failure is due to a missing/empty name specifically, names both remediation paths explicitly.

### Testing Strategy

1. Single data store registered, no `SetDefaultDataStore` call → repository resolves successfully using the inferred name; an `LogInformation` entry is emitted.
2. Two data stores registered, no `SetDefaultDataStore` call, every repository sets `DataStoreName` explicitly → no exception anywhere (regression guard against the false-positive risk this design avoids).
3. Two data stores registered, no `SetDefaultDataStore` call, a repository is used *without* setting `DataStoreName` → `DataStoreNotFoundException` with the new empty-name message, listing both registered names.
4. Explicit `SetDefaultDataStore` call is never overridden by the auto-infer logic, regardless of how many data stores are registered.
5. `DataStoreNotFoundException` message content assertions for both the empty-name and wrong-name cases.

### File Summary

| File | Action | Location |
|------|--------|----------|
| `DefaultDataStoreOptionsPostConfigure.cs` | Create | `Src/RCommon.Persistence/` |
| `PersistenceBuilderExtensions.cs` | Modify (`WithPersistence<TObjectAccess>` — register post-configure via `TryAddEnumerable`) | `Src/RCommon.Persistence/` |
| `DataStoreFactory.cs` | Modify (`Resolve<B>` message) | `Src/RCommon.Persistence/` |
| Test files (per Testing Strategy above) | Create | `Tests/RCommon.Persistence.Tests/` |

## API Design

New public surface, strictly additive:

```csharp
namespace RCommon;

public interface IRCommonBuilder
{
    // existing members preserved unchanged
    TSubBuilder GetOrAddBuilder<TSubBuilder>(Func<TSubBuilder> factory)
        where TSubBuilder : class;
    string GetBootstrapDiagnostics();
}

public static class ServiceCollectionExtensions
{
    // existing members preserved unchanged
    public static bool IsRCommonInitialized(this IServiceCollection services);
}
```

No removals, no signature changes, no exception-type changes on existing throws.

## Testing Strategy

- New test files added under `Tests/RCommon.Tests/Bootstrapping/`, `Tests/RCommon.EfCore.Tests/Bootstrapping/`, `Tests/RCommon.Mediatr.Tests/Bootstrapping/`.
- Coverage anchors: idempotent `AddRCommon`, sub-builder cache invariants, singleton-verb conflict detection, soft-duplicate warning emission, multi-module EF Core integration (including datastore-name collisions), multi-module MediatR integration.
- TDD ordering: core idempotency → cache helper → singleton tracker → datastore-name collision → soft-duplicate finalize → per-provider migrations.
- Existing tests for `_guidConfigured` / `_dateTimeConfigured` / `DataStoreFactoryOptions.Register` need replacement to reflect the relaxed-on-same-type semantics.

## Migration / Rollout

- Strictly source-compatible. No release-note "action required" item for consumers using the single-call pattern.
- Modular consumers benefit automatically with no code change.
- Third-party `WithX` extension authors can opt into the cache by switching `Activator.CreateInstance` invocations to `IRCommonBuilder.GetOrAddBuilder<T>` (mechanical edit; documented).
- The existing `[Obsolete]` overloads (`WithPersistence<T,U>`) receive the same `GetOrAddBuilder` routing so legacy users don't regress.

## Open Questions

None at spec-level. All implementation-level open items (exact hosted-service class name, exact filename enumeration for `Src/RCommon.Wolverine/`) are tracked in the source design doc and will be resolved during planning.

## Feature Breakdown

This domain is covered by a single specification (no further feature decomposition needed). Detailed implementation specifics — call-site-by-call-site changes, internal data structures, finalize hosted service mechanics — are documented in the brainstorming design at [`docs/superpowers/specs/2026-05-15-modular-bootstrapper-design.md`](../../superpowers/specs/2026-05-15-modular-bootstrapper-design.md), which serves as input to the implementation plan.
