# Event Handling 3.2.0 — Phase 3a: Route + Durability Model, In-Process `Publish`/`.UseOutbox`, Route-Driven Pipeline Split — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a **durability dimension** on the event route model so an event's destination *and* durability are declared at the composition root, add the in-process-bus fluent verb `Publish<T>()` with the durability modifiers `.UseOutbox("store")` (per-event) and `UseRCommonOutbox("store")` (builder-level), and wire durability into the reordered commit pipeline so a single outbox host **dispatches transient (domain) events pre-commit and persists only durable events to the outbox** — completing the route-driven domain-vs-durable split that Phase 2 deliberately deferred (AC-2), and delivering the in-process slice of AC-12.

**Architecture:** Today `EventSubscriptionManager` maps event→producer but carries **no durability information**, and the `OutboxEntityEventTracker` persists **every** harvested entity event to the outbox (its `DispatchDomainEventsAsync` is a Phase-2 no-op). This phase adds a new singleton `IEventRoutingRegistry` recording, per event type, whether it is **durable** (routed through RCommon's per-datastore outbox, with the target store name) or **transient** (in-process, pre-commit). `Publish<T>()` on the in-memory bus builder auto-registers the in-process producer, records the fan-out subscription, and returns a chainable route handle whose `.UseOutbox("store")` marks the event durable; `UseRCommonOutbox("store")` sets a builder-level durable default (per-event `.UseOutbox` overrides it). The `OutboxEntityEventTracker` is reworked to **partition** harvested events by durability: transient events are dispatched pre-commit through the Phase-2 FIFO generation drain (an in-process dispatch path it now composes), while durable events are buffered to the `OutboxEventRouter`, persisted pre-commit, and relayed post-commit. Events a domain handler raises mid-dispatch are routed by the same durability rule — a handler raising a durable integration event lands its outbox row in the same transaction (AC-5, now realized end-to-end in a real outbox host). A startup diagnostic fails loud if a `.UseOutbox`/`UseRCommonOutbox` names a datastore with no registered outbox.

**Scope decision (in-process only — confirmed with the design owner 2026-07-22):** Phase 3 is sliced so Phase 3a covers **in-process transports only**. `Send<T>()` (point-to-point) and all broker verbs (`Publish`/`Send`/`Consume` on MassTransit/Wolverine, `UseBrokerOutbox`) are **out of scope here**: `Send` is meaningless on the in-process bus (it is a mediator/broker concept — 3b/Phase 4), and broker durability coordination is Phase 4. Phase 3a implements `Publish<T>()` + `.UseOutbox`/`UseRCommonOutbox` on the **in-memory bus builder** and the route-driven pipeline. The in-process **mediator** `Publish`/`Send` verbs are Phase 3b.

**Tech Stack:** .NET 10 (Src multi-targets net8/9/10; Tests are net10.0), xUnit 2.9.3, AwesomeAssertions 7.2.1 (`FluentAssertions` namespace), Moq 4.20.72. No new packages.

**Spec:** `docs/specs/event-handling/event-handling.md` — this sub-phase implements **AC-2** (route-based semantics — an event's in-process-vs-durable handling is determined by its configured route(s)), the **in-process slice of AC-12** (fluent API: `Publish<T>()`, `.UseOutbox("store")`, `UseRCommonOutbox("store")`, per-event overrides builder-level, neither ⇒ no outbox), and the **in-process bus part of AC-13** (in-process bus is a first-class destination). **Design:** `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md` §1 (core model — capture paths, routes, durability), §4 (fluent API). **Branch:** `feature/event-handling-outbox-recipes` (Phases 0–2 already on this branch; do not switch branches).

---

## Scope & boundaries

**In scope (AC-2, AC-12 in-process, AC-13 in-process bus):**
- `IEventRoutingRegistry` / `EventRoutingRegistry` singleton: event type → durability (`Transient` | `Durable(storeName)`).
- `Publish<T>()` on the in-memory bus builder (auto-registers `PublishWithEventBusEventProducer`, records the fan-out subscription) returning a chainable route handle.
- `.UseOutbox("store")` per-event durability modifier; `UseRCommonOutbox("store")` builder-level default; per-event overrides builder-level; neither ⇒ transient.
- Route-driven pipeline split in `OutboxEntityEventTracker`: transient → pre-commit FIFO dispatch; durable → outbox persist (pre-commit) → relay (post-commit); mid-dispatch handler-raised events routed by the same rule (AC-5 end-to-end).
- Startup fail-loud diagnostic: a durable route naming a datastore with no registered outbox.

**Explicitly out of scope (later — do NOT build here):**
- `Send<T>()` (point-to-point) anywhere; in-process **mediator** `Publish`/`Send` verbs (**Phase 3b**).
- All broker verbs and `UseBrokerOutbox` (**Phase 4**).
- `AddOutboxProducer`/`AddOutboxProcessor` topology split (**Phase 3c**, AC-21).
- `Consume<T,H>()` + the `AddSubscriber`→`Consume` obsolete alias on broker builders (**Phase 4**, AC-17 for brokers).
- Deserialization allow-list (AC-20), payload protector (AC-19), metrics (AC-18) — later phases.

**Key design decisions (documented so the implementer does not re-derive them):**
- **`AddSubscriber<T,H>()` semantics unchanged:** it registers an in-process handler for `T`; such handlers are transient/domain (pre-commit). `AddSubscriber` does **not** touch the routing registry (it never marks an event durable).
- **Durability is per-event, recorded in `IEventRoutingRegistry`.** An event is *durable* iff some `Publish<T>().UseOutbox(s)` or a builder-level `UseRCommonOutbox(s)` applies to it; otherwise *transient*. This is the single source of truth the pipeline consults to decide pre-commit-dispatch vs outbox-persist.
- **In-process `Publish<T>()` with no durability = transient, pre-commit** (dispatched with domain handlers). For the in-process bus there is no "broker to fire at commit", so a non-durable in-process `Publish` collapses to pre-commit dispatch. (The design's "fire-at-commit" language is broker-oriented; it is realized in Phase 4.) `Publish<T>()` still auto-registers the bus producer so subscribers receive the event.
- **Durable event ⇒ persisted to outbox, relayed post-commit; NOT also dispatched pre-commit** (no double-delivery). Its subscribers are invoked during the post-commit relay via the bus producer. For an entity-sourced durable event the outbox row is written to the **entity's** datastore (Phase-1 co-location, AC-7); the `.UseOutbox("store")` name is used to (a) mark durability and (b) validate the store is a registered outbox — for router-added (transaction-script) events with no entity datastore, the `.UseOutbox` store names the target datastore.
- **`.UseOutbox` store must be a registered outbox datastore** (from `db.AddOutbox(o => o.OnDataStore("X"))`), matched case-insensitively (carrying forward the Phase-1 follow-up convention). A durable route with no matching registered outbox fails loud at startup.

---

## File structure

**Core event-handling (`Src/RCommon.Core/EventHandling/`)**
- Create `Routing/IEventRoutingRegistry.cs` + `Routing/EventRoutingRegistry.cs` (namespace `RCommon.EventHandling.Routing`) — event→durability map (singleton). `MarkDurable(Type eventType, string storeName)`, `bool TryGetOutboxStore(Type eventType, out string? storeName)`, `bool IsDurable(Type eventType)`, `IReadOnlyCollection<string> DurableStoreNames { get; }`.
- Create `IEventRouteHandle.cs` + `EventRouteHandle.cs` (namespace `RCommon.EventHandling`) — the chainable handle returned by `Publish<T>()`; carries the event type + builder services + registry; exposes `IEventRouteHandle UseOutbox(string dataStoreName)`.
- Modify `InMemoryEventBusBuilderExtensions.cs` — add `IEventRouteHandle Publish<TEvent>(this IInMemoryEventBusBuilder builder)` (auto-register `PublishWithEventBusEventProducer` + record subscription, mirroring `AddSubscriber`'s producer wiring) and `IInMemoryEventBusBuilder UseRCommonOutbox(this IInMemoryEventBusBuilder builder, string dataStoreName)` (builder-level default).
- Modify `RCommonBuilder.cs` — register `EventRoutingRegistry` as a singleton (alongside `EventSubscriptionManager` at line ~40).
- Create `Routing/DurableRouteValidationHostedService.cs` (namespace `RCommon.EventHandling.Routing`) OR extend the existing bootstrap diagnostics — startup fail-loud if a durable route's store is not a registered outbox. (Prefer a small dedicated hosted service to avoid coupling core to persistence; it reads `IEventRoutingRegistry.DurableStoreNames` and the outbox datastore registry — see Task 5 for the exact seam and where it is registered, since `IOutboxDataStoreRegistry` lives in `RCommon.Persistence`. This validator therefore lives in `RCommon.Persistence` and is registered by `AddOutbox`, not in core.)

**Persistence (`Src/RCommon.Persistence/`)**
- Modify `Outbox/OutboxEntityEventTracker.cs` — the route-driven split (the core of this phase). Partition harvested + mid-dispatch events by `IEventRoutingRegistry` durability: transient → in-process FIFO dispatch (pre-commit); durable → `OutboxEventRouter` buffer → persist (pre-commit) → relay (post-commit). It must compose an in-process dispatch path (see "Wiring" below).
- Create `Outbox/DurableRouteOutboxValidationHostedService.cs` — the startup validator (reads `IEventRoutingRegistry.DurableStoreNames` + `IOutboxDataStoreRegistry`); registered by `AddOutbox`. Fail loud if a durable store is not registered as an outbox.
- Modify `Outbox/OutboxPersistenceBuilderExtensions.cs` — register the validator hosted service; ensure the in-process dispatch path the tracker needs is available in the outbox host (see Wiring).

**Wiring note (critical — resolve before Task 6):** the outbox host registers `OutboxEventRouter` as `IEventRouter`, so the Phase-2 FIFO drain (which lives in `InMemoryTransactionalEventRouter`) is NOT the resolved `IEventRouter` there. Two viable approaches; the plan uses **(A)**:
- **(A) Compose an explicit in-process dispatcher in the outbox tracker.** Register `InMemoryTransactionalEventRouter` as a concrete scoped type (in addition to `OutboxEventRouter` as `IEventRouter`) and inject it into `OutboxEntityEventTracker` as the transient dispatcher. The tracker seeds transient events into it and drains; buffers durable events into the `OutboxEventRouter`. This keeps one FIFO drain implementation (no duplication) and a clean separation: `InMemoryTransactionalEventRouter` = transient in-process dispatch, `OutboxEventRouter` = durable persist/relay.
- (B) Fold the drain into `OutboxEventRouter`. Rejected — duplicates the generation/cycle-breaker logic and muddies the router's single responsibility.

**Tests**
- Create `Tests/RCommon.Core.Tests/EventRoutingRegistryTests.cs`.
- Create `Tests/RCommon.Core.Tests/InMemoryEventBusPublishTests.cs` — `Publish<T>()` auto-registers producer + records subscription; `.UseOutbox`/`UseRCommonOutbox` mark durability; per-event overrides builder-level.
- Create `Tests/RCommon.Persistence.Tests/DurableRouteOutboxValidationTests.cs` — durable route with unregistered store fails loud; registered store passes.
- Create `Tests/RCommon.Persistence.Tests/RouteDrivenOutboxPipelineTests.cs` — the big one: transient event dispatched pre-commit (not persisted); durable event persisted (not dispatched pre-commit) + relayed; a domain handler raising a durable event mid-dispatch persists its row (AC-5); an event with both a subscriber and a durable route (AC-2). Plain unit/integration-of-units — NO `[Trait("Category","Integration")]`.
- Modify existing `OutboxEntityEventTrackerTests.cs` as the split changes its behavior.

> **CI trait convention (unchanged):** every Phase-3a test is a fast-lane unit test. Do NOT add `[Trait("Category","Integration")]`; none start containers.

> **Interface-break discipline:** if any change touches `IEventRouter`/`IEntityEventTracker`/`OutboxEntityEventTracker`'s constructor, update all implementers/test doubles in the same commit and verify `dotnet build Src/RCommon.sln` before committing.

---

## Recommended task order

1. `IEventRoutingRegistry` + `EventRoutingRegistry` (+ singleton registration).
2. `Publish<T>()` + `IEventRouteHandle`/`.UseOutbox` on the in-memory bus builder.
3. `UseRCommonOutbox("store")` builder-level default + precedence.
4. `DurableRouteOutboxValidationHostedService` (startup fail-loud) + registration.
5. Compose the in-process dispatcher into the outbox host (Wiring approach A) — registration + `OutboxEntityEventTracker` constructor, no behavior change yet (keeps build green).
6. Route-driven pipeline split in `OutboxEntityEventTracker` (transient pre-commit dispatch; durable persist/relay; mid-dispatch routing) — AC-2, AC-5.
7. Holistic recipe-1-style pipeline proof (transient domain + durable integration, atomic rollback).

---

## Task 1: `IEventRoutingRegistry` + `EventRoutingRegistry`

**Files:**
- Create: `Src/RCommon.Core/EventHandling/Routing/IEventRoutingRegistry.cs`, `Src/RCommon.Core/EventHandling/Routing/EventRoutingRegistry.cs`
- Modify: `Src/RCommon.Core/RCommonBuilder.cs` (register singleton)
- Test: `Tests/RCommon.Core.Tests/EventRoutingRegistryTests.cs`

**Contract:**
```csharp
namespace RCommon.EventHandling.Routing
{
    public interface IEventRoutingRegistry
    {
        void MarkDurable(Type eventType, string dataStoreName);
        bool IsDurable(Type eventType);
        bool TryGetOutboxStore(Type eventType, out string? dataStoreName);
        IReadOnlyCollection<string> DurableStoreNames { get; }
    }
}
```
- Implementation: a `ConcurrentDictionary<Type, string>` (event type → store name). `MarkDurable` records/overwrites (later registration wins — matches "per-event overrides builder-level" when the builder default is applied first; see Task 3). Guard null/whitespace store name. `DurableStoreNames` = distinct values.
- **Registration MUST be instance-based** (Critical): register `Services.AddSingleton<IEventRoutingRegistry>(new EventRoutingRegistry())` in `RCommonBuilder`, exactly mirroring `Services.AddSingleton(new EventSubscriptionManager())` at `RCommonBuilder.cs:40`. This is required because the config-time lookup helper (`GetRoutingRegistry`, Task 2) reads `descriptor.ImplementationInstance` — a type-based `AddSingleton<IEventRoutingRegistry, EventRoutingRegistry>()` would leave `ImplementationInstance` null and make `Publish<T>().UseOutbox(...)` silently no-op at config time. Register it next to `EventSubscriptionManager`.

- [ ] **Step 1: failing tests** — default empty; `MarkDurable` then `IsDurable`/`TryGetOutboxStore` return the store; `DurableStoreNames` lists distinct stores; re-marking overwrites; null/blank store throws.
- [ ] **Step 2:** run → fail (type missing).
- [ ] **Step 3:** implement the interface + class + singleton registration.
- [ ] **Step 4:** run → pass; `dotnet build Src/RCommon.Core/RCommon.Core.csproj` clean.
- [ ] **Step 5: commit** `feat(events): add IEventRoutingRegistry durability map (AC-2)`

---

## Task 2: `Publish<T>()` + `.UseOutbox` on the in-memory bus builder

**Files:**
- Create: `Src/RCommon.Core/EventHandling/IEventRouteHandle.cs`, `Src/RCommon.Core/EventHandling/EventRouteHandle.cs`
- Modify: `Src/RCommon.Core/EventHandling/InMemoryEventBusBuilderExtensions.cs`
- Test: `Tests/RCommon.Core.Tests/InMemoryEventBusPublishTests.cs`

**Design:**
- `IEventRouteHandle { IEventRouteHandle UseOutbox(string dataStoreName); }`.
- `EventRouteHandle` holds the `Type eventType`, the `IEventRoutingRegistry`, and (for builder-level precedence) a flag; `UseOutbox(store)` calls `_registry.MarkDurable(eventType, store)` and returns `this`.
- `Publish<TEvent>(this IInMemoryEventBusBuilder builder)`: (a) auto-register `PublishWithEventBusEventProducer` if not present + record the producer for the builder (mirror `AddSubscriber`'s existing producer wiring at `InMemoryEventBusBuilderExtensions.cs:28-44`); (b) record the fan-out subscription via `EventSubscriptionManager.AddSubscription(builder.GetType(), typeof(TEvent))`; (c) resolve `IEventRoutingRegistry` from the builder's services (config-time singleton instance lookup, mirroring `GetSubscriptionManager`); (d) return a new `EventRouteHandle`. If a builder-level default store was set (Task 3), apply it now so `.UseOutbox` can override.
- Add a `GetRoutingRegistry(this IServiceCollection)` helper mirroring `GetSubscriptionManager` (which reads the registered singleton instance at config time).

- [ ] **Step 1: failing tests** — `Publish<T>()` registers exactly one `PublishWithEventBusEventProducer` (idempotent across calls) and records the subscription (assert via `EventSubscriptionManager`); `Publish<T>()` alone leaves the event **transient** (`IEventRoutingRegistry.IsDurable` false); `Publish<T>().UseOutbox("Orders")` marks it durable with store "Orders".
- [ ] **Step 2:** run → fail.
- [ ] **Step 3:** implement handle + `Publish` + helper.
- [ ] **Step 4:** run → pass; full `RCommon.Core.Tests` green.
- [ ] **Step 5: commit** `feat(events): add in-memory Publish<T>() + .UseOutbox route handle (AC-12)`

---

## Task 3: `UseRCommonOutbox("store")` builder-level default + precedence

**Files:**
- Modify: `Src/RCommon.Core/EventHandling/InMemoryEventBusBuilderExtensions.cs` (+ a small builder-scoped state holder if needed)
- Test: add to `Tests/RCommon.Core.Tests/InMemoryEventBusPublishTests.cs`

**Design (order-independent — do NOT rely on declaration order):**
- Add a small config-time state holder tracking, per builder type: the builder-level default store (nullable) and the set of event types `Publish`'d on that builder that were **explicitly** given a per-event `.UseOutbox` (so the default never clobbers an explicit choice). Store it on the routing registry (add internal helpers) or a companion config-time singleton looked up like `GetRoutingRegistry`.
- `UseRCommonOutbox(builder, store)`: (1) set the builder default; (2) **retroactively** `MarkDurable(t, store)` for every event type already `Publish`'d on this builder that was NOT explicitly `.UseOutbox`'d.
- `Publish<TEvent>()`: after registering the producer/subscription, if a builder default is already set and this event has no explicit `.UseOutbox`, `MarkDurable(T, default)`. Record `T` as published-on-this-builder.
- `.UseOutbox(store)` on the handle: `MarkDurable(T, store)` **and** record `T` as explicitly-outboxed on this builder (so a later `UseRCommonOutbox` will not overwrite it).
- **Precedence (guaranteed regardless of statement order):** an explicit per-event `.UseOutbox(x)` always wins over the builder-level `UseRCommonOutbox(y)`, whether the builder-level call comes before or after the `Publish`. A `Publish` with neither ⇒ transient.
- This makes the three orderings all correct: (default-then-publish), (publish-then-default), and (default + per-event override in any order). Pin all three with tests.

- [ ] **Step 1: failing tests** — cover ALL orderings: (i) `UseRCommonOutbox("Orders")` then `Publish<T>()` ⇒ durable to "Orders"; (ii) `Publish<T>()` then `UseRCommonOutbox("Orders")` ⇒ durable to "Orders" (retroactive); (iii) `UseRCommonOutbox("Orders")` then `Publish<T>().UseOutbox("Billing")` ⇒ durable to "Billing" (per-event wins); (iv) `Publish<T>().UseOutbox("Billing")` then `UseRCommonOutbox("Orders")` ⇒ durable to "Billing" (explicit not clobbered); (v) `Publish<T>()` with no default and no `.UseOutbox` ⇒ transient.
- [ ] **Step 2:** run → fail.
- [ ] **Step 3:** implement.
- [ ] **Step 4:** run → pass.
- [ ] **Step 5: commit** `feat(events): add UseRCommonOutbox builder default with per-event precedence (AC-12)`

---

## Task 4: `DurableRouteOutboxValidationHostedService` (startup fail-loud)

**Files:**
- Create: `Src/RCommon.Persistence/Outbox/DurableRouteOutboxValidationHostedService.cs`
- Modify: `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs` (register it)
- Test: `Tests/RCommon.Persistence.Tests/DurableRouteOutboxValidationTests.cs`

**Design:** an `IHostedService` whose `StartAsync` compares `IEventRoutingRegistry.DurableStoreNames` against the registered outbox datastores (`IOutboxDataStoreRegistry.Registrations`, from Phase 1), case-insensitively. If a durable route names a store with no registered outbox, throw a descriptive exception (fail loud). Model it on the existing Phase-1 fail-loud hosted service **`Src/RCommon.EfCore/Outbox/OutboxSchemaVerificationHostedService.cs`** (note: that one lives in `RCommon.EfCore`, is `internal sealed`, and resolves services via `IServiceProvider` + `CreateScope()` in `StartAsync`). Your new validator belongs in **`RCommon.Persistence`** because both its dependencies — `IEventRoutingRegistry` (core) and `IOutboxDataStoreRegistry` (persistence) — are available there; it reads `IOutboxDataStoreRegistry.Registrations`. Register via `TryAddEnumerable`/idempotent add in `AddOutbox` (mirroring the Phase-1 diagnostic registration so multi-datastore `AddOutbox` yields one validator).

- [ ] **Step 1: failing tests** — durable route "Ghost" with only "Orders" registered ⇒ `StartAsync` throws with a message naming "Ghost"; durable route "Orders" with "Orders" registered ⇒ no throw; case-insensitive match ("orders" vs "Orders") passes.
- [ ] **Step 2:** run → fail.
- [ ] **Step 3:** implement + register.
- [ ] **Step 4:** run → pass; full persistence project green.
- [ ] **Step 5: commit** `feat(outbox): fail loud when a durable route names an unregistered outbox datastore (AC-2)`

---

## Task 5: Compose the in-process dispatcher into the outbox host (wiring only, no behavior change)

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxPersistenceBuilderExtensions.cs` (register `InMemoryTransactionalEventRouter` as a concrete scoped type in the outbox host; register `IEventRoutingRegistry` resolvable — it is a core singleton, already present when core event handling is configured)
- Modify: `Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs` (constructor gains the in-process dispatcher + `IEventRoutingRegistry`; NO behavior change yet — `DispatchDomainEventsAsync` still no-op, `PersistEventsAsync` still persists all — so this task is a safe, green refactor that sets up Task 6)
- Test: update `OutboxEntityEventTrackerTests.cs` constructor usages

**Design:** Register `services.TryAddScoped<InMemoryTransactionalEventRouter>()` in `AddOutbox` (the **concrete** type). Inject **both routers as concrete types** into `OutboxEntityEventTracker`: it already takes the concrete `OutboxEventRouter` (durable persist/relay — see its current ctor `(InMemoryEntityEventTracker inner, OutboxEventRouter outboxRouter)`); add the concrete `InMemoryTransactionalEventRouter` (transient dispatcher) + `IEventRoutingRegistry`. Injecting concrete types side-steps the ambiguity of what `IEventRouter` resolves to in the outbox host — the tracker never depends on the `IEventRouter` alias for either role. Keep all method bodies exactly as they are (Task 6 changes behavior). This isolates the risky wiring change (ctor break + DI) from the behavior change, each independently green.

> **Do NOT rely on the inner `InMemoryEntityEventTracker`'s `IEventRouter` for transient dispatch.** In the outbox host the inner tracker's injected `IEventRouter` is NOT the transient dispatcher (it is whatever `IEventRouter` resolves to — likely `OutboxEventRouter`). Task 6 uses the tracker's OWN composed `InMemoryTransactionalEventRouter` concrete instance for transient dispatch; it does not delegate to `_inner.DispatchDomainEventsAsync`.

> **DI-resolution assertion (add to Step 4):** write a small test that builds a real provider for an outbox-configured host and asserts: (a) `InMemoryTransactionalEventRouter` resolves as its own concrete scoped type, distinct instance from the resolved `IEventRouter`; (b) `OutboxEntityEventTracker` resolves with both routers injected. This pins the wiring the plan depends on and surfaces the actual `IEventRouter` backing type.

> **Guard:** `InMemoryTransactionalEventRouter` needs `IOptions<EventHandlingOptions>`, `IServiceProvider`, `ILogger<>`, `EventSubscriptionManager` — all registered when core event handling is configured (it always is; event handling is configured via `WithEventHandling`). If DI resolution is uncertain, construct via `ActivatorUtilities` inside the tracker — but prefer constructor injection.

- [ ] **Step 1:** update `OutboxEntityEventTracker` constructor + all its test constructions (RED = compile).
- [ ] **Step 2:** run persistence tests → fail (compile).
- [ ] **Step 3:** add registration + constructor params; bodies unchanged.
- [ ] **Step 4:** `dotnet build Src/RCommon.sln` clean; full persistence project green (behavior identical).
- [ ] **Step 5: commit** `refactor(outbox): inject in-process dispatcher + routing registry into OutboxEntityEventTracker (wiring for AC-2)`

---

## Task 6: Route-driven pipeline split in `OutboxEntityEventTracker` (AC-2, AC-5)

**Files:**
- Modify: `Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs`
- Test: `Tests/RCommon.Persistence.Tests/RouteDrivenOutboxPipelineTests.cs` (+ update `OutboxEntityEventTrackerTests.cs`)

**New behavior (partition by durability):**
- Implement seed+subscribe+drain **from scratch** in `OutboxEntityEventTracker.DispatchDomainEventsAsync` (do NOT delegate to `_inner.DispatchDomainEventsAsync` — that would drain through the inner tracker's `IEventRouter`, not the composed transient dispatcher). Reuse the exact seed/subscribe/`finally`-unsubscribe pattern from `InMemoryEntityEventTracker.DispatchDomainEventsAsync` (traverse each tracked entity graph's `BusinessEntity`s, subscribe `TransactionalEventAdded`, seed existing `LocalEvents`), but the enqueue callback routes by durability instead of unconditionally enqueuing to one router.
- `DispatchDomainEventsAsync` (pre-commit): subscribe each tracked graph `BusinessEntity`'s `TransactionalEventAdded` to a router-by-durability callback; seed each existing `LocalEvent` by the same rule; drain the **in-process** dispatcher (`_inProcessRouter.RouteEventsAsync()`, the composed `InMemoryTransactionalEventRouter`) so transient events are dispatched pre-commit, ordered, with the cycle-breaker. Unsubscribe in `finally`. Route-by-durability rule:
  - `_routingRegistry.TryGetOutboxStore(evt.GetType(), out var store)` true ⇒ **durable**: `_outboxRouter.AddTransactionalEvent(evt, store ?? entityDataStoreName)` (buffer for outbox; for entity-sourced events prefer the entity's datastore for co-location — see decision below).
  - else ⇒ **transient**: `_inProcessRouter.AddTransactionalEvent(evt)` (FIFO seed/enqueue for pre-commit dispatch).
- `PersistEventsAsync` (pre-commit): `_outboxRouter.PersistBufferedEventsAsync()` — persists exactly the durable events buffered during dispatch (including mid-dispatch handler-raised durable events — **AC-5**).
- `EmitTransactionalEventsAsync` (post-commit): `_outboxRouter.RouteEventsAsync()` — relay durable events (unchanged).

**Datastore-for-durable decision (co-location wins, mismatch fails loud):** for an **entity-sourced** durable event, persist to the **entity's** datastore (`dataStoreName` from `TrackedEntitiesWithDataStore`) to preserve Phase-1 co-location/atomicity (AC-7). The `.UseOutbox("store")` name is NOT silently ignored: at buffer/persist time, if the routing registry's store for the event (resolved via `TryGetOutboxStore`) is non-null and does **not** match the entity's resolved datastore (case-insensitive), **throw a descriptive exception (fail loud)** — this catches the `Publish<OrderConfirmed>().UseOutbox("Billing")` on an Orders-tracked aggregate misconfiguration that the startup validator (Task 4) cannot detect (it only checks the named store is *a* registered outbox, not that it matches the entity). For a **router-added** durable event (no entity datastore), the store from the routing registry is authoritative. Document this; do not attempt cross-datastore reconciliation.

**No double-dispatch:** a durable event is buffered to the outbox and NOT seeded into the in-process FIFO, so it is never dispatched pre-commit — only relayed post-commit. A transient event is only dispatched pre-commit. An event that is *both* (has a durable route AND `AddSubscriber` handlers): it is durable, so it is persisted and relayed post-commit; its subscribers run during relay via the bus producer (AC-2 "handled both ways" — the subscriber still receives it, durably). Assert this explicitly.

- [ ] **Step 1: failing tests** (`RouteDrivenOutboxPipelineTests.cs`):
  - (a) A transient entity event is dispatched pre-commit (in-process handler fires) and **no** outbox row is persisted.
  - (b) A durable entity event (`.UseOutbox`) is persisted to the outbox and **not** dispatched pre-commit; it is relayed post-commit (handler fires during relay).
  - (c) A domain handler that raises a durable integration event mid-dispatch (via `AddTransactionalEvent` or an aggregate that raises a `.UseOutbox`'d event) results in that outbox row persisted in the same transaction (**AC-5**).
  - (d) An event registered with BOTH `Publish<T>().UseOutbox("Orders")` AND `AddSubscriber<T,H>()` (the AC-2 "handled both ways" case): persisted+relayed once, subscriber fires during relay, NOT double-delivered and NOT also dispatched pre-commit. (This depends on `Publish`+`AddSubscriber` producer/subscription idempotency — assert exactly one persisted row and one handler invocation.)
  - (e) A durable event whose `.UseOutbox` store does NOT match the tracked entity's datastore ⇒ `DispatchDomainEventsAsync`/`PersistEventsAsync` throws the co-location fail-loud exception.
  - Use real `InMemoryTransactionalEventRouter` + a mock/fake `OutboxEventRouter`/`IOutboxStore` recording persisted rows (follow existing `OutboxEntityEventTrackerTests` fakes).
- [ ] **Step 2:** run → fail (current no-op/persist-all behavior).
- [ ] **Step 3:** implement the partition.
- [ ] **Step 4:** run → pass; full persistence project green (Phase-1 cross-datastore behavior for durable events preserved; update `OutboxEntityEventTrackerTests` as needed but do NOT weaken).
- [ ] **Step 5: commit** `feat(outbox)!: route-driven split — dispatch transient pre-commit, persist durable (AC-2, AC-5)`

---

## Task 7: Holistic recipe-1-style pipeline proof

**Files:**
- Create: `Tests/RCommon.Persistence.Tests/RouteDrivenRecipePipelineTests.cs`

Prove the recipe-1 wiring end-to-end through real units (real bus, real in-process router, real routing registry, mock/fake outbox store): an aggregate raises a domain event handled by an `AddSubscriber` handler **pre-commit**, and a `Publish<OrderConfirmed>().UseOutbox("Orders")` integration event is **persisted** (not dispatched pre-commit) and **relayed post-commit**; a pre-commit handler throwing rolls back so neither the domain side effect nor the outbox row survives (compose with a real `UnitOfWork` + `TransactionScope` if practical, else assert at the tracker level as Phase-2 Task 6 did). Plain unit test, fast lane.

- [ ] Steps 1–5 as usual; commit `test(events): holistic route-driven recipe-1 pipeline proof (AC-2, AC-12)`.

---

## Phase-completion checks

- [ ] Full fast-lane suite green: `dotnet test Src/RCommon.sln --filter "Category!=Integration"`.
- [ ] Solution builds all TFMs: `dotnet build Src/RCommon.sln -c Release`.
- [ ] Phase-1 cross-datastore integration test still green on Podman (the durable path is exercised by real Postgres):
  `$env:DOCKER_HOST = "npipe://./pipe/podman-machine-default"; $env:TESTCONTAINERS_RYUK_DISABLED = "true"; dotnet test Tests/RCommon.IntegrationTests --filter "FullyQualifiedName~CrossDataStoreOutboxTests"`.
- [ ] Final whole-branch code review (superpowers:code-reviewer) of the Phase-3a diff — special attention to the route-driven split (no double-delivery; AC-5 mid-dispatch; datastore co-location) and the `Publish`/`.UseOutbox`/`UseRCommonOutbox` precedence.
- [ ] No Phase-3a test carries `[Trait("Category","Integration")]`.

## Notes for later phases (captured, not built here)
- Phase 3b: `Publish`/`Send` on mediator builders reuse `IEventRoutingRegistry` + the same `.UseOutbox` handle; `Send` introduced there (point-to-point). The `EventRouteHandle` should be transport-agnostic enough to reuse.
- Phase 4: broker `Publish`/`Send`/`Consume` + `UseBrokerOutbox` (a *different* durability mechanism than `IEventRoutingRegistry` — broker-native). Keep `UseBrokerOutbox` out of `IEventRoutingRegistry` (that registry is RCommon-outbox-only).
- The non-durable in-process `Publish` = pre-commit dispatch decision may be revisited if a post-commit in-process "fire-at-commit" is ever wanted; currently intentionally collapsed to pre-commit.
