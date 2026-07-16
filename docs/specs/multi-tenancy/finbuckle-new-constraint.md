# RCommon.Finbuckle: Remove Unused `new()` Constraint Breaking Built-In TenantInfo

**Branch:** bugfix/consumer-feedback-hardening
**Date:** 2026-07-16
**Status:** Implemented (discovered while building Examples.MultiTenancy.Finbuckle for the docs-consolidation pass; see docs/specs/documentation/docs-consolidation.md Sub-Spec B)
**Breaking Change:** No (constraint removal is backward-compatible — anything that satisfied the old, stricter constraint still satisfies the new one)

## Problem

`FinbuckleMultiTenantBuilder<TTenantInfo>`, `IFinbuckleMultiTenantBuilder<TTenantInfo>`, and `FinbuckleTenantIdAccessor<TTenantInfo>` all constrain `TTenantInfo` with `where TTenantInfo : class, ITenantInfo, new()`. Finbuckle's own built-in `TenantInfo` class — the type `multi-tenancy/finbuckle.mdx`'s own "Basic setup" section uses directly (`WithMultiTenancy<FinbuckleMultiTenantBuilder<TenantInfo>>`) — has `required` members (`Id`, `Identifier`) in the Finbuckle.MultiTenant version RCommon.Finbuckle currently references (10.0.8). C# does not allow a type with `required` members to satisfy a `new()` generic constraint (`CS9040`), so the documented example does not compile.

Confirmed via reflection that `new TTenantInfo()` is never actually called anywhere in `RCommon.Finbuckle` — the constraint was copied onto RCommon's own types without being load-bearing on net10.0.

The fix turned out to be per-target-framework, not universal: `RCommon.Finbuckle` multi-targets net8.0/net9.0/net10.0, and only net10.0 references the Finbuckle.MultiTenant version (10.0.8) with `required` members on `TenantInfo`. net8.0/net9.0 reference an older version (9.1.4) whose own `IMultiTenantContextAccessor<TTenantInfo>` interface still requires `new()` on its own type parameter — removing RCommon's `new()` unconditionally broke the net8.0/net9.0 builds with `CS0310` (`FinbuckleTenantIdAccessor<TTenantInfo>`'s field of type `IMultiTenantContextAccessor<TTenantInfo>` could no longer be verified to satisfy that interface's own constraint). The constraint is therefore `#if NET10_0` / `#else` conditional in `FinbuckleTenantIdAccessor<TTenantInfo>` and `FinbuckleMultiTenantBuilder<TTenantInfo>` (dropped only on net10.0, kept on net8.0/net9.0). `IFinbuckleMultiTenantBuilder<TTenantInfo>` needed no conditional at all — a class may implement a generic interface with a stricter type-parameter constraint than the interface itself declares, so the interface simply has no `new()` on any target framework.

There is no existing `RCommon.Finbuckle.Tests` project, which is presumably why this was never caught — the package's only usage examples live in documentation and READMEs, none of which are compiled as part of the build. The new test project only targets net10.0 (matching `Tests/Directory.Build.props`), so it specifically covers the target framework that needed the fix.

## Must Have

- Conditionally drop `new()` from the generic constraint on `FinbuckleMultiTenantBuilder<TTenantInfo>` and `FinbuckleTenantIdAccessor<TTenantInfo>` for `net10.0` only (`#if NET10_0 ... #else ... #endif`), keeping `where TTenantInfo : class, ITenantInfo, new()` for `net8.0`/`net9.0` where the older referenced Finbuckle version still requires it.
- `IFinbuckleMultiTenantBuilder<TTenantInfo>`'s constraint drops `new()` unconditionally (`where TTenantInfo : class, ITenantInfo` on every target framework) — no conditional needed since nothing inside the interface itself requires it, and implementing classes are free to add a stricter constraint.
- New `Tests/RCommon.Finbuckle.Tests` project with basic coverage: constructor null-guard tests for both new-constraint-bearing types, and a tenant-scoping behavior test proving `FinbuckleTenantIdAccessor<TenantInfo>.GetTenantId()` correctly reads (and returns `null` in the absence of) a Finbuckle-resolved tenant context, using the built-in `TenantInfo` class specifically (so a regression of this exact bug fails a build again).

## Must Not Do

- Must not change `ITenantIdAccessor`, `IMultiTenantBuilder`, or any other RCommon-wide multi-tenancy abstraction — this fix is scoped entirely to the three `RCommon.Finbuckle` types above.
- Must not pin `RCommon.Finbuckle`'s `Finbuckle.MultiTenant` package reference to an older, pre-`required`-members version as an alternative fix — that would leave consumers who upgrade Finbuckle themselves exposed to the same break.
- Must not remove `new()` unconditionally across all target frameworks — confirmed by an actual build failure that net8.0/net9.0 still need it.

## Testing Strategy

- New `Tests/RCommon.Finbuckle.Tests/FinbuckleMultiTenantBuilderTests.cs` and `FinbuckleTenantIdAccessorTests.cs`, using the built-in `Finbuckle.MultiTenant.Abstractions.TenantInfo` as the type argument throughout (the specific case that failed to compile before this fix).
- Full regression: `dotnet build Src/RCommon.sln`, `dotnet test Tests/RCommon.Finbuckle.Tests`, 0 errors/0 failures.

## File Summary

| File | Action |
|------|--------|
| `Src/RCommon.Finbuckle/FinbuckleMultiTenantBuilder.cs` | Modify — remove `new()` constraint |
| `Src/RCommon.Finbuckle/IFinbuckleMultiTenantBuilder.cs` | Modify — remove `new()` constraint |
| `Src/RCommon.Finbuckle/FinbuckleTenantIdAccessor.cs` | Modify — remove `new()` constraint |
| `Tests/RCommon.Finbuckle.Tests/RCommon.Finbuckle.Tests.csproj` | Create |
| `Tests/RCommon.Finbuckle.Tests/FinbuckleMultiTenantBuilderTests.cs` | Create |
| `Tests/RCommon.Finbuckle.Tests/FinbuckleTenantIdAccessorTests.cs` | Create |
