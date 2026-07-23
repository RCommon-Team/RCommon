# Outbox DI Registration Hardening (3.2.1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:test-driven-development for every task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Eliminate the silent-data-loss defect where the transactional outbox stops persisting durable events under modular / multi-`WithPersistence` composition, harden the processor-only host registration, correct the 3.2.0 docs that describe unshipped features, and ship a worked modular multi-datastore example.

**Architecture:** The outbox producer decorates two core services — `IEntityEventTracker` (→ `OutboxEntityEventTracker`) and `IEventRouter` (→ `OutboxEventRouter` forwarder). Today both are registered with `TryAdd*`, and `RCommonBuilder`'s constructor registers `IEventRouter` with an unconditional `AddScoped`. So (a) the outbox `IEventRouter` forwarder *always* no-ops, and (b) the outbox `IEntityEventTracker` no-ops whenever a prior non-outbox `WithPersistence` call already registered the in-memory tracker via `WithEventTracking`. The fix makes the outbox registration **authoritative** (remove-then-add) so it wins regardless of call ordering, since `OutboxEntityEventTracker` is a strict superset of the in-memory tracker (durable → outbox, transient → in-process).

**Tech Stack:** .NET 10, `Microsoft.Extensions.DependencyInjection`, xUnit + FluentAssertions, EF Core InMemory provider (fast lane, no containers).

**Release:** 3.2.1 (patch; no public API surface change — behavior-only correction + additive diagnostic).

---

## Background — verified root causes (3.2.0 source)

- **#15 (High, silent data loss).** `OutboxPersistenceBuilderExtensions.AddOutboxProducer` uses `TryAddScoped<IEntityEventTracker, OutboxEntityEventTracker>` (line 69). `PersistenceBuilderExtensions.WithEventTracking` runs at the end of **every** `WithPersistence<T>` call and does `TryAddScoped<IEntityEventTracker, InMemoryEntityEventTracker>` (line 87). If any non-outbox `WithPersistence` runs before the outbox one (modular composition, second datastore, separate provider), the in-memory tracker is pinned first and the outbox `TryAdd` no-ops → durable events are dispatched in-process and **never written to the outbox**, with no exception and no warning. Single-`WithPersistence` happy path works because the `AddOutbox` call inside the delegate runs before `WithEventTracking`.
- **#15 nuance.** The customer attributed the root cause to the `IEventRouter` no-op. That no-op is real (`RCommonBuilder` ctor `AddScoped<IEventRouter, InMemoryTransactionalEventRouter>` unconditionally, so `AddOutboxProducer`'s `TryAddScoped<IEventRouter>` forwarder always no-ops) but it is **not** what breaks persistence — `OutboxEntityEventTracker` composes the *concrete* `OutboxEventRouter`, not the `IEventRouter` alias. Both are fixed here for correctness/consistency, but the load-bearing fix is `IEntityEventTracker`.
- **#15 diagnostic gap.** `OutboxRoutingDiagnosticsHostedService` only inspects `IEventRouter`'s last descriptor. Because the core ctor pins `IEventRouter` to the in-memory impl unconditionally, this diagnostic warns on **every** outbox configuration including the working one (false positive) and never inspects the load-bearing `IEntityEventTracker`. Fix it to inspect the tracker.
- **#16 (processor-only host).** `AddOutboxProcessor` registers only the shared core + hosted poller. To be reproduced with a DI smoke test (validateOnBuild) before choosing the fix; the poller itself resolves `IOutboxStore`, `IOutboxSerializer`, `IEventProducer[]`, `EventSubscriptionManager`, `IOutboxDataStoreRegistry`, `IBackoffStrategy`, `IOptions<OutboxOptions>`, `IOptions<DefaultDataStoreOptions>` — verify which is missing on a processor-only host and register it (or document the dual-call requirement).
- **#17 (docs over-claim).** The 3.2.0 changelog "Added" list includes an outbox metrics `Meter`, `IOutboxPayloadProtector`, and a deserialization allow-list (spec AC-18/19/20). Grep confirms these are **absent** from shipped `Src`. Correct changelog/migration/spec to mark them planned-not-shipped.

---

## Task 1: #15 regression test — modular composition drops durable events

**Files:**
- Test: `Examples/EventHandling/Examples.EventHandling.Outbox.Tests/ReproOutboxModularDiTests.cs` (create)

- [ ] **Step 1: Write the failing test.** Build a provider that mirrors modular composition: two separate `WithPersistence<EFCorePersistenceBuilder>` calls (or one non-outbox datastore registered before the outbox datastore) so `WithEventTracking` pins the in-memory tracker before `AddOutbox`. Publish a durable event, commit through the unit of work, then assert the owning datastore's `__OutboxMessages` has exactly one row. Also assert `provider.GetRequiredService<IEntityEventTracker>()` is `OutboxEntityEventTracker`.
- [ ] **Step 2: Run it, watch it fail** — rows = 0 and tracker = `InMemoryEntityEventTracker`. This is the reproduction.

## Task 2: #15 fix — authoritative outbox registration

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs:58-69`

- [ ] **Step 1:** In `AddOutboxProducer`, keep the concrete `TryAddScoped` registrations (`OutboxEventRouter`, `InMemoryTransactionalEventRouter`, `InMemoryEntityEventTracker`). Replace the two interface `TryAdd`s with authoritative remove-then-add so the outbox wins regardless of ordering:
  ```csharp
  builder.Services.RemoveAll<IEventRouter>();
  builder.Services.AddScoped<IEventRouter>(sp => sp.GetRequiredService<OutboxEventRouter>());
  builder.Services.RemoveAll<IEntityEventTracker>();
  builder.Services.AddScoped<IEntityEventTracker, OutboxEntityEventTracker>();
  ```
  Add a comment explaining why `TryAdd` is wrong here (defect #15). Idempotent across repeated `AddOutbox` calls (remove-then-add nets one). A later non-outbox `WithPersistence` `TryAdd` will no-op because the outbox registration is present.
- [ ] **Step 2:** Run Task 1 test → passes (1 row, tracker = `OutboxEntityEventTracker`).
- [ ] **Step 3:** Run the whole `Examples.EventHandling.Outbox.Tests` + `RCommon.Persistence` unit suites → all green (no regression to happy-path / two-datastore).
- [ ] **Step 4: Commit.**

## Task 3: #15 fix — routing diagnostic checks the load-bearing tracker

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxRoutingDiagnosticsHostedService.cs`
- Test: add a diagnostic test (a host where the tracker was overridden warns; a correct host does not)

- [ ] **Step 1:** Write a failing test asserting the diagnostic warns iff the effective `IEntityEventTracker` is not `OutboxEntityEventTracker` (and does **not** false-positive on a correctly-wired outbox host).
- [ ] **Step 2:** Change the diagnostic to inspect the last `IEntityEventTracker` descriptor (implementation type / factory target) instead of `IEventRouter`. With the Task 2 fix in place a correct host never warns.
- [ ] **Step 3:** Run → green. **Commit.**

## Task 4: #16 — processor-only host

**Files:**
- Test: `Examples/EventHandling/Examples.EventHandling.Outbox.Tests/ProcessorOnlyHostDiTests.cs` (create)
- Modify (pending repro): `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs` (`AddOutboxProcessor`)

- [ ] **Step 1:** Write a DI smoke test: `AddRCommon().WithPersistence(ef => { AddDbContext; SetDefaultDataStore; ef.AddOutboxProcessor<EFCoreOutboxStore>(); })` with `BuildServiceProvider(validateScopes: true, validateOnBuild: true)`, then start the host / resolve `IHostedService`s and run one poll batch. Observe the actual failure.
- [ ] **Step 2:** Based on the observed failure, either (a) have `AddOutboxProcessor` register the shared routing/tracker it needs, or (b) if a pure poller is genuinely self-sufficient and the failure only arises when the processor host also does domain writes, apply the same authoritative tracker registration and document the topology. Prefer the registration fix over docs-only.
- [ ] **Step 3:** Run → green. **Commit.**

## Task 5: #17 — correct the docs over-claim

**Files:**
- Modify: `website/docs/api-reference/changelog.mdx` (3.2.0 entry)
- Modify: `website/docs/api-reference/migration-guide.mdx`
- Modify: `docs/specs/event-handling/event-handling.md` (AC-18/19/20 status)
- Modify: `website/versioned_docs/version-3.2.0/api-reference/{changelog,migration-guide}.mdx` (released snapshot)

- [ ] **Step 1:** Remove metrics `Meter`, `IOutboxPayloadProtector`, and deserialization allow-list from the 3.2.0 "Added" list; move them to a "Planned / not yet shipped" note (or delete). Mark AC-18/19/20 in the spec as deferred. **Commit.**

## Task 6: modular multi-datastore outbox example

**Files:**
- Create: `Examples/EventHandling/Examples.EventHandling.Outbox.Modular/` (project) — module registration extension methods (one per bounded context), each calling `WithPersistence`/`WithEventHandling`; a composition root wiring 2–3 datastores with the native outbox; a `Program` that commits and prints outbox row counts.
- Create: matching `*.Tests` asserting durable rows persist across all datastores under the modular composition.
- Add both to the solution.

- [ ] **Step 1:** Build the modular example illustrating exactly the customer's shape (multiple modules, multiple datastores, native `AddOutbox`). Test proves rows persist. **Commit.**

## Task 7: green + finish

- [x] Run full solution build + the event-handling/persistence test suites (fast lane, `Category!=Integration`).
- [ ] Push branch; open PR. (Deferred — user reviewing commits first.)
- [ ] Do **not** tag/release until the user approves the PR.

## Verification (2026-07-23)

**Unit (fast lane, `Category!=Integration`) — all green:**
RCommon.Persistence 453 · RCommon.Core 575 · RCommon.EfCore 158 (+3 skipped) · RCommon.Entities 236 · RCommon.Wolverine.Outbox 9 · RCommon.Dapper 92 · RCommon.Linq2Db 73.

**Examples (whole `Examples.sln`, fast lane) — all green:**
Outbox 6 (incl. the #15 & #16 repros) · Outbox.Modular 2 · MediatR 1 · NoUnitOfWork 2 · TransactionScript 1 · DomainDrivenDesign 10 · Persistence.Sagas 3 · HR.LeaveManagement 4. Full `Examples.sln` builds. Modular example console app runs clean (1 outbox row per datastore, none leaked).

**Integration (Podman → Postgres/RabbitMQ) — outbox path all green when containers start:**
`CrossDataStoreOutboxTests` 2/2 · `RecipeTwoBBrokerOutboxTests` 2/2 · `MassTransitOutboxCoordinationSpikeTests` 2/2 · `WolverineOutboxCoordinationSpikeTests` 3/3. Running all four collections at once produced 5 `PostgreSqlFixture.InitializeAsync` container-start timeouts (named-pipe connect) — an environmental 2 GiB-Podman contention issue, not an assertion/DI failure; each class passes when run alone.

**Not run (out of scope for the outbox DI change):** S3 (LocalStack) / Azure (Azurite) blob-storage integration tests — unrelated subsystem, require emulators.
