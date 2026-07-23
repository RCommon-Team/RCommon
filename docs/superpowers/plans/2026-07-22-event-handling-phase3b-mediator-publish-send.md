# Event Handling 3.2.0 — Phase 3b: In-Process Mediator `Publish`/`Send` Verbs — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the in-process **mediator** (MediatR) a first-class delivery destination for the 3.2.0 fluent API by adding `Publish<T>()` (fan-out) and `Send<T>()` (point-to-point) verbs on `IMediatREventHandlingBuilder`, plus the builder-level `UseRCommonOutbox("store")` default — all reusing the Phase-3a durability model (`IEventRoutingRegistry` + `EventRouteHandle` + `.UseOutbox`). This delivers the mediator slice of AC-12/AC-13: `Publish`/`Send` apply to the mediator exactly as to the in-process bus, a mediator route may be made durable via RCommon's per-datastore outbox (`.UseOutbox`/`UseRCommonOutbox`), and `UseBrokerOutbox` does not apply (no mediator-native outbox).

**Architecture:** The mediator producers already exist (`PublishWithMediatREventProducer` → `IMediatorService.Publish`, `SendWithMediatREventProducer` → `IMediatorService.Send`) but are only wired manually via `AddProducer<T>()`. Phase 3a's `Publish<T>()`/`UseRCommonOutbox` on the in-memory bus record durability through **internal** `RCommon.Core` members (`EventRoutingRegistry.GetOrCreateBuilderState`, `BuilderOutboxState`, the internal `EventRouteHandle` ctor). Since mediator extensions live in `RCommon.Mediatr` (a separate assembly), Phase 3b first **extracts a public route-recording helper** in `RCommon.Core` that encapsulates "register-publish-route + apply-builder-default → return handle" and "set-builder-default (retroactive)", then refactors the bus verbs to use it (pure refactor, bus tests stay green), and finally adds the mediator `Publish`/`Send`/`UseRCommonOutbox` verbs on top. The Phase-3a route-driven pipeline (transient → pre-commit dispatch; durable → outbox persist/relay; `TargetProducers` recorded from the subscription map) is **reused unchanged** — a durable mediator event persists to the outbox and is relayed post-commit to the mediator producer, purely because the mediator producer is recorded as the event's target in `EventSubscriptionManager`. Phase 3b is therefore additive verbs + one enabling refactor; it changes no pipeline behavior.

**Scope decision (confirmed 2026-07-22):** in-process only. This phase adds mediator `Publish`/`Send`/`UseRCommonOutbox`. It does NOT build any broker verb, `UseBrokerOutbox`, `Consume`, or the MediatR/native-mediator **CQRS request pipeline** (command/query handling, `AddUnitOfWorkToRequestPipeline`) — the latter is explicitly out of scope per spec MN-6; this phase treats the mediator only as an event-handling (producer/subscriber) transport. `Send<T>()` here is the **outbound delivery verb** that registers the existing `SendWithMediatREventProducer`; the consuming side of a mediator route remains `AddSubscriber<T,H>()` (notification handlers), unchanged.

**Tech Stack:** .NET 10 (Src multi-targets net8/9/10; Tests net10.0), xUnit 2.9.3, AwesomeAssertions 7.2.1 (`FluentAssertions`), Moq 4.20.72, MediatR (already referenced by `RCommon.Mediatr`). No new packages.

**Spec:** `docs/specs/event-handling/event-handling.md` — **AC-12** (mediator gets `Publish`/`Send` + `.UseOutbox`/`UseRCommonOutbox`; per-event overrides builder-level; neither ⇒ transient) and **AC-13** (in-process mediator is a first-class destination; `Publish`/`Send` apply; `UseBrokerOutbox` does not). **Design:** `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md` §1 (in-process mediator note) + §4 (fluent API — the `Publish`/`Send` transport-agnostic paragraph). **Branch:** `feature/event-handling-outbox-recipes` (Phases 0–3a on this branch; do not switch).

---

## Scope & boundaries

**In scope (AC-12 mediator, AC-13 mediator):**
- Extract a public, builder-agnostic route-recording helper in `RCommon.Core` (enabling reuse from `RCommon.Mediatr` and, later, broker assemblies).
- Refactor the in-memory bus `Publish`/`UseRCommonOutbox` to use the helper (pure refactor, no behavior change).
- `Publish<TEvent>()` and `Send<TEvent>()` on `IMediatREventHandlingBuilder` (auto-register `PublishWithMediatREventProducer`/`SendWithMediatREventProducer`, record the subscription, return an `IEventRouteHandle` supporting `.UseOutbox`).
- `UseRCommonOutbox("store")` on `IMediatREventHandlingBuilder` (builder-level durable default, order-independent precedence — same semantics as the bus).

**Explicitly out of scope (later — do NOT build here):**
- Broker `Publish`/`Send`/`Consume`, `UseBrokerOutbox` (**Phase 4**).
- `AddOutboxProducer`/`AddOutboxProcessor` topology split (**Phase 3c**, AC-21).
- The CQRS request pipeline / request handlers / `AddUnitOfWorkToRequestPipeline` (**out of scope entirely**, MN-6). `Send<T>()` here only registers the outbound producer; it does not wire a request-handler pipeline.
- Any change to the Phase-3a route-driven pipeline, `IEventRoutingRegistry`, or the trackers/routers (reused as-is).

**Key decisions (documented):**
- **No `InternalsVisibleTo`.** Reuse is via a public helper, not by exposing Core internals to transport assemblies. The helper keeps `BuilderOutboxState`/`EventRouteHandle`-ctor internal to Core.
- **Mediator consuming stays `AddSubscriber`.** `Publish`/`Send` are outbound; `AddSubscriber<T,H>()` (already present, registers the MediatR notification handler) is the in-process handler registration. `Consume` is broker-only (Phase 4).
- **Durable mediator routes reuse the existing pipeline.** No mediator-specific outbox code. A durable mediator event is persisted and relayed post-commit to the mediator producer because `EventSubscriptionManager` maps the event to that producer (recorded by `Publish`/`Send`). Prove this with a routing/target-recording test, not new pipeline code.

---

## File structure

**Core (`Src/RCommon.Core/EventHandling/`)**
- Create `Routing/EventRouteRegistrationExtensions.cs` (namespace `RCommon.EventHandling.Routing` or `RCommon.EventHandling`) — public helpers, builder-agnostic:
  - `IEventRouteHandle RecordPublishRoute(this IServiceCollection services, Type builderType, Type eventType)` — resolves the routing registry, records the event as published-on-builder, applies any already-set builder default (order-independent), returns an `IEventRouteHandle` wired to the per-builder state. (This is exactly the durability-recording half of the bus `Publish<T>()`, minus the producer/subscription registration which is builder-specific.)
  - `void ApplyBuilderOutboxDefault(this IServiceCollection services, Type builderType, string dataStoreName)` — the `UseRCommonOutbox` retroactive-marking logic (guard args; set default; retroactively mark non-explicit published events).
- Modify `EventHandling/InMemoryEventBusBuilderExtensions.cs` — refactor `Publish<T>()` and `UseRCommonOutbox` to delegate their durability-recording to the two helpers (keep the producer/subscription registration in place). No behavior change.
- Possibly widen the internal `EventRouteHandle` ctor / `EventRoutingRegistry.GetOrCreateBuilderState` visibility to `internal` (already) — the helper lives in Core so it can use them; no cross-assembly exposure.

**MediatR (`Src/RCommon.Mediatr/`)**
- Modify `MediatREventHandlingBuilderExtensions.cs` — add `Publish<TEvent>()`, `Send<TEvent>()`, `UseRCommonOutbox(...)` on `IMediatREventHandlingBuilder`, each registering the appropriate producer + subscription and delegating durability to the Core helpers. Confirm the producer type names/namespaces: `PublishWithMediatREventProducer`, `SendWithMediatREventProducer` (under `Src/RCommon.Mediatr/Producers/`).

**Tests**
- Modify `Tests/RCommon.Core.Tests/InMemoryEventBusPublishTests.cs` / `InMemoryEventBusUseRCommonOutboxTests.cs` — must stay green after the refactor (they pin the extracted behavior). Add a direct test of the new helpers if useful.
- Create `Tests/RCommon.Mediatr.Tests/MediatREventHandlingPublishSendTests.cs` — the mediator verbs.

> **CI trait convention (unchanged):** all fast-lane unit tests; no `[Trait("Category","Integration")]`.

---

## Recommended task order

1. Extract the public route-recording helpers in Core + refactor the bus verbs to use them (pure refactor; bus tests stay green).
2. Mediator `Publish<TEvent>()`.
3. Mediator `Send<TEvent>()`.
4. Mediator `UseRCommonOutbox("store")`.
5. Mediator durability + target-producer routing proof (durable mediator event records the mediator producer as target; transient by default; precedence).

---

## Task 1: Extract public route-recording helpers in Core (enabling refactor, no behavior change)

**Files:**
- Create `Src/RCommon.Core/EventHandling/Routing/EventRouteRegistrationExtensions.cs`.
- Modify `Src/RCommon.Core/EventHandling/InMemoryEventBusBuilderExtensions.cs`.
- Test: existing `InMemoryEventBusPublishTests`/`InMemoryEventBusUseRCommonOutboxTests` must stay green; optionally add `EventRouteRegistrationExtensionsTests`.

**Design:** move the durability-recording logic currently inline in the bus `Publish<T>()` (the `GetRoutingRegistry` → `GetOrCreateBuilderState` → `RecordPublished` → apply-default → `new EventRouteHandle(...)` dance) into public `IServiceCollection` helpers keyed by an explicit `builderType` argument. Refactor bus `Publish<T>()` to: register producer + subscription (unchanged) then `return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));`. Refactor bus `UseRCommonOutbox` to: `builder.Services.ApplyBuilderOutboxDefault(builder.GetType(), dataStoreName); return builder;`.

- [ ] **Step 1: RED via refactor safety net** — the existing bus tests are the safety net. First run them to confirm green baseline. Then write a small direct unit test for the new helpers if they don't yet exist (asserts `RecordPublishRoute` returns a handle whose `.UseOutbox` marks durable; `ApplyBuilderOutboxDefault` retroactively marks published events) — this will fail to compile (helpers missing).
- [ ] **Step 2: run to verify fail** — `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~EventRouteRegistration"` → FAIL (missing).
- [ ] **Step 3: implement** the helpers; refactor the bus verbs to delegate to them.
- [ ] **Step 4: run** the new helper tests → PASS, AND the full `RCommon.Core.Tests` → all green (the bus Publish/UseRCommonOutbox tests must still pass unchanged — this proves the refactor preserved behavior). `dotnet build Src/RCommon.sln` clean.
- [ ] **Step 5: commit** `refactor(events): extract public route-recording helpers for reuse across builders`

---

## Task 2: Mediator `Publish<TEvent>()`

**Files:**
- Modify `Src/RCommon.Mediatr/MediatREventHandlingBuilderExtensions.cs`.
- Test: `Tests/RCommon.Mediatr.Tests/MediatREventHandlingPublishSendTests.cs`.

**Design:** `public static IEventRouteHandle Publish<TEvent>(this IMediatREventHandlingBuilder builder) where TEvent : class, ISerializableEvent`:
1. `builder.AddProducer<PublishWithMediatREventProducer>();` (idempotent — confirm the exact type/namespace; use the same `AddProducer<T>` used elsewhere).
2. `builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));`
3. `return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));`
(Add the necessary `using RCommon.EventHandling;` / `RCommon.EventHandling.Routing;` / producer namespace.)

- [ ] **Step 1: failing tests** — build a real RCommon builder with `WithEventHandling<MediatREventHandlingBuilder>(events => events.Publish<TestEvent>())` (follow existing `MediatREventHandlingBuilderTests` for bootstrap). Assert: exactly one `PublishWithMediatREventProducer` registered (idempotent across repeated `Publish`/`AddSubscriber`); subscription recorded for the mediator builder type; `Publish<TestEvent>()` alone leaves the event transient (`GetRoutingRegistry().IsDurable` false); `Publish<TestEvent>().UseOutbox("Orders")` marks it durable → "Orders".
- [ ] **Step 2: run to verify fail.**
- [ ] **Step 3: implement.**
- [ ] **Step 4: run to verify pass** (filtered + full `RCommon.Mediatr.Tests`).
- [ ] **Step 5: commit** `feat(events): add mediator Publish<T>() + .UseOutbox (AC-12, AC-13)`

---

## Task 3: Mediator `Send<TEvent>()`

**Files:** as Task 2.

**Design:** identical shape to Task 2 but registers `SendWithMediatREventProducer` (point-to-point). `public static IEventRouteHandle Send<TEvent>(this IMediatREventHandlingBuilder builder) where TEvent : class, ISerializableEvent`. Same three steps (producer + subscription + `RecordPublishRoute`).

> **Scope reminder:** `Send<T>()` here registers only the OUTBOUND producer. It does NOT wire a MediatR `IRequestHandler` / CQRS request pipeline (MN-6). Document this in the method's XML remarks (the consuming side for a mediator route is the developer's MediatR handler / `AddSubscriber` for notifications).

- [ ] **Step 1: failing tests** — `Send<TestEvent>()` registers exactly one `SendWithMediatREventProducer`; records subscription; transient by default; `.UseOutbox("Orders")` marks durable. Also: `Publish<T>()` and `Send<T>()` for the SAME event register BOTH producers (fan-out + point-to-point) and both are recorded as targets (assert via `EventSubscriptionManager.GetProducersForEvent`).
- [ ] **Step 2–4:** RED → implement → GREEN (filtered + full Mediatr project).
- [ ] **Step 5: commit** `feat(events): add mediator Send<T>() point-to-point verb (AC-12, AC-13)`

---

## Task 4: Mediator `UseRCommonOutbox("store")`

**Files:** as Task 2.

**Design:** `public static IMediatREventHandlingBuilder UseRCommonOutbox(this IMediatREventHandlingBuilder builder, string dataStoreName)` → `builder.Services.ApplyBuilderOutboxDefault(builder.GetType(), dataStoreName); return builder;`.

- [ ] **Step 1: failing tests** — all orderings (mirror the bus `UseRCommonOutbox` tests): default-then-Publish; Publish-then-default (retroactive); per-event `.UseOutbox` overrides builder default (both orders); no default + no `.UseOutbox` ⇒ transient. Confirm the mediator builder's state is keyed by its own builder type (isolated from a bus builder's default in the same container — if both are configured, their defaults do not bleed).
- [ ] **Step 2–4:** RED → implement → GREEN.
- [ ] **Step 5: commit** `feat(events): add mediator UseRCommonOutbox builder default (AC-12)`

---

## Task 5: Mediator durability + target-producer routing proof

**Files:**
- Create `Tests/RCommon.Mediatr.Tests/MediatRDurableRouteRoutingTests.cs`.

Prove that a durable mediator route integrates with the Phase-3a pipeline WITHOUT new pipeline code:
- `Publish<OrderConfirmed>().UseOutbox("Orders")` on the mediator builder ⇒ `IEventRoutingRegistry.TryGetOutboxStore(typeof(OrderConfirmed))` = "Orders" AND `EventSubscriptionManager.GetProducersForEvent(..., typeof(OrderConfirmed))` includes `PublishWithMediatREventProducer` (so the outbox `TargetProducers` recording — which uses this same subscription filter — will record the mediator producer, and the poller/relay will dispatch to it post-commit).
- A transient mediator `Publish<OrderPlaced>()` (no `.UseOutbox`) ⇒ not durable; would be dispatched pre-commit by the tracker.
- (Optional, if feasible with the existing outbox test fakes) assemble the outbox tracker over a mediator `Publish<...>().UseOutbox(...)` route and assert the durable event is buffered/persisted with the mediator producer recorded as target. If this duplicates Phase-3a coverage too much, the registry+subscription assertions above are sufficient — do not rebuild the whole pipeline test.

- [ ] Steps 1–5 as usual; commit `test(events): prove durable mediator routes record the mediator producer as outbox target (AC-2, AC-13)`.

---

## Phase-completion checks

- [ ] Full fast-lane suite green: `dotnet test Src/RCommon.sln --filter "Category!=Integration"`.
- [ ] Solution builds all TFMs: `dotnet build Src/RCommon.sln -c Release`.
- [ ] Final whole-branch code review (superpowers:code-reviewer) of the Phase-3b diff — special attention that the Task-1 refactor preserved bus behavior (bus tests unchanged and green), the mediator verbs register the correct producers idempotently, and durability recording is identical across bus and mediator (shared helper).
- [ ] No Phase-3b test carries `[Trait("Category","Integration")]`.

## Notes for later phases
- Phase 3c (AC-21): `AddOutboxProducer`/`AddOutboxProcessor` topology split — independent of these verbs.
- Phase 4: broker `Publish`/`Send`/`Consume` + `UseBrokerOutbox` will ALSO reuse the Task-1 `RecordPublishRoute` helper for their RCommon-outbox durability (`.UseOutbox`/`UseRCommonOutbox`), while `UseBrokerOutbox` uses a separate broker-native mechanism (not `IEventRoutingRegistry`).
- If a native (non-MediatR) mediator event-handling builder is ever added, it reuses these same helpers.
