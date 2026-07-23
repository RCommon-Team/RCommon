# Event Handling 3.2.0 — Phase 1: Datastore-Aware Transactional Outbox (B4/U5) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thread datastore identity through the entire transactional-outbox pipeline (tracker → router → store → poller) so that an event's outbox row is always written to the **same** DbContext/datastore/transaction as the state change that produced it, each outbox row records its target producer(s), and the poller drains every registered outbox datastore.

**Architecture:** Today one scoped `OutboxEventRouter` funnels every datastore's events into one `IOutboxStore` pinned to the default datastore at construction — so a non-default-datastore aggregate's event is written to the wrong database and loses atomicity (feedback B4/U5). This phase (a) captures a datastore name at `AddEntity`, (b) groups harvested events by datastore in the tracker, (c) makes `IOutboxStore` datastore-parametric so a single `EFCoreOutboxStore` resolves the correct `RCommonDbContext` per call via `IDataStoreFactory`, (d) records target producer(s) per row, (e) auto-maps the `OutboxMessage` entity onto every outbox-owning datastore's model with a fail-loud startup verification, and (f) makes `OutboxProcessingService` iterate every registered outbox datastore. A new `IOutboxDataStoreRegistry` singleton is the single source of truth for "which datastores own an outbox."

**Tech Stack:** .NET 10 (Src multi-targets net8/9/10; Tests are net10.0), EF Core 10.0.8, xUnit 2.9.3, AwesomeAssertions 7.2.1 (`FluentAssertions` namespace), Moq 4.20.72; the Phase 0 Podman/Testcontainers harness (`Tests/RCommon.IntegrationTests`, Postgres) for the cross-datastore atomicity proof.

**Spec:** `docs/specs/event-handling/event-handling.md` — this phase implements **AC-7** (per-datastore outbox), **AC-8** (datastore threading), **AC-9** (multi-datastore polling), **AC-10** (target-producer routing), **AC-11** (schema auto-map + verify), and the **AC-17** shims for these seams. **Design:** `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md` §3. **Branch:** `feature/event-handling-outbox-recipes` (Phase 0 already merged into this branch; do not switch branches).

---

## Scope & boundaries

**In scope (AC-7–AC-11, AC-17 for these seams):**
- Datastore capture at `IEntityEventTracker.AddEntity`, grouping in the tracker, datastore-parametric `IOutboxStore`, per-datastore polling, target-producer recording + honoring, schema auto-map + startup verify, and back-compat shims.

**Explicitly out of scope (later phases — do NOT build here):**
- Pipeline reorder / pre-commit ordered dispatch / FIFO drain queue / cycle-breaker (**Phase 2**, AC-3–AC-6). This phase keeps the existing harvest-at-`PersistEventsAsync` + post-commit `EmitTransactionalEventsAsync` mechanism intact and only makes it datastore-aware.
- Fluent API verbs (`Publish`/`Send`/`Consume`/`UseOutbox`/`UseRCommonOutbox`), the rich event→producer route map, `AddOutboxProducer`/`AddOutboxProcessor` topology split (**Phase 3**, AC-12/13/21).
- MassTransit/Wolverine wrappers and recipes (**Phase 4**).
- **Target-producer derivation** in this phase uses the *current* producer-resolution mechanism (`EventSubscriptionManager` + registered `IEventProducer`s), recording matched producer **type full-names** per row. Phase 3's route map refines how targets are chosen; this phase only builds the column, the recording seam, and the poller honoring it (with a null/empty ⇒ resolve-all back-compat fallback).

**Key simplifying decision (YAGNI):** "Per-datastore `__OutboxMessages` table" means each datastore's own database has its own `__OutboxMessages` table — it does **not** require distinct table *names*. The global `OutboxOptions.TableName` default (`"__OutboxMessages"`) is kept; each outbox-owning datastore's `RCommonDbContext` maps that entity in its own database. Do not introduce per-datastore table-name configuration in this phase.

---

## File structure

> **Path note (verified):** The EF Core outbox files (`EFCoreOutboxStore.cs`, `OutboxMessageConfiguration.cs`, `ModelBuilderExtensions.cs`) live under **`Src/RCommon.EfCore/Outbox/`** — their C# namespace is `RCommon.Persistence.EFCore.Outbox`, but there is **no** `Src/RCommon.Persistence.EFCore/` project/directory. Use the `Src/RCommon.EfCore/Outbox/` paths in all `git add` commands.

**Core / persistence abstractions (`Src/RCommon.Persistence/`)**
- Modify `Outbox/IOutboxMessage.cs` — add `string? TargetProducers`.
- Modify `Outbox/OutboxMessage.cs` — add `TargetProducers` property.
- Modify `Outbox/IOutboxStore.cs` — every method gains a `string dataStoreName` parameter.
- Create `Outbox/IOutboxDataStoreRegistry.cs` — registry of outbox-owning datastore names.
- Create `Outbox/OutboxDataStoreRegistry.cs` — default singleton implementation.
- Modify `Outbox/OutboxEventRouter.cs` — buffer `(event, dataStoreName)`; group + persist per datastore; record target producers.
- Modify `Outbox/OutboxEntityEventTracker.cs` — harvest per `(entity, dataStoreName)`, group by datastore.
- Modify `Outbox/OutboxProcessingService.cs` — iterate every registered outbox datastore; honor `TargetProducers`.
- Modify `Outbox/OutboxPersistenceBuilderExtensions.cs` — register the registry; `AddOutbox` records the datastore (default = default datastore).

**Entities (`Src/RCommon.Entities/`)**
- Modify `IEntityEventTracker.cs` — add `AddEntity(IBusinessEntity, string dataStoreName)`.
- Modify `InMemoryEntityEventTracker.cs` — store `(entity, dataStoreName)` pairs; keep flat overload.

**EF Core provider (`Src/RCommon.EfCore/`)**
- Modify `Outbox/EFCoreOutboxStore.cs` — remove pinned `_dataStoreName`; resolve context per call from the `dataStoreName` argument.
- Modify `RCommonDbContext.cs` — auto-apply `OutboxMessage` mapping when this context's datastore owns an outbox.
- Modify `Crud/EFCoreRepository.cs` + `Crud/EFCoreAggregateRepository.cs` — pass `DataStoreName` to `EventTracker.AddEntity`.
- Create `Outbox/OutboxSchemaVerificationHostedService.cs` — startup fail-loud if a registered outbox datastore lacks the mapping/table.
- (Shim) Modify `Outbox/EFCoreOutboxStore.cs` region or add `Outbox/EFCoreOutboxStoreOfT.cs` — `[Obsolete] EFCoreOutboxStore<TContext>`.

**Other providers (keep compiling — datastore-parametric interface)**
- Modify `Src/RCommon.Dapper/Outbox/DapperOutboxStore.cs` — resolve connection per `dataStoreName`.
- Modify `Src/RCommon.Linq2Db/Outbox/Linq2DbOutboxStore.cs` — resolve data connection per `dataStoreName`.

**Tests**
- Modify `Tests/RCommon.Persistence.Tests/OutboxEventRouterTests.cs`, `OutboxEntityEventTrackerTests.cs`, `OutboxProcessingServiceTests.cs`.
- Modify `Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs`, `OutboxImmediateDispatchTests.cs`.
- Modify `Tests/RCommon.Dapper.Tests/DapperOutboxStoreTests.cs`, `Tests/RCommon.Linq2Db.Tests/Linq2DbOutboxStoreTests.cs` (signature updates).
- Create `Tests/RCommon.Persistence.Tests/OutboxDataStoreRegistryTests.cs`.
- Create `Tests/RCommon.EfCore.Tests/OutboxSchemaVerificationHostedServiceTests.cs`.
- Create `Tests/RCommon.IntegrationTests/CrossDataStoreOutboxTests.cs` — Podman/Postgres two-datastore atomicity proof (AC-7). MUST carry class-level `[Trait("Category", "Integration")]`.

> **CRITICAL — CI trait convention (unchanged from Phase 0).** Any new test class that starts containers MUST carry class-level `[Trait("Category", "Integration")]`; the fast CI job runs `dotnet test Src/RCommon.sln --filter "Category!=Integration"` on a container-less runner. Only `CrossDataStoreOutboxTests` in this plan is container-backed; all other new/modified tests are plain unit tests (no trait) that run in the fast lane.

> **Interface-break discipline.** Changing `IOutboxStore` (Task 3) breaks all three provider implementations at once. That task modifies the interface **and** all three implementations **and** the router/poller call sites **and** the affected tests in a single commit so the solution never lands in a non-building state. Verify with a full `dotnet build Src/RCommon.sln` before committing Task 3.

---

## Recommended task order

Bottom-up so the solution keeps compiling and each layer's tests are green before the next depends on it: **1** (entity field) → **2** (registry + registration) → **3** (datastore-parametric store, all providers) → **4** (tracker capture) → **5** (repo call sites) → **6** (router/tracker grouping + target recording) → **7** (DbContext auto-map) → **8** (startup verify) → **9** (poller multi-datastore + honor targets) → **10** (AC-17 shims) → **11** (Podman cross-datastore atomicity proof).

---

### Task 1: Add `TargetProducers` to the outbox message

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/IOutboxMessage.cs`
- Modify: `Src/RCommon.Persistence/Outbox/OutboxMessage.cs`
- Modify: `Src/RCommon.EfCore/Outbox/OutboxMessageConfiguration.cs`
- Test: `Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs`

- [ ] **Step 1: Write the failing test** — extend an existing EFCore outbox store round-trip test (or add `SaveAsync_persists_TargetProducers`) asserting that a saved `OutboxMessage` with `TargetProducers = "Ns.MyProducer"` reads back with that value. (Use the existing in-memory/SQLite DbContext test harness in `EFCoreOutboxStoreTests.cs`.)

- [ ] **Step 2: Run it — verify it FAILS** (property/column does not exist).
Run: `dotnet test Tests/RCommon.EfCore.Tests --filter "FullyQualifiedName~EFCoreOutboxStoreTests"`

- [ ] **Step 3: Implement**
  - `IOutboxMessage`: add `string? TargetProducers { get; set; }` (nullable — old rows have none).
  - `OutboxMessage`: add the property.
  - `OutboxMessageConfiguration`: map the column, e.g. `builder.Property(x => x.TargetProducers);` (nullable string, no length cap needed for Phase 1; follow the file's existing property style).

- [ ] **Step 4: Run tests — verify PASS.**

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.Persistence/Outbox/IOutboxMessage.cs Src/RCommon.Persistence/Outbox/OutboxMessage.cs Src/RCommon.EfCore/Outbox/OutboxMessageConfiguration.cs Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs
git commit -m "feat(outbox): add TargetProducers column to outbox message (AC-10)"
```

---

### Task 2: Outbox datastore registry + datastore-aware `AddOutbox`

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/IOutboxDataStoreRegistry.cs`
- Create: `Src/RCommon.Persistence/Outbox/OutboxDataStoreRegistry.cs`
- Modify: `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs`
- Test: `Tests/RCommon.Persistence.Tests/OutboxDataStoreRegistryTests.cs`

**Design:** A singleton that records the set of datastore names that own an outbox. `AddOutbox` gains an optional datastore name; when omitted it registers the default datastore (resolved from `DefaultDataStoreOptions.DefaultDataStoreName`). Multiple `AddOutbox(...)` calls accumulate distinct datastore names.

- [ ] **Step 1: Write the failing test** (`OutboxDataStoreRegistryTests`): register `"Orders"` and `"Billing"`; assert `Registrations` contains both (distinct, order-independent); registering `"Orders"` twice yields one entry.

- [ ] **Step 2: Run — verify FAIL** (types don't exist).
Run: `dotnet test Tests/RCommon.Persistence.Tests --filter "FullyQualifiedName~OutboxDataStoreRegistryTests"`

- [ ] **Step 3: Implement**
```csharp
// IOutboxDataStoreRegistry.cs
namespace RCommon.Persistence.Outbox;

public interface IOutboxDataStoreRegistry
{
    void Register(string dataStoreName);
    IReadOnlyCollection<string> Registrations { get; }
}
```
```csharp
// OutboxDataStoreRegistry.cs — thread-safe, distinct by name (case-sensitive to match IDataStoreFactory naming)
using System.Collections.Concurrent;
namespace RCommon.Persistence.Outbox;

public sealed class OutboxDataStoreRegistry : IOutboxDataStoreRegistry
{
    private readonly ConcurrentDictionary<string, byte> _names = new();
    public void Register(string dataStoreName) => _names.TryAdd(dataStoreName, 0);
    public IReadOnlyCollection<string> Registrations => _names.Keys.ToArray();
}
```
  - In `OutboxPersistenceBuilderExtensions.AddOutbox<TOutboxStore>`: add an optional `string? dataStoreName = null` parameter. `TryAddSingleton<IOutboxDataStoreRegistry, OutboxDataStoreRegistry>()`. After building services, register the datastore name into the registry at configuration time: resolve the name now if provided; otherwise defer to the default. Because the default name is known only from `DefaultDataStoreOptions`, register via a small `IConfigureOptions`-free approach: capture into the singleton instance directly. Concretely, construct/resolve the singleton instance during `AddOutbox` and call `.Register(name)`, falling back to reading the default at first-use if `dataStoreName` is null.

  > **Note for the implementer:** `DefaultDataStoreOptions` is configured via `SetDefaultDataStore(...)` on the persistence builder and may be set *after* `AddOutbox` runs. To avoid ordering fragility, register the **explicit** name eagerly when provided, and when `dataStoreName` is null, register a sentinel that the registry resolves to the default at first read (inject `IOptions<DefaultDataStoreOptions>` into consumers, or resolve the default in the registry's `Registrations` getter). Choose the simplest approach that makes the test in Step 1 and Task 9's poller test pass; document the choice in a code comment. If this proves ambiguous, prefer: `AddOutbox` with no name registers `DefaultDataStoreOptions.DefaultDataStoreName` by reading it lazily via an injected `IOptions<DefaultDataStoreOptions>` in a `IOutboxDataStoreRegistry` implementation that composes explicit names + the default.

- [ ] **Step 4: Run — verify PASS.**

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.Persistence/Outbox/IOutboxDataStoreRegistry.cs Src/RCommon.Persistence/Outbox/OutboxDataStoreRegistry.cs Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs Tests/RCommon.Persistence.Tests/OutboxDataStoreRegistryTests.cs
git commit -m "feat(outbox): registry of outbox-owning datastores + datastore-aware AddOutbox (AC-8)"
```

---

### Task 3: Make `IOutboxStore` datastore-parametric (all providers)

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/IOutboxStore.cs`
- Modify: `Src/RCommon.EfCore/Outbox/EFCoreOutboxStore.cs`
- Modify: `Src/RCommon.Dapper/Outbox/DapperOutboxStore.cs`
- Modify: `Src/RCommon.Linq2Db/Outbox/Linq2DbOutboxStore.cs`
- Modify (callers, to keep building): `Src/RCommon.Persistence/Outbox/OutboxEventRouter.cs`, `Src/RCommon.Persistence/Outbox/OutboxProcessingService.cs`
- Test: `Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs`, and signature fixes in `Tests/RCommon.Dapper.Tests/DapperOutboxStoreTests.cs`, `Tests/RCommon.Linq2Db.Tests/Linq2DbOutboxStoreTests.cs`

**Design:** Add a `string dataStoreName` parameter to **every** `IOutboxStore` method (`SaveAsync`, `ClaimAsync`, `MarkProcessedAsync`, `MarkFailedAsync`, `MarkDeadLetteredAsync`, `DeleteProcessedAsync`, `DeleteDeadLetteredAsync`, `GetDeadLettersAsync`, `ReplayDeadLetterAsync`). `EFCoreOutboxStore` stops reading `DefaultDataStoreOptions` at construction and instead resolves `_dataStoreFactory.Resolve<RCommonDbContext>(dataStoreName)` inside each method. Same pattern for Dapper (`Resolve<RDbConnection>(dataStoreName)`) and Linq2Db (`Resolve<RCommonDataConnection>(dataStoreName)`).

- [ ] **Step 1: Write the failing test** — in `EFCoreOutboxStoreTests`, add `SaveAsync_writes_to_the_named_datastore`: register **two** EF datastores (`"A"`, `"B"`) in the test service provider, save a message with `dataStoreName: "B"`, and assert it appears in B's context and **not** in A's. (This is the core B4 behavior at the store layer.)

- [ ] **Step 2: Run — verify FAIL** (won't compile: method has no datastore param → good, that's the red).
Run: `dotnet test Tests/RCommon.EfCore.Tests --filter "FullyQualifiedName~EFCoreOutboxStoreTests"`

- [ ] **Step 3: Implement**
  - `IOutboxStore`: add `string dataStoreName` to each method signature — **immediately before the trailing `CancellationToken`** (this rule applies uniformly to all 9 methods; several — `ClaimAsync`, `GetDeadLettersAsync`, `MarkProcessedAsync`, `DeleteProcessedAsync` — have no `IOutboxMessage message` parameter, and `MarkFailedAsync` has extra params).
  - `EFCoreOutboxStore`: delete the `_dataStoreName` field and its `DefaultDataStoreOptions` constructor dependency; change `private RCommonDbContext DbContext => ...` into `private RCommonDbContext Context(string name) => _dataStoreFactory.Resolve<RCommonDbContext>(name);` and use `Context(dataStoreName)` in each method.
  - `DapperOutboxStore` + `Linq2DbOutboxStore`: same — resolve per call from the argument; drop the pinned name/constructor option.
  - Update the two callers minimally to compile: `OutboxEventRouter` and `OutboxProcessingService` currently call `SaveAsync(message, ct)` / `ClaimAsync(instanceId, ...)` etc. — pass a datastore name through. For this task, thread the **default datastore name** so behavior is unchanged (real grouping/iteration lands in Tasks 6 and 9). Inject `IOptions<DefaultDataStoreOptions>` where needed for the interim default.

- [ ] **Step 4: Full solution build + tests**
Run: `dotnet build Src/RCommon.sln` (MUST be 0 errors — this is the interface-break checkpoint)
Run: `dotnet test Tests/RCommon.EfCore.Tests Tests/RCommon.Dapper.Tests Tests/RCommon.Linq2Db.Tests --filter "Category!=Integration"`
Expected: PASS (including the new two-datastore test).

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.Persistence/Outbox/IOutboxStore.cs Src/RCommon.EfCore/Outbox/EFCoreOutboxStore.cs Src/RCommon.Dapper/Outbox/DapperOutboxStore.cs Src/RCommon.Linq2Db/Outbox/Linq2DbOutboxStore.cs Src/RCommon.Persistence/Outbox/OutboxEventRouter.cs Src/RCommon.Persistence/Outbox/OutboxProcessingService.cs Tests/RCommon.EfCore.Tests/ Tests/RCommon.Dapper.Tests/ Tests/RCommon.Linq2Db.Tests/
git commit -m "feat(outbox)!: make IOutboxStore datastore-parametric; resolve context per call (AC-8, U5)"
```

---

### Task 4: Datastore capture at the tracker

**Files:**
- Modify: `Src/RCommon.Entities/IEntityEventTracker.cs`
- Modify: `Src/RCommon.Entities/InMemoryEntityEventTracker.cs`
- Test: `Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs` (or a new `InMemoryEntityEventTrackerTests` if the flat tracker isn't yet covered)

**Design (design §3 step 1):** `AddEntity(entity, dataStoreName)` stores `(entity, dataStoreName)`; the existing `AddEntity(entity)` overload is preserved and defaults to the default datastore (**AC-17**). The in-memory tracker exposes tracked entities grouped by (or annotated with) datastore for the decorator to consume.

- [ ] **Step 1: Write the failing test** — add two entities under datastores `"A"` and `"B"`; assert the tracker exposes each entity associated with its datastore name (e.g. a `TrackedEntitiesByDataStore` grouping or a `(entity, name)` collection).

- [ ] **Step 2: Run — verify FAIL.**
Run: `dotnet test Tests/RCommon.Persistence.Tests --filter "FullyQualifiedName~EntityEventTracker"`

- [ ] **Step 3: Implement**
  - `IEntityEventTracker`: add `void AddEntity(IBusinessEntity entity, string dataStoreName);` Keep the existing `AddEntity(IBusinessEntity entity)`.
  - `InMemoryEntityEventTracker`: replace the flat `List<IBusinessEntity>` with a structure preserving datastore association (e.g. `List<(IBusinessEntity Entity, string DataStore)>`); `AddEntity(entity)` delegates to `AddEntity(entity, defaultName)` where the default is injected via `IOptions<DefaultDataStoreOptions>` (add the dependency) or an empty sentinel resolved to default. Expose the grouped view the decorator needs. Keep `TrackedEntities` (flat) working for any existing callers/back-compat.

- [ ] **Step 4: Run — verify PASS.**

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.Entities/IEntityEventTracker.cs Src/RCommon.Entities/InMemoryEntityEventTracker.cs Tests/RCommon.Persistence.Tests/
git commit -m "feat(outbox): capture datastore name at IEntityEventTracker.AddEntity (AC-8, AC-17)"
```

---

### Task 5: Repositories pass their datastore name to the tracker

**Files:**
- Modify: `Src/RCommon.EfCore/Crud/EFCoreRepository.cs` (call sites ~162, ~183, ~203)
- Modify: `Src/RCommon.EfCore/Crud/EFCoreAggregateRepository.cs` (analogous `EventTracker.AddEntity` call sites)
- Test: covered end-to-end by Task 11; add a focused unit test if the repo layer has an existing test harness (`Tests/RCommon.EfCore.Tests`).

**Design:** Both repository base classes already expose `DataStoreName` (used at `_dataStoreFactory.Resolve<RCommonDbContext>(this.DataStoreName)`). Replace `EventTracker.AddEntity(entity)` with `EventTracker.AddEntity(entity, this.DataStoreName)` at every call site. **There are more call sites than a quick glance suggests** — grep confirmed ~5 in `EFCoreRepository.cs` (≈162, 183, 203, 288, 583) and ~8 in `EFCoreAggregateRepository.cs` (≈161, 182, 202, 287, 582, 623, 646, 652). Change **all** of them; do not stop at the first three.

- [ ] **Step 1: Write the failing test** — prefer a lightweight in-memory/SQLite repository test (the EFCore test project already spins up DbContexts against SQLite/in-memory) so Task 5 is honestly red-green in isolation rather than landing green-untested until Task 11. Configure a repository for datastore `"B"`, add an entity, and assert the (spy/mock) tracker associated that entity with `"B"`. Only if a repo-level harness genuinely cannot be stood up without full EF wiring, fall back to writing **Task 11's** failing assertion first and note the deferral explicitly in the commit message.

- [ ] **Step 2: Run — verify FAIL** (entity associated with default, not `"B"`).

- [ ] **Step 3: Implement** — grep both files for `EventTracker.AddEntity(` and change **every** occurrence to pass `this.DataStoreName`. Confirm no other `AddEntity(` call sites in `Src/RCommon.EfCore` were missed.

- [ ] **Step 4: Run — verify PASS**; `dotnet build Src/RCommon.sln` clean.

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.EfCore/Crud/EFCoreRepository.cs Src/RCommon.EfCore/Crud/EFCoreAggregateRepository.cs Tests/RCommon.EfCore.Tests/
git commit -m "feat(outbox): repositories pass DataStoreName to the event tracker (AC-8)"
```

---

### Task 6: Router/tracker group by datastore + record target producers

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs`
- Modify: `Src/RCommon.Persistence/Outbox/OutboxEventRouter.cs`
- Test: `Tests/RCommon.Persistence.Tests/OutboxEventRouterTests.cs`, `Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs`

**Design (design §3 steps 2–3, §"Target producers per row"):** `OutboxEntityEventTracker.PersistEventsAsync` harvests events from each tracked `(entity graph, dataStoreName)`, associates each harvested event with its entity's datastore, and persists each event to **that** datastore's outbox via the now-parametric store. At persist time, resolve the matching producers for the event (current mechanism: `EventSubscriptionManager` + registered `IEventProducer`s) and record their **type full-names** into `OutboxMessage.TargetProducers` (e.g. comma-separated or JSON). `OutboxEventRouter` buffers `(event, dataStoreName)` and, in `PersistBufferedEventsAsync`, groups by datastore and calls `SaveAsync(message, dataStoreName, ct)`.

- [ ] **Step 1: Write the failing tests**
  - Router: buffer two events for datastore `"A"` and one for `"B"`; call persist; assert the mock `IOutboxStore.SaveAsync` was invoked with `dataStoreName == "A"` twice and `"B"` once, and that each saved message's `TargetProducers` equals the resolved producer type-name(s).
  - Tracker: two entities in `"A"`/`"B"` each raising a local event; assert events persist to their respective datastores.

- [ ] **Step 2: Run — verify FAIL.**
Run: `dotnet test Tests/RCommon.Persistence.Tests --filter "FullyQualifiedName~OutboxEventRouterTests|FullyQualifiedName~OutboxEntityEventTrackerTests"`

- [ ] **Step 3: Implement**
  - `OutboxEventRouter`: change the buffer to hold `(ISerializableEvent Event, string DataStore)`; add `AddTransactionalEvents(IEnumerable<ISerializableEvent>, string dataStoreName)` (keep the old overloads defaulting to the default datastore for back-compat). In `PersistBufferedEventsAsync`, group by datastore and `SaveAsync(msg, group.Key, ct)`. Populate `msg.TargetProducers` by resolving matching producers (reuse the existing subscription-manager/producer filtering already present in `RouteEventsAsync`) and joining their `GetType().FullName`.
  - `OutboxEntityEventTracker`: when harvesting, walk each tracked entity's graph and buffer harvested events with the entity's datastore name (from Task 4's grouped view), then flush.

- [ ] **Step 4: Run — verify PASS.**

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs Src/RCommon.Persistence/Outbox/OutboxEventRouter.cs Tests/RCommon.Persistence.Tests/
git commit -m "feat(outbox): group events by datastore and record target producers at persist (AC-8, AC-10)"
```

---

### Task 7: `RCommonDbContext` auto-maps the outbox for outbox-owning datastores

**Files:**
- Modify: `Src/RCommon.EfCore/RCommonDbContext.cs`
- (Reuse) `Src/RCommon.EfCore/Outbox/ModelBuilderExtensions.cs` (`AddOutboxMessages`)
- Test: `Tests/RCommon.EfCore.Tests/` (new test class or extend an existing model test)

**Design (design §3 "Schema provisioning"):** In `OnModelCreating`, if this context's datastore name is present in `IOutboxDataStoreRegistry.Registrations`, call `modelBuilder.AddOutboxMessages(tableName)` automatically (no manual call).

> **⚠ This is the hardest task in the phase — expect to build a genuinely new seam, not to reuse an existing one.** Verified facts the implementer must design around:
> - `IDataStore` (`Src/RCommon.Persistence/IDataStore.cs`) exposes **only** `GetDbConnection()` — there is **no** `Name` property. `RCommonDbContext` (`Src/RCommon.EfCore/RCommonDbContext.cs`) currently has **no** datastore-name awareness and does **not** override `OnModelCreating`.
> - Today the outbox mapping is applied **manually** by the developer calling `modelBuilder.AddOutboxMessages()` inside their own `OnModelCreating` (see `Examples.EventHandling.Outbox/AppDbContext.cs` and the EFCore test contexts). This task moves that into the base context, gated on registry membership — while leaving the manual call working for back-compat (idempotent / no double-map).
> - Two sub-problems to solve: (a) **the context must learn its own registered datastore name** — it is registered via `IDataStoreFactory` under a string name, but the instance doesn't carry that name today; introduce a minimal way for the context to know its name (e.g. a name set during `AddDbContext(name, ...)` registration, or a lookup through `DataStoreFactoryOptions`). (b) **the context must read `IOutboxDataStoreRegistry` from `OnModelCreating`**, where scoped-service injection is awkward — pass the registry (and the resolved name) via the context constructor / a `DbContextOptions` extension / a small accessor, consistent with how RCommon already supplies services to `RCommonDbContext`.
> - Prefer the **smallest** seam that satisfies the tests; do not broadly refactor `RCommonDbContext`. Document the seam you add in a code comment. If, once in the code, the clean approach is materially different from the sketch above (e.g. auto-map is better applied via a model-customization/convention registered at `AddDbContext` time rather than an `OnModelCreating` override), **that is acceptable** — the acceptance criterion is only that a registered outbox datastore's model ends up with `OutboxMessage` mapped and a non-registered one does not. If you find yourself blocked choosing between seams, report back for guidance rather than guessing.

Developer still owns migrations (RCommon only shapes the model).

- [ ] **Step 1: Write the failing test** — register an outbox on datastore `"A"`; build the model for A's `RCommonDbContext`; assert `model.FindEntityType(typeof(OutboxMessage))` is non-null (mapped). Register a context on `"C"` with **no** outbox; assert `OutboxMessage` is **not** mapped there.

- [ ] **Step 2: Run — verify FAIL.**

- [ ] **Step 3: Implement** — resolve the registry inside the context, guard `AddOutboxMessages` on membership. Follow the existing pattern for how `RCommonDbContext` obtains its datastore name and any injected services; if none exists, add a minimal seam (documented) rather than a broad refactor.

- [ ] **Step 4: Run — verify PASS**; `dotnet build Src/RCommon.sln` clean.

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.EfCore/RCommonDbContext.cs Tests/RCommon.EfCore.Tests/
git commit -m "feat(outbox): auto-map OutboxMessage on outbox-owning datastores (AC-11)"
```

---

### Task 8: Startup schema verification (fail-loud)

**Files:**
- Create: `Src/RCommon.EfCore/Outbox/OutboxSchemaVerificationHostedService.cs`
- Create/Modify: an **EfCore-side registration seam** (see design note) — e.g. `Src/RCommon.EfCore/Outbox/EFCoreOutboxBuilderExtensions.cs` (new) or an existing `IEFCorePersistenceBuilder` extension.
- Test: `Tests/RCommon.EfCore.Tests/OutboxSchemaVerificationHostedServiceTests.cs`

**Design (design §3, AC-11; consistent with the 3.1.3 fail-loud `OutboxRoutingDiagnosticsHostedService`):** On startup, for each `IOutboxDataStoreRegistry` datastore, resolve its `RCommonDbContext` and verify the `OutboxMessage` entity is mapped **and** the underlying table is reachable. If missing, **fail loud** — throw on startup (or log a high-severity warning, matching the existing diagnostic's severity convention; prefer throw for a *registered-but-unmapped* datastore since that is a misconfiguration that silently drops events). Model the class on the existing `OutboxRoutingDiagnosticsHostedService` (internal sealed hosted service).

> **⚠ Cross-project registration gap (must resolve):** This hosted service needs **EF types** (resolve `RCommonDbContext`, call `IModel.FindEntityType(typeof(OutboxMessage))`), so it must live in `RCommon.EfCore`. But the **only** `AddOutbox<TOutboxStore>` extension today lives in `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs`, and the dependency direction is `RCommon.EfCore → RCommon.Persistence` — so the Persistence-side `AddOutbox` **cannot** reference an EF-typed hosted service. There is no EfCore-side outbox registration hook today. **Resolution:** introduce a small EfCore-side registration extension (on the existing `IEFCorePersistenceBuilder`) that registers this hosted service — e.g. the developer's outbox setup on an EF datastore calls that extension, or `EFCorePersistenceBuilder` auto-registers it when an outbox is configured. Do NOT try to register an EF-typed service from `RCommon.Persistence`. Keep the Persistence-side `AddOutbox` (store/router/tracker/poller/registry, all provider-agnostic) as-is; add the EF-specific verification registration on the EfCore side.

- [ ] **Step 1: Write the failing test** — registry has `"A"` (mapped) and `"Missing"` (a context without the outbox mapping); assert the service throws/logs loud for `"Missing"` and is silent for `"A"`. Use the existing hosted-service test pattern from `OutboxRoutingDiagnosticsHostedServiceTests.cs`.

- [ ] **Step 2: Run — verify FAIL.**

- [ ] **Step 3: Implement** the hosted service; register it in `AddOutbox` alongside the existing diagnostics service.

- [ ] **Step 4: Run — verify PASS.**

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.EfCore/Outbox/OutboxSchemaVerificationHostedService.cs Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs Tests/RCommon.EfCore.Tests/OutboxSchemaVerificationHostedServiceTests.cs
git commit -m "feat(outbox): fail-loud startup verification of outbox schema per datastore (AC-11)"
```

---

### Task 9: Poller drains every registered outbox datastore + honors target producers

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxProcessingService.cs`
- Test: `Tests/RCommon.Persistence.Tests/OutboxProcessingServiceTests.cs`

**Design (design §3 step 4, AC-9, AC-10):** `ProcessBatchAsync` iterates `IOutboxDataStoreRegistry.Registrations`; for each datastore it claims + drains that datastore's outbox (`store.ClaimAsync(instanceId, batch, lock, dataStoreName, ct)` and the mark/delete calls all pass that name). When a claimed row has non-empty `TargetProducers`, dispatch it **only** to producers whose `GetType().FullName` is in that set (AC-10); when null/empty (legacy rows), fall back to the current resolve-all behavior (AC-17 back-compat). Preserve the existing "warn once per event type with zero matching subscribers" behavior from 3.1.3.

- [ ] **Step 1: Write the failing tests**
  - Multi-datastore: registry `{"A","B"}`, pending rows in both; assert `ClaimAsync` is called for `"A"` and `"B"` and rows from both are processed.
  - Target honoring: a row with `TargetProducers = "Ns.ProducerX"` and two registered producers X and Y; assert only X's `ProduceEventAsync` is invoked.
  - Back-compat: a row with `TargetProducers = null` dispatches via the existing resolve-all path.

- [ ] **Step 2: Run — verify FAIL.**
Run: `dotnet test Tests/RCommon.Persistence.Tests --filter "FullyQualifiedName~OutboxProcessingServiceTests"`

- [ ] **Step 3: Implement** the per-datastore loop and target-producer filtering; resolve `IOutboxDataStoreRegistry` from the per-batch scope.

- [ ] **Step 4: Run — verify PASS.**

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.Persistence/Outbox/OutboxProcessingService.cs Tests/RCommon.Persistence.Tests/OutboxProcessingServiceTests.cs
git commit -m "feat(outbox): poller drains every outbox datastore and honors target producers (AC-9, AC-10)"
```

---

### Task 10: AC-17 back-compat shims

**Files:**
- Create/Modify: `Src/RCommon.EfCore/Outbox/EFCoreOutboxStore.cs` (or a new `EFCoreOutboxStoreOfT.cs`)
- Test: `Tests/RCommon.EfCore.Tests/`

**Design (AC-17):** The pre-3.2.0 pattern pinned a store to a context type/subclass. Provide `[Obsolete("Datastore is now selected per call; register EFCoreOutboxStore and pass the datastore name.")] public class EFCoreOutboxStore<TContext> : EFCoreOutboxStore { }` (or equivalent forwarding) so existing subclass registrations still compile. Confirm `IEntityEventTracker.AddEntity(entity)` (single-arg) remains and defaults to the default datastore (already done in Task 4).

- [ ] **Step 1: Write the failing test** — assert `typeof(EFCoreOutboxStore<>)` exists and carries `[Obsolete]`; a `EFCoreOutboxStore<SomeContext>` instance still functions as an `IOutboxStore` (delegates to the datastore-parametric base).

- [ ] **Step 2: Run — verify FAIL.**

- [ ] **Step 3: Implement** the obsolete generic shim.

- [ ] **Step 4: Run — verify PASS**; `dotnet build Src/RCommon.sln` — confirm the `[Obsolete]` produces a warning (not error) and the build stays green.

- [ ] **Step 5: Commit**
```bash
git add Src/RCommon.EfCore/Outbox/ Tests/RCommon.EfCore.Tests/
git commit -m "feat(outbox): obsolete EFCoreOutboxStore<TContext> shim for back-compat (AC-17)"
```

---

### Task 11: Cross-datastore atomicity proof (Podman/Postgres integration test)

**Files:**
- Create: `Tests/RCommon.IntegrationTests/CrossDataStoreOutboxTests.cs`
- (Reuse) Phase 0 fixtures: `Fixtures/PostgreSqlFixture.cs`, `Fixtures/IntegrationCollections.cs`

**Design (AC-7, the headline B4 fix):** Register **two** EF datastores against Postgres containers (or two schemas/databases on one Postgres container — prefer two `RCommonDbContext`s on distinct databases/schemas to model true multi-datastore), both owning an outbox. Persist an aggregate to the **non-default** datastore that raises an integration event, commit through the RCommon `UnitOfWork`, and assert: the event's `__OutboxMessages` row exists in **that** datastore and **not** in the other; the row + business state are in the same transaction. Then a rollback case: throw before commit and assert neither the business row nor any outbox row persists in either datastore.

- [ ] **Step 1: Write the failing test** (`CrossDataStoreOutboxTests`, class-level `[Trait("Category", "Integration")]`, `[Collection(PostgreSqlCollection.Name)]`). Two datastores `"Orders"` (default) and `"Billing"`; a `Billing`-owned aggregate raises an event on commit.
  - Assert: `Billing.__OutboxMessages` count == 1; `Orders.__OutboxMessages` count == 0.
  - Rollback variant: dispose UoW without commit ⇒ both business and outbox counts == 0 in both.

- [ ] **Step 2: Run it — verify it FAILS first for the right reason** (before Tasks 1–9 land it would mis-route to the default datastore; run at the end of the phase it should PASS). Run with the Phase 0 env vars:
```powershell
$env:DOCKER_HOST = "npipe://./pipe/podman-machine-default"; $env:TESTCONTAINERS_RYUK_DISABLED = "true"; dotnet test Tests/RCommon.IntegrationTests --filter "FullyQualifiedName~CrossDataStoreOutboxTests"
```

- [ ] **Step 3: Make it pass** — this is the capstone; if it fails, the defect is in Tasks 4–7/9 wiring, not the test. Fix the pipeline, not the assertion.

- [ ] **Step 4: Commit**
```bash
git add Tests/RCommon.IntegrationTests/CrossDataStoreOutboxTests.cs
git commit -m "test(integration): prove cross-datastore outbox atomicity (AC-7)"
```

---

## Exit criteria (Phase 1 done when)

- [ ] An event raised by an aggregate in a non-default datastore is written to **that** datastore's `__OutboxMessages` in the same transaction, never another datastore's (AC-7) — proven by `CrossDataStoreOutboxTests` on Podman/Postgres.
- [ ] `IEntityEventTracker.AddEntity` captures a datastore name; events are grouped by datastore; `IOutboxStore` is datastore-parametric; one `EFCoreOutboxStore` resolves the correct context per call (AC-8).
- [ ] `OutboxProcessingService` claims/drains from every registered outbox datastore (AC-9).
- [ ] Each outbox row records target producer(s); the poller relays each row only to its recorded producer(s), with a null ⇒ resolve-all back-compat fallback (AC-10).
- [ ] Registering an outbox on a datastore auto-maps `OutboxMessage` on that datastore's model; a startup diagnostic fails loud if a registered outbox datastore's mapping/table is missing (AC-11).
- [ ] Back-compat shims in place: `AddEntity(entity)` overload, `[Obsolete] EFCoreOutboxStore<TContext>` (AC-17).
- [ ] `dotnet build Src/RCommon.sln` is clean; the fast test lane (`--filter "Category!=Integration"`) is green; the integration lane passes on Podman.

## Notes for the executor

- **TDD is mandatory** (@superpowers:test-driven-development): red → green → refactor for every behavioral change. The interface-break task (Task 3) is the one place where "red" is a compile failure across providers — that's expected; keep the commit whole so the tree builds.
- **Confirm EF Core 10 / Npgsql APIs via context7** if a model/mapping call is uncertain (auto-map in `OnModelCreating`, `FindEntityType`).
- **Do not** implement Phase 2+ concerns (FIFO/pre-commit reorder, fluent verbs, broker wrappers). If a task tempts you toward them, stop and note it — this phase is strictly the datastore-aware pipeline.
- **Keep the other providers (Dapper, Linq2Db) compiling** through the interface change even though the behavioral tests focus on EF Core.
