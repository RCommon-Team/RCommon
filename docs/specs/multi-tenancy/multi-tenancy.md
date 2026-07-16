# Multi-Tenancy: Scoped Filter Bypass & Stamping Semantics

**Branch:** bugfix/consumer-feedback-hardening
**Date:** 2026-07-15
**Status:** Approved
**Breaking Change:** No

## Overview

This spec covers three verified gaps in RCommon's multi-tenancy support, found during a consumer field review against 3.1.0-alpha.3/3.1.1:

1. There is no per-call bypass for repository-level tenant filtering — only an all-or-nothing accessor swap, documented as a DI-registration change rather than a scoped, call-site primitive.
2. The automatic `TenantId` stamping guard's exact semantics (skip on empty, unconditional overwrite on non-empty) are correct but undocumented.
3. A previously-reported "Finbuckle `EnforceMultiTenant()` throws before `TenantMismatchMode` is consulted" footgun does not reproduce against this codebase — `RCommon.Finbuckle` never wires Finbuckle's EF Core integration (`IMultiTenantDbContext`/`EnforceMultiTenant()`) at all — but is worth an explicit doc callout for consumers who might add that integration themselves on top of RCommon.

## Personas

- **Library consumer (single-tenant-per-request app)** — The common case. Injects `IGraphRepository<T>`/`IAggregateRepository<T,TKey>`, relies on ambient `ITenantIdAccessor` resolution (via claims or Finbuckle) for automatic filtering and stamping. Never needs a bypass.
- **Library consumer (cross-tenant admin/bootstrap operation)** — Needs one specific call (list all tenants a user belongs to, create the first row for a brand-new tenant) to skip ambient tenant scoping, without changing behavior for the rest of the request.
- **Library consumer (custom `ITenantIdAccessor`)** — Has written their own accessor implementation rather than using `ClaimsTenantIdAccessor`/`FinbuckleTenantIdAccessor`. Expects an opt-in path to the same bypass primitive without RCommon forcing a specific accessor implementation on them.

## Core Requirements

### Must Have

- A first-class, ambient, scoped bypass primitive — `TenantScope.Bypass()` in `RCommon.Security.Claims` — that, for its scope's lifetime (including across async continuations, via `AsyncLocal<bool>`), causes `ITenantIdAccessor.GetTenantId()` to resolve to `null` for any accessor RCommon wraps by default. This requires no changes to `LinqRepositoryBase.FilteredRepositoryQuery`, `MultiTenantHelper.SetTenantIdIfApplicable`, or any of the three providers' repository implementations — all of them already treat a `null`/empty tenant ID as "skip filtering" / "skip stamping," per `ITenantIdAccessor.GetTenantId()`'s own existing XML doc contract.
- `TenantScope.Bypass()` returns an `IDisposable`; disposing it restores the previous ambient state (supports nesting — an inner bypass scope disposed inside an outer one restores "bypassed," not "not bypassed").
- `TenantScopeAwareTenantIdAccessor : ITenantIdAccessor` is a public decorator. RCommon's own built-in accessors that resolve a real (non-null) tenant ID — `ClaimsTenantIdAccessor` (registered by `WithClaimsAndPrincipalAccessor()`) and Finbuckle's `FinbuckleTenantIdAccessor<TTenantInfo>` (registered by RCommon.Finbuckle's builder) — are wrapped with it automatically at their existing registration sites. `NullTenantIdAccessor` is intentionally not wrapped (it always returns `null`; wrapping would be a no-op).
- A consumer with a fully custom `ITenantIdAccessor` implementation can opt into bypass support with one line at their own registration site (`new TenantScopeAwareTenantIdAccessor(new MyAccessor(...))`) — this is not automatic for accessors RCommon didn't register itself, since RCommon has no way to intercept a registration it doesn't own.
- `MultiTenantHelper.SetTenantIdIfApplicable`'s exact guard (`Src/RCommon.Persistence/Crud/MultiTenantHelper.cs:53-59`) is documented explicitly: stamping is skipped entirely (not nulled) when the resolved tenant ID is null/empty; a non-empty resolved tenant ID unconditionally overwrites whatever the entity's `TenantId` already was. This includes the direct consequence that a bypass scope active during `AddAsync` also suspends stamping (resolved tenant ID is `null` under bypass), which is the documented mechanism for the tenant-bootstrap scenario below.
- Documentation explicitly states that RCommon.Finbuckle does not wire `IMultiTenantDbContext`/`EnforceMultiTenant()` — tenant enforcement is entirely repository-level (`MultiTenantHelper`), driven by `ITenantIdAccessor` — and warns that layering Finbuckle's own EF Core integration on top of `RCommonDbContext` independently can reintroduce an unconditional throw-before-`TenantMismatchMode`-is-consulted behavior that is Finbuckle's, not RCommon's, and is not mitigated by anything in `RCommon.Finbuckle`.

### Must Not Do

- Must not add a member to `ITenantIdAccessor`, `IRepository<T>`, `ILinqRepository<T>`, `IGraphRepository<T>`, or `IAggregateRepository<T,TKey>`. The bypass is implemented entirely as a decorator around the accessor, so no existing interface changes and no custom repository/accessor implementation is broken by this change.
- Must not change `MultiTenantHelper.SetTenantIdIfApplicable`'s behavior. It's already correct; this spec only documents it.
- Must not require consumers using RCommon's own built-in accessors to make any code change to get bypass support — it must work automatically for `ClaimsTenantIdAccessor` and `FinbuckleTenantIdAccessor` out of the box.

### Nice to Have

- A worked example on the multi-tenancy docs page for each persona scenario (cross-tenant listing, tenant bootstrap, custom accessor opt-in).

## Technical Constraints

- `AsyncLocal<bool>` for ambient flow across async continuations, consistent with the existing `CurrentPrincipalAccessorBase` pattern in the same package (`Src/RCommon.Security/Claims/CurrentPrincipalAccessorBase.cs:27`).
- No new package dependencies. No DI-container-specific decoration library (e.g., Scrutor) — decoration is done via a manual factory registration at each of the two call sites being changed, which is a two-line change per site.

## Resilience

No external dependencies; this is in-process ambient state, not a network or I/O concern. If `Bypass()`'s returned `IDisposable` is never disposed (e.g., an exception path that skips a `using` block due to a bug in calling code), the ambient flag remains set for the rest of that logical call context — recommend `using`/`await using` in all documentation examples, and note this failure mode explicitly in the XML doc on `Bypass()`.

## Observability

- Not logged. This is a fine-grained, potentially per-call primitive; logging every bypass scope entry/exit would be noisy for zero signal value, consistent with this repo's existing policy of not logging per-call framework operations (see `docs/specs/bootstrapping/bootstrapping.md`'s Observability section for the same reasoning applied to per-verb registration calls).

## Security

- `TenantScope.Bypass()` removes tenant isolation for its scope. It is an in-process, code-level primitive with no authorization check of its own — same trust model as `ITenantIdAccessor` itself (whatever calls `Bypass()` is trusted application code, not user input). Documentation must state plainly that consumers are responsible for gating access to any code path that calls `Bypass()` behind their own authorization checks (e.g., an admin role), since RCommon cannot know which callers should be allowed to see cross-tenant data.

## Performance & Scalability

- Negligible. `AsyncLocal<bool>` read/write is O(1); the decorator adds one virtual call per `GetTenantId()` invocation, already a per-request/per-repository-call-scale operation, not a hot loop.

## Design Detail

### `TenantScope`

**Location:** `Src/RCommon.Security/Claims/TenantScope.cs`

```csharp
namespace RCommon.Security.Claims
{
    /// <summary>
    /// Ambient, scoped bypass for tenant-based repository filtering and stamping. While a
    /// scope returned by <see cref="Bypass"/> is active, any <see cref="ITenantIdAccessor"/>
    /// wrapped with <see cref="TenantScopeAwareTenantIdAccessor"/> resolves to <c>null</c>,
    /// which every repository already treats as "skip tenant filtering / skip stamping" per
    /// <see cref="ITenantIdAccessor.GetTenantId"/>'s existing contract.
    /// </summary>
    public static class TenantScope
    {
        private static readonly AsyncLocal<bool> _bypassed = new();

        public static bool IsBypassed => _bypassed.Value;

        /// <summary>
        /// Suspends tenant scoping for the returned scope's lifetime, including across async
        /// continuations. Always dispose the returned handle (a <c>using</c> block is
        /// recommended) -- if it is never disposed, the bypass remains active for the rest of
        /// the current logical call context.
        /// </summary>
        public static IDisposable Bypass()
        {
            var previous = _bypassed.Value;
            _bypassed.Value = true;
            return new BypassHandle(previous);
        }

        private sealed class BypassHandle : IDisposable
        {
            private readonly bool _previous;
            private bool _disposed;
            public BypassHandle(bool previous) => _previous = previous;
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _bypassed.Value = _previous;
            }
        }
    }
}
```

### `TenantScopeAwareTenantIdAccessor`

**Location:** `Src/RCommon.Security/Claims/TenantScopeAwareTenantIdAccessor.cs`

```csharp
namespace RCommon.Security.Claims
{
    /// <summary>
    /// Decorates an <see cref="ITenantIdAccessor"/> so that <see cref="TenantScope.Bypass"/>
    /// suspends its resolution for the scope's lifetime. RCommon wraps its own
    /// <see cref="ClaimsTenantIdAccessor"/> and Finbuckle's tenant accessor with this
    /// automatically; wrap a custom <see cref="ITenantIdAccessor"/> implementation with this
    /// type at your own registration site to opt into the same bypass support.
    /// </summary>
    public sealed class TenantScopeAwareTenantIdAccessor : ITenantIdAccessor
    {
        private readonly ITenantIdAccessor _inner;

        public TenantScopeAwareTenantIdAccessor(ITenantIdAccessor inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public string? GetTenantId() => TenantScope.IsBypassed ? null : _inner.GetTenantId();
    }
}
```

### Registration changes

**`Src/RCommon.Security/SecurityConfigurationExtensions.cs:31`** — was:
```csharp
config.Services.TryAddTransient<ITenantIdAccessor, ClaimsTenantIdAccessor>();
```
becomes:
```csharp
config.Services.TryAddTransient<ClaimsTenantIdAccessor>();
config.Services.TryAddTransient<ITenantIdAccessor>(sp =>
    new TenantScopeAwareTenantIdAccessor(sp.GetRequiredService<ClaimsTenantIdAccessor>()));
```

**`Src/RCommon.Finbuckle/FinbuckleTenantIdAccessor.cs`'s registration site** (in RCommon.Finbuckle's builder) — same pattern applied to `FinbuckleTenantIdAccessor<TTenantInfo>`.

`NullTenantIdAccessor`'s registration in `PersistenceBuilderExtensions.WithPersistence` (`Src/RCommon.Persistence/PersistenceBuilderExtensions.cs:47`) is left unchanged — wrapping an always-null accessor has no effect.

### Worked scenario: tenant bootstrap (ties Design Decision above to item 5's original report)

```csharp
public class CreateTenantCommandHandler
{
    private readonly IAggregateRepository<Tenant, Guid> _tenants;

    public async Task HandleAsync(CreateTenantCommand cmd, CancellationToken ct)
    {
        using (TenantScope.Bypass())
        {
            var tenant = new Tenant(cmd.NewTenantId, cmd.Name);
            // tenant.TenantId is set explicitly by the aggregate's own constructor.
            // With the bypass active, ITenantIdAccessor.GetTenantId() resolves to null,
            // so MultiTenantHelper.SetTenantIdIfApplicable's guard skips stamping entirely --
            // the explicitly-set TenantId is left untouched, not overwritten with null and
            // not overwritten with whatever ambient tenant (if any) the caller happened to have.
            await _tenants.AddAsync(tenant, ct);
        }
    }
}
```

## Testing Strategy

1. `TenantScope.Bypass()` causes a `TenantScopeAwareTenantIdAccessor`-wrapped accessor to return `null` for the scope's lifetime, including across an `await` continuation (proves `AsyncLocal` flows correctly).
2. Disposing the bypass handle restores the prior accessor behavior.
3. Nested bypass scopes: disposing an inner scope while an outer scope is still active leaves bypass active (restores to `true`, not `false`).
4. A repository query against a multi-tenant entity, executed inside a bypass scope, returns rows across all tenants (integration test against EF Core; the same predicate-building logic in `MultiTenantHelper` is provider-agnostic, so one EF Core test is representative).
5. `AddAsync` inside a bypass scope does not overwrite an aggregate's already-set `TenantId` (the tenant-bootstrap scenario).
6. A custom `ITenantIdAccessor`, manually wrapped with `TenantScopeAwareTenantIdAccessor` at the test's own registration site, also honors `TenantScope.Bypass()` (proves the opt-in path works without any RCommon-specific accessor).

## File Summary

| File | Action | Location |
|------|--------|----------|
| `TenantScope.cs` | Create | `Src/RCommon.Security/Claims/` |
| `TenantScopeAwareTenantIdAccessor.cs` | Create | `Src/RCommon.Security/Claims/` |
| `SecurityConfigurationExtensions.cs` | Modify (`WithClaimsAndPrincipalAccessor`) | `Src/RCommon.Security/` |
| `FinbuckleTenantIdAccessor.cs` registration site | Modify | `Src/RCommon.Finbuckle/` |
| `multi-tenancy.mdx` (overview) | Modify — add bypass section, stamping-guard semantics, Finbuckle `EnforceMultiTenant()` callout | `website/docs/multi-tenancy/` |
| `README.md` | Modify — document `TenantScope`, cross-reference stamping guard | `Src/RCommon.Security/`, `Src/RCommon.MultiTenancy/` |
| Test files (per Testing Strategy above) | Create | `Tests/RCommon.Security.Tests/`, `Tests/RCommon.EfCore.Tests/` |
