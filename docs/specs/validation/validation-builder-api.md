# Validation Builder API: Overload Disambiguation

**Branch:** bugfix/consumer-feedback-hardening
**Date:** 2026-07-15
**Status:** Approved
**Breaking Change:** No (one method is deprecated, not removed; one is deleted outright as a verified byte-for-byte duplicate — see Must Not Do)

## Overview

`RCommon.ApplicationServices` and `RCommon.FluentValidation` each declare a static class named `ValidationBuilderExtensions`, **in the same namespace** (`RCommon.ApplicationServices`), across two different assemblies:

- `Src/RCommon.ApplicationServices/ValidationBuilderExtensions.cs`: `WithValidation<T>(IRCommonBuilder)`, `WithValidation<T>(IRCommonBuilder, Action<T>)`, `UseWithCqrs(IValidationBuilder, Action<CqrsValidationOptions>)`.
- `Src/RCommon.FluentValidation/ValidationBuilderExtensions.cs`: `WithValidation<T>(IRCommonBuilder)` (byte-for-byte duplicate signature and body of the one above), `WithValidation<T>(IRCommonBuilder, Action<CqrsValidationOptions>)`.

This produces two distinct compiler problems for any consumer with both packages referenced and `using RCommon.ApplicationServices;` in scope (the normal case, since `RCommon.FluentValidation` depends on `RCommon.ApplicationServices` and both extension classes share its namespace):

1. The zero-arg `.WithValidation<T>()` call is **unconditionally** ambiguous (CS0121) — both classes declare the identical signature.
2. The single-arg call is ambiguous **only** when the lambda passed is convertible to both `Action<T>` and `Action<CqrsValidationOptions>` (e.g. a discard `_ => {}`, or one touching only members common to both) — existing docs avoid this by writing lambda bodies that only type-check against one candidate, which is why the ambiguity wasn't caught earlier.

## Personas

- **Library consumer wiring FluentValidation** — Calls `.WithValidation<FluentValidationBuilder>(...)` during `AddRCommon()` setup, expects to register validators and optionally configure whether the CQRS pipeline auto-validates commands/queries.
- **RCommon contributor adding a provider package** — Needs a clear rule for where a new package (e.g., a future `RCommon.DataAnnotations` validation provider) may and may not declare its own builder-verb overloads, so this collision doesn't recur.

## Core Requirements

### Must Have

- `RCommon.FluentValidation.ValidationBuilderExtensions.WithValidation<T>(this IRCommonBuilder builder)` (the zero-arg overload) is **deleted**. It is a verified byte-for-byte duplicate of `RCommon.ApplicationServices.ValidationBuilderExtensions.WithValidation<T>(this IRCommonBuilder builder)` — same signature, same body (`return WithValidation<T>(builder, x => {});` calling each package's own respective `Action<...>` overload with the same net effect: get-or-add the cached `T` builder via `GetOrAddBuilder<T>`). Deleting one of two identical methods changes nothing observable for any caller: whichever one the compiler bound to before, behavior is identical, and the CS0121 ambiguity this specific overload caused disappears entirely.
- `RCommon.FluentValidation.ValidationBuilderExtensions.WithValidation<T>(this IRCommonBuilder builder, Action<CqrsValidationOptions> actions)` is marked `[Obsolete]`, body unchanged, still fully functional — this overload is genuinely used today (documented at `website/docs/validation/fluent-validation.mdx:85-96` and `website/versioned_docs/version-3.1.1/...` equivalents) so it cannot be deleted outright without breaking those call sites.
- The `[Obsolete]` message points to the pattern that already exists and already fully covers this overload's purpose: call `UseWithCqrs(Action<CqrsValidationOptions>)` — already defined in `RCommon.ApplicationServices.ValidationBuilderExtensions` — *inside* the `Action<T>` lambda passed to `WithValidation<T>(Action<T>)`, since `T : IValidationBuilder` and `UseWithCqrs` extends `IValidationBuilder`:
  ```csharp
  builder.WithValidation<FluentValidationBuilder>(v =>
  {
      v.AddValidator<CreateOrderCommand, CreateOrderValidator>();
      v.UseWithCqrs(opts => opts.ValidateCommands = true);
  });
  ```
  This requires no new public API — `UseWithCqrs` already exists, already works today, and is already naturally discoverable via IntelliSense on `v` inside the lambda (since `v`'s static type is `T`, constrained to `IValidationBuilder`).
- Documentation (`website/docs/validation/fluent-validation.mdx` and its versioned copies) is updated to present the `UseWithCqrs`-inside-the-lambda pattern as the primary/recommended way to configure `CqrsValidationOptions`, and to note the deprecated overload only as a migration note for existing code.

### Must Not Do

- Must not change `RCommon.ApplicationServices.ValidationBuilderExtensions.WithValidation<T>(Action<T>)` or `UseWithCqrs` — both are correct as shipped; this spec only removes/deprecates the colliding additions from the FluentValidation package.
- Must not delete `RCommon.FluentValidation.ValidationBuilderExtensions.WithValidation<T>(Action<CqrsValidationOptions>)` outright in this release — it is documented and in active use; removal is a genuine breaking change reserved for a future major version, at which point the `[Obsolete]` marker becomes a removal candidate.
- Must not introduce a third method (e.g. a new `WithValidationOptions<T>`) to cover the same ground `UseWithCqrs` already covers — adding a third way to configure the same thing would be a worse outcome than the two-methods-doing-the-same-thing problem this spec is fixing.
- **Going forward, for any RCommon provider/add-on package:** must not declare an extension method with the same name as an existing core-package builder verb (`WithX<T>`) in a namespace the core package's own extensions already occupy, unless the new overload's parameter list is unambiguous against every existing overload for *any* possible lambda body (not just the specific bodies used in that package's own docs/examples). This is the rule this spec's incident violated; stating it explicitly here gives future contributors (including future AI-assisted sessions) a concrete guardrail rather than relying on each contributor to independently rediscover this failure mode.

## Technical Constraints

- No new NuGet dependencies. No signature changes to any existing non-colliding member.
- Targets the same .NET 8/9/10 frameworks as the rest of RCommon.

## Resilience

Not applicable — this is a compile-time API surface fix with no runtime behavior, I/O, or external dependency involved.

## Observability

Not applicable at runtime. The `[Obsolete]` attribute is the only "signal" — it surfaces as a build warning (CS0618) at the consumer's compile time, which is the correct layer for this kind of deprecation notice (not a runtime log).

## Security

Not applicable — pure API surface / build-time change, no data handling or trust boundary involved.

## Performance & Scalability

Not applicable — no runtime cost either way; this is purely a compile-time overload-resolution fix.

## Testing Strategy

1. Compile-time test (a small fixture project or an existing example under `Examples/Validation/`) proving `.WithValidation<FluentValidationBuilder>()` (zero-arg) compiles without CS0121 after the duplicate is removed.
2. Compile-time test proving `.WithValidation<FluentValidationBuilder>(_ => {})` (discard lambda, previously ambiguous) now compiles unambiguously, resolving to the `RCommon.ApplicationServices` overload.
3. Runtime test proving `UseWithCqrs` called inside a `WithValidation<T>(Action<T>)` lambda correctly configures `CqrsValidationOptions` (locks in the recommended replacement pattern).
4. Existing test/example coverage for the now-`[Obsolete]` `WithValidation<T>(Action<CqrsValidationOptions>)` overload continues to pass unchanged (proves the deprecation doesn't regress current behavior) — expect a new build warning (CS0618) in that test project, which is the intended, correct signal.

## File Summary

| File | Action | Location |
|------|--------|----------|
| `ValidationBuilderExtensions.cs` | Modify — delete zero-arg overload, mark `Action<CqrsValidationOptions>` overload `[Obsolete]` | `Src/RCommon.FluentValidation/` |
| `fluent-validation.mdx` | Modify — recommend `UseWithCqrs`-inside-lambda pattern; frame the obsolete overload as a migration note | `website/docs/validation/` |
| `README.md` | Modify — same correction | `Src/RCommon.FluentValidation/` |
| Test files (per Testing Strategy above) | Create/Modify | `Tests/RCommon.FluentValidation.Tests/` |
