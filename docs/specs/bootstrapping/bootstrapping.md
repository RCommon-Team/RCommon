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
