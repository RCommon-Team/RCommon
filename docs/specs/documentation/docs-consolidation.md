# Documentation & Examples Consolidation Pass

**Branch:** bugfix/consumer-feedback-hardening
**Date:** 2026-07-16
**Status:** Draft — pending review
**Breaking Change:** No

## Overview

This spec has grown beyond "reconcile the docs that Specs 1–6 already touched." It now covers five distinct problems, found via direct investigation of the current website, `Examples/` tree, and build tooling:

1. **Item 11** from the original consumer feedback review — `ICurrentUser`'s full member surface isn't documented in one place (the one feedback item with no corresponding code change).
2. **Two orphaned doc pages** — `event-handling/outbox-producer-processor-topology` and `persistence/aggregate-repository` exist on disk, render at a live URL, but aren't listed in `sidebars.ts` and so are unreachable from any nav/sidebar/footer link.
3. **Missing runnable examples** — Dapper, Linq2Db, Sagas, Blob Storage (Azure + S3), State Machines, Multi-Tenancy, and the Outbox pattern each have website documentation but zero corresponding project under `Examples/`.
4. **No end-to-end DDD recipe** — the five `domain-driven-design/*.mdx` pages each invent their own disposable example domain and never connect; no runnable project demonstrates `AggregateRoot`, domain events, value objects, and soft delete together.
5. **No machine-readable full API reference** — nothing in the repo exports XML doc comments or reflects over built assemblies; the existing `llms-full.txt` is a prose-only dump of the `.mdx` doc pages, not an API surface listing. Consuming the full public API today requires reading source or decompiling.

Given the scope, this is organized into four sub-specs (A–D) that can be implemented and committed independently, mirroring how Specs 1–6 were sequenced. This document is the umbrella; each sub-spec below is self-contained enough to implement on its own.

## Personas

(Carried over from the original docs-consolidation draft, plus:)

- **Claude Code / an LLM coding agent working against RCommon** — today has no way to enumerate the full public API surface short of reading `Src/` directly or decompiling built NuGet packages. Needs a single, comprehensive, machine-readable artifact listing every public type/member/signature/doc-comment.
- **A new consumer picking a persistence provider** — currently sees full docs pages for Dapper, Linq2Db, and Sagas, but no runnable example for any of them, and would have to reverse-engineer usage from the docs' inline snippets alone.
- **A consumer trying to model a real domain** — currently has five disconnected DDD concept pages, each with a different throwaway example, and one deep recipe (HR Leave Management) that avoids every DDD building block. There's no single place showing how aggregates, domain events, value objects, and soft delete fit together in one running project.

---

## Sub-Spec A: Navigation, Naming, and DI-Coverage Fixes

The cheapest, lowest-risk part of this pass — no new projects, just corrections to what already exists.

### Must Have

- Add `'event-handling/outbox-producer-processor-topology'` to the `Event Handling` category's `items` array in `website/sidebars.ts`.
- Add `'persistence/aggregate-repository'` to the `Persistence` category's `items` array in `website/sidebars.ts`.
- New `ICurrentUser` member-reference subsection on `website/docs/security-web/authorization.mdx`, listing all seven members (`Id`, `IsAuthenticated`, `Roles`, `TenantId`, `FindClaim(string)`, `FindClaims(string)`, `GetAllClaims()`) individually — signature plus one-sentence semantics each — rather than the current single compressed table row. Includes an explicit callout distinguishing `ICurrentUser.TenantId` (the current caller's tenant, from a claim) from `IMultiTenant.TenantId` (a persistence-layer property stamped onto an entity), cross-linking to `multi-tenancy/overview.mdx`.
- `Src/RCommon.Security/README.md` cross-references the expanded website section rather than duplicating the seven-member breakdown in prose.
- Site-wide naming consistency: every code sample across the docs site and READMEs that currently writes `EFCorePerisistenceBuilder` (the obsolete, misspelled type kept only as a compatibility shim per Spec 1) is updated to the corrected `EFCorePersistenceBuilder`. Confirmed affected: `efcore.mdx`, `hr-leave-management.mdx`, `finbuckle.mdx`, `multi-tenancy/overview.mdx`, `sagas.mdx`, `nuget-packages.mdx`, and the `examples-recipes/*.mdx` pages. A one-line note is added wherever the type is first introduced (`efcore.mdx`'s API Summary) documenting that the old spelling still works but is obsolete.
- `website/docs/api-reference/nuget-packages.mdx`'s dependency map is corrected to show `RCommon.ApplicationServices` also depending on `RCommon.Persistence` (added by Spec 5).
- New dedicated doc coverage for `IReadModelRepository<TEntity>` / `EFCoreReadModelRepository<>` — currently registered by every `EFCorePersistenceBuilder` but has zero mentions anywhere in `website/docs/`. Add a section to `persistence/efcore.mdx` (or a new `persistence/read-model-repository.mdx` if it warrants its own page once written).
- `event-handling/in-memory.mdx` (and the shared `AddProducer`/`AddSubscriber` surface it documents) gets the three missing overloads documented: `AddSubscriber<TEvent,TEventHandler>(Func<IServiceProvider,TEventHandler>)`, `AddProducer<T>(Func<IServiceProvider,T>)`, and `AddProducer<T>(T producer)` — the last of which has a behaviorally significant side effect (auto-registers the producer as `IHostedService` if it implements that interface) that is currently undiscoverable from docs.
- `cqrs-mediator/command-query-bus.mdx`'s API Summary table gains the missing `AddCommandHandlers(params Type[])` / `AddQueryHandlers(params Type[])` overload rows.
- `persistence/efcore.mdx` gains cross-links to `persistence/aggregate-repository.mdx` and `persistence/sagas.mdx` noting that `IAggregateRepository<,>` and `ISagaStore<,>` are registered automatically alongside the repositories `efcore.mdx` already documents.

### Must Not Do

- Must not touch `website/versioned_docs/version-3.1.1/...` for any fix in this sub-spec — all of these are new-content/consistency fixes, not corrections of factually wrong 3.1.1 behavior.
- Must not introduce a new `api-reference/`-style sub-page per interface for item 11; follow the existing inline-on-feature-page convention (see the original docs-consolidation draft's reasoning).

### Testing Strategy

No automated tests (documentation-only). Verify via `pnpm build` (Docusaurus's `onBrokenLinks: 'throw'` catches dead cross-links) and a manual diff of `sidebars.ts` against `website/docs/**/*.mdx` file listing to confirm zero orphans remain.

### File Summary

| File | Action |
|------|--------|
| `website/sidebars.ts` | Modify — add 2 missing doc IDs |
| `website/docs/security-web/authorization.mdx` | Modify — expand `ICurrentUser` reference, add `TenantId` disambiguation |
| `Src/RCommon.Security/README.md` | Modify — cross-reference instead of duplicate |
| `website/docs/persistence/efcore.mdx` | Modify — naming fix, cross-links, `IReadModelRepository` section |
| `website/docs/persistence/hr-leave-management.mdx`, `finbuckle.mdx`, `multi-tenancy/overview.mdx`, `sagas.mdx`, `nuget-packages.mdx`, `examples-recipes/*.mdx` | Modify — naming fix |
| `website/docs/api-reference/nuget-packages.mdx` | Modify — dependency map fix |
| `website/docs/event-handling/in-memory.mdx` | Modify — document 3 missing overloads |
| `website/docs/cqrs-mediator/command-query-bus.mdx` | Modify — add missing overload rows |

---

## Sub-Spec B: New Runnable Examples

One new `Examples/` project per functional area currently at zero coverage, added to `Examples/Examples.sln`.

### Must Have

- `Examples/Persistence/Examples.Persistence.Dapper/` — CRUD against `IGraphRepository<T>`/`ILinqRepository<T>` backed by `RCommon.Dapper`, against a real (or LocalDB/SQLite-for-example-purposes) connection.
- `Examples/Persistence/Examples.Persistence.Linq2Db/` — same shape, backed by `RCommon.Linq2Db`.
- `Examples/Persistence/Examples.Persistence.Sagas/` — a runnable version of the "order fulfillment saga" walkthrough already written in prose on `persistence/sagas.mdx`, using `SagaOrchestrator`/`ISagaStore<,>`/`SagaState<TKey>`.
- `Examples/BlobStorage/Examples.BlobStorage.Azure/` and `Examples/BlobStorage/Examples.BlobStorage.S3/` — minimal upload/download/delete demonstrations against `RCommon.Azure.Blobs` and `RCommon.Amazon.S3Objects` respectively (using Azurite/local S3-compatible emulator or clearly-marked placeholder connection strings, consistent with how `Examples.Caching.RedisCaching` already handles "requires external infra").
- `Examples/StateMachines/Examples.StateMachines.Stateless/` — demonstrates `IStateMachine<TState,TTrigger>` via the `RCommon.Stateless` adapter.
- `Examples/MultiTenancy/Examples.MultiTenancy.Finbuckle/` — demonstrates `WithMultiTenancy<FinbuckleMultiTenantBuilder<TTenantInfo>>`, an `IMultiTenant` entity, tenant-scoped reads/writes, and `TenantScope.Bypass()` for the tenant-bootstrap scenario (Spec 3's new primitive) — the first runnable example of multi-tenancy in the repo.
- `Examples/EventHandling/Examples.EventHandling.Outbox/` — demonstrates the transactional outbox pattern end-to-end (`EFCoreOutboxStore`/`EFCoreInboxStore`, outbox processing), tying to the previously-orphaned `outbox-producer-processor-topology` page.
- Each new project gets its corresponding docs page updated with a link to the runnable example (mirroring how `hr-leave-management.mdx` links to `CleanWithCQRS/`).
- Clean up the two stale, sourceless directories `Examples/Messaging/Samples.Messaging.MassTransitRabbitMq/` and `Examples/Messaging/Samples.Messaging.WolverineRabbitMq/` (build-artifact-only leftovers with no `.csproj`, not referenced by the solution) so they don't get mistaken for real examples.

### Must Not Do

- Must not require a live external dependency (real Azure/AWS/Redis credentials, a real message broker) to build. Every new example must build and run its happy path against an emulator, in-memory/local substitute, or clearly-commented placeholder — consistent with existing examples (`InMemoryEventBusBuilder`, EFCore `UseInMemoryDatabase`, etc.).
- Must not turn any new example into a second "deep" reference app like the HR sample. Match the existing "thin, single-feature, single-call" style already established by the majority of `Examples/` projects, unless the DDD recipe (Sub-Spec C) explicitly calls for more.

### Testing Strategy

Each new project must build as part of `Examples/Examples.sln` (`dotnet build Examples/Examples.sln`) with 0 errors — the same regression gate used for every prior spec on this branch. No unit tests required (consistent with the existing thin examples), except the Sagas and Outbox examples, which should include a minimal test proving the happy path completes (these two involve enough moving parts that a silent runtime failure would be easy to miss).

### File Summary

| Project | Location |
|---|---|
| `Examples.Persistence.Dapper` | `Examples/Persistence/` |
| `Examples.Persistence.Linq2Db` | `Examples/Persistence/` |
| `Examples.Persistence.Sagas` | `Examples/Persistence/` |
| `Examples.BlobStorage.Azure` | `Examples/BlobStorage/` |
| `Examples.BlobStorage.S3` | `Examples/BlobStorage/` |
| `Examples.StateMachines.Stateless` | `Examples/StateMachines/` |
| `Examples.MultiTenancy.Finbuckle` | `Examples/MultiTenancy/` |
| `Examples.EventHandling.Outbox` | `Examples/EventHandling/` |

---

## Sub-Spec C: End-to-End DDD Recipe

### Must Have

- One new runnable project, `Examples/DomainDrivenDesign/Examples.DomainDrivenDesign/`, built around a single coherent aggregate (candidate: a `Team`/`TeamMembership` aggregate, reusing the shape already established in Spec 1's test fixtures and docs examples, so terminology is consistent across the repo) that in one place:
  - Derives from `AggregateRoot<TKey>`.
  - Raises a domain event (`AddDomainEvent`) on a state-changing method, handled via `ISubscriber<TEvent>`.
  - Includes at least one `ValueObject`/`ValueObject<T>` member (e.g. an `EmailAddress` or `Address` value object on a member entity).
  - Implements `ISoftDelete`.
  - Implements `IMultiTenant`.
  - Is persisted via `IAggregateRepository<TAggregate,TKey>` (not `IGraphRepository<T>`), demonstrating both supported add/update-child patterns already written up in `persistence/aggregate-repository.mdx`.
- A new `examples-recipes/domain-driven-design.mdx` page walking through the project end-to-end, with each section linking back to the relevant conceptual page (`entities-aggregates.mdx`, `domain-events.mdx`, `value-objects.mdx`, `soft-delete.mdx`) at the point where that concept is introduced in the walkthrough.
- Each of the five existing `domain-driven-design/*.mdx` conceptual pages gets a "See it in a full example" pointer to the new recipe, replacing (or supplementing) their standalone throwaway snippets where practical — full replacement is not required if the conceptual page's isolated snippet is still pedagogically clearer for that specific concept in isolation.

### Must Not Do

- Must not require touching `Src/` production code — this is purely a new example project plus docs; no aggregate/value-object/event base classes need new capabilities, they're all already fully supported.
- Must not fold multi-tenancy into this same aggregate in a way that conflicts with Sub-Spec B's separate `Examples.MultiTenancy.Finbuckle` project's scope — this recipe's use of `IMultiTenant` should stay minimal (stamping/filtering only) and defer `TenantScope.Bypass()` and Finbuckle-specific wiring to the dedicated multi-tenancy example, cross-linking to it rather than duplicating.

### Testing Strategy

The new project should include a small test suite (unit tests over the aggregate's behavior — domain event raised on the right method call, value object equality, soft-delete flag behavior) consistent with the test-coverage bar the rest of this branch has held to. Build gate: `dotnet build Examples/Examples.sln`, 0 errors.

### File Summary

| File | Action |
|------|--------|
| `Examples/DomainDrivenDesign/Examples.DomainDrivenDesign/` | Create (new project + tests) |
| `website/docs/examples-recipes/domain-driven-design.mdx` | Create |
| `website/sidebars.ts` | Modify — add the new recipe page to `Examples & Recipes` |
| `website/docs/domain-driven-design/*.mdx` (5 files) | Modify — add "See it in a full example" cross-links |

---

## Sub-Spec D: Machine-Readable Full API Reference

### Problem

Nothing in the repo exports XML doc comments (`GenerateDocumentationFile` isn't set anywhere) or reflects over built assemblies. The existing `website/scripts/generate-llms-full.ts` only concatenates the prose `.mdx` doc pages — it has no code path touching `.csproj`, compiled assemblies, or XML doc files, so any type/member signature in `llms-full.txt` today exists only because it was hand-transcribed into a doc page's code block. An agent (or human) needing the exhaustive public API surface currently has to read `Src/` directly or decompile the built NuGet packages.

### Design Decision: reflection dump, not a browsable DocFX site

A full DocFX (or Sandcastle) static-site generator was considered and rejected for this pass: it requires new build-pipeline infrastructure, hosting/styling integration with the existing Docusaurus site, and serves a browsable-human-reference goal that isn't what's being asked for here. The stated goal is narrower and cheaper to satisfy: a single, comprehensive, machine-readable artifact that an LLM coding agent can read in one shot instead of decompiling assemblies — the same shape `llms-full.txt` already takes for prose docs, applied to the API surface instead.

### Must Have

- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` added to `Src/Directory.Build.props`, so every `RCommon.*` package emits its `.xml` doc sidecar on build. (Side effect: the compiler will now surface `CS1591` warnings for any public member missing a doc comment. Given this codebase's existing convention of thorough XML doc comments, expect this to be low-noise, but the build should be checked after enabling it; suppress via `<NoWarn>1591</NoWarn>` only if the volume is unreasonable, and prefer fixing genuinely undocumented members over suppressing.)
- New tool, `Tools/RCommon.ApiReferenceGenerator/` (a small .NET console project, not shipped as a package), that:
  - Loads each `Src/RCommon.*/bin/<config>/<tfm>/RCommon.*.dll` build output and its sibling `.xml` doc file via reflection (`System.Reflection` + a minimal XML-doc-comment parser keyed by member ID — the same ID format the C# compiler emits, e.g. `M:Namespace.Type.Member(ParamType)`).
  - Walks every public type and public member (methods, properties, constructors, events) reachable from each assembly's public surface.
  - Emits one Markdown section per package: namespace → type (with its doc-comment summary) → each public member (signature + doc-comment summary/params/returns/exceptions).
  - Concatenates all packages into a single output file, mirroring `llms-full.txt`'s header/footer/section-separator conventions.
- Output written to `website/static/api-reference-full.txt` (naming parallel to `llms-full.txt`).
- Wired into the existing build pipeline: `website/package.json`'s `"build"` script gains a `generate:api` step (`pnpm generate:api && pnpm generate:llms && docusaurus build`), and `generate:api` invokes the new tool (`dotnet run --project ../Tools/RCommon.ApiReferenceGenerator -- <path-to-Src-bin-outputs> <output-path>`), after a prerequisite `dotnet build Src/RCommon.sln -c Release` step (documented in the tool's own README, and in `website/README.md` if one exists, as a prerequisite for a full local build).
- `website/docs/api-reference/nuget-packages.mdx` (or a new small page) gets one line pointing at `/api-reference-full.txt` as the canonical machine-readable full-surface reference, parallel to the existing `/llms-full.txt` pointer.

### Must Not Do

- Must not attempt to reflect over third-party dependency assemblies (EF Core, MediatR, MassTransit, etc.) — scope is RCommon's own public surface only.
- Must not make the website `pnpm build` fail if the tool can't find built assemblies (e.g. a docs-only contributor who never ran `dotnet build`) — degrade gracefully with a clear console warning and skip regenerating `api-reference-full.txt` (leaving the last-committed version in place) rather than hard-failing the whole site build.
- Must not duplicate what XML doc comments already say by hand-writing prose — this is a mechanical reflection+transcription tool, not a second copy of the docs site.

### Testing Strategy

No unit tests for the generator tool itself in this pass (it's a build-time script, not shipped library code) — verification is functional: run it against a `Release` build of `Src/RCommon.sln` and confirm the output file contains every public type from at least `RCommon.Core`, `RCommon.Persistence`, and `RCommon.EFCore` (spot-check by grepping the output for a handful of known type/member names, e.g. `EFCoreAggregateRepository`, `TenantScope.Bypass`, `UnitOfWorkCommandBus`).

### File Summary

| File | Action |
|------|--------|
| `Src/Directory.Build.props` | Modify — enable `GenerateDocumentationFile` |
| `Tools/RCommon.ApiReferenceGenerator/` | Create (new console project) |
| `website/package.json` | Modify — add `generate:api` step |
| `website/static/api-reference-full.txt` | Generate (build output, not hand-authored) |
| `website/docs/api-reference/nuget-packages.mdx` | Modify — add pointer to the new artifact |

---

## Open Questions

- Sub-Spec D's generator: confirm the output should be one single concatenated file (matching `llms-full.txt`'s shape) rather than one file per package — a single file is simpler for an agent to fetch in one request, but could get large across ~40 packages. Leaning single-file unless size becomes a real problem (llms-full.txt precedent suggests it won't).
- Sub-Spec B/C new example projects: confirm none of them need real cloud credentials checked into CI — assumed emulator/local-substitute-only per "Must Not Do" above; flag now if any of Dapper/Linq2Db/Sagas/BlobStorage/StateMachines/MultiTenancy/Outbox is expected to demonstrate against a real managed service instead.
- Sequencing: given the size, recommend implementing and committing A → B → C → D in that order (matching the numbered priority already implicit in how the sub-specs are written), same one-spec-at-a-time review cadence used for Specs 1–6.
