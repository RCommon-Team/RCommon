# Event Handling

## Overview

RCommon's event-handling domain provides a single, uniform model for raising, dispatching, and durably delivering events across in-process and out-of-process transports. Every event is treated uniformly (`ISerializableEvent`); the distinction between **domain** (in-process, ordered, transactional) and **integration** (cross-boundary, durable) handling is a consequence of how the developer *routes* an event, never a type the framework imposes.

This domain spec formalizes the 3.2.0 redesign. Full design rationale, diagrams, and worked examples live in the design document: `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md`. That document is the authoritative narrative; this spec is the testable contract.

## Personas

- **Application developer (DDD)** — models aggregates that raise domain events; wants in-process, ordered, transactional handling and reliable cross-boundary integration events.
- **Application developer (non-DDD / transaction-script)** — wants to publish events imperatively within a unit of work without aggregates.
- **Platform/infrastructure engineer** — runs producer and processor hosts, chooses transports (RCommon outbox, MassTransit, Wolverine, MediatR), owns migrations and CI.
- **RCommon maintainer** — must keep the model coherent across transports and prevent silent event/write loss.

## Core Requirements

### Must Have (testable acceptance criteria)

- **AC-1 (uniform model):** Any type implementing `ISerializableEvent` can be raised and routed. No `IIntegrationEvent`/`IDomainEvent` type gate exists; `IDomainEvent`/`AggregateRoot.AddDomainEvent` remain ergonomic helpers only.
- **AC-2 (route-based semantics):** Whether an event is handled in-process (domain) or persisted+relayed (integration) is determined solely by its configured route(s). The same event routed to both an in-process subscriber and a durable/outbound producer is handled both ways.
- **AC-3 (pre-commit ordered dispatch):** During `CommitAsync`, in-process domain handlers run **before** the transaction commits, inside the transaction. `ISyncEvent` handlers execute sequentially in raise-order; `IAsyncEvent` handlers are awaited concurrently. Verified by an explicit ordering test.
- **AC-4 (FIFO drain):** Events raised by handlers during dispatch are enqueued to a single FIFO dispatch queue and drained to empty before persistence; termination is empty-queue, not a re-harvest comparison. A cascade exceeding the configurable generation limit (default 16) throws a descriptive exception (fail-loud).
- **AC-5 (transactional translation):** A domain handler that raises an integration event (via `AddTransactionalEvent`) results in that outbox row being persisted within the same transaction as the originating state change.
- **AC-6 (atomicity / all-or-nothing):** If any pre-commit handler throws, the transaction rolls back: no state change, no outbox rows, no relay.
- **AC-7 (per-datastore outbox / B4):** An event's outbox row is written to the **same** DbContext/datastore/transaction as the state change that produced it. Events from a non-default datastore are never written to another datastore's outbox table.
- **AC-8 (datastore threading):** `IEntityEventTracker.AddEntity` captures a datastore name; the tracker groups events by datastore; `IOutboxStore.SaveAsync` is datastore-parametric; a single `EFCoreOutboxStore` resolves the correct context per call (no per-store subclass required).
- **AC-9 (multi-datastore polling):** `OutboxProcessingService` claims and drains from every registered outbox datastore.
- **AC-10 (target-producer routing):** Each outbox row records its target producer(s); the poller relays each row only to its recorded producer(s), supporting fan-out to multiple transports from one per-datastore table.
- **AC-11 (schema auto-map + verify):** Registering an outbox on a datastore auto-applies the `OutboxMessage` mapping to that datastore's `RCommonDbContext` model. A startup diagnostic fails loud (warning/throw) if a registered outbox datastore's table/mapping is missing.
- **AC-12 (fluent API):** Delivery strategy and durability are expressed independently: `AddSubscriber<T,H>()` (in-process handler), `Publish<T>()` (fan-out), `Send<T>()` (point-to-point), `Consume<T,H>()` (inbound broker consumer). Durability modifiers: per-event `.UseOutbox("store")`, builder-level `UseRCommonOutbox("store")` / `UseBrokerOutbox(...)`; per-event overrides builder-level; neither ⇒ no outbox.
- **AC-13 (three transports):** In-process bus, in-process mediator (MediatR/native), and broker (MassTransit/Wolverine) are all first-class destinations. `Publish`/`Send` apply to bus, mediator, and broker; `Consume` is broker-only; `UseBrokerOutbox` is broker-only.
- **AC-14 (broker outbox wrapper):** `UseBrokerOutbox(o => o.OnDataStore("X"))` configures the broker's native EF Core outbox against datastore X's `DbContext` (MassTransit `AddEntityFrameworkOutbox` + `UseBusOutbox`; Wolverine `UseEntityFrameworkCoreTransactions`), resolved from the RCommon datastore registration.
- **AC-15 (broker coordination proven):** An integration test on the Podman/Testcontainers harness asserts that, under recipe 2b, business state + broker-outbox rows commit atomically and a rollback leaves neither. Recipe 2b is "done" only when green.
- **AC-16 (recipes proven):** Each of the five recipes ships as a runnable example with an end-to-end test asserting the documented wiring composes and produces the recipe's observable outcome.
- **AC-17 (back-compat shims):** `IEntityEventTracker.AddEntity(entity)` overload preserved (defaults to default datastore); `AddSubscriber` retained as `[Obsolete]` alias forwarding to `Consume` on broker builders; `EFCoreOutboxStore<TContext>`/subclass marked `[Obsolete]`.
- **AC-18 (opt-in metrics) — DEFERRED, not shipped in 3.2.0.** Planned: RCommon exposes a `RCommon.Outbox` `System.Diagnostics.Metrics.Meter` instrumenting per-datastore pending depth, oldest-unprocessed age, relay success/failure counts, dead-letter rate, and dispatch-queue depth. Opt-in (host registers the meter with its metrics pipeline); additive/non-breaking. **Current state:** not implemented; observe via the `__OutboxMessages` tables or poller logs.
- **AC-19 (payload protection hook) — DEFERRED, not shipped in 3.2.0.** Planned: an `IOutboxPayloadProtector` seam wrapping payload serialization with `Protect`/`Unprotect`; default pass-through (plaintext), applications may supply an encrypting implementation. Non-breaking. **Current state:** not implemented; payloads are plaintext within the application-database trust boundary.
- **AC-20 (deserialization allow-list) — DEFERRED, not shipped in 3.2.0.** Planned: the outbox serializer resolves only event types present in the registration set (types with a route/subscriber/producer); an unknown/unresolvable `EventType` on relay/consume is logged loud and dead-lettered — never deserialized to an arbitrary type. **Current state:** not implemented; the serializer resolves the type named in the row.
- **AC-21 (producer/processor topology):** First-class `AddOutboxProducer` (store/router/tracker, no hosted poller) and `AddOutboxProcessor` (hosted poller) registration methods exist alongside `AddOutbox` (= producer + processor). Each is datastore-scoped (`OnDataStore(...)`), consolidating the multi-host topology into this release's registration rework.

### Must Not Do

- **MN-1:** Must not impose an `IIntegrationEvent`/`IDomainEvent` type split.
- **MN-2:** Must not build a mediator-native outbox; mediator durability, when wanted, uses RCommon's per-datastore outbox.
- **MN-3:** Must not silently drop events or writes — every dead path fails loud (carries forward the 3.1.3 principle).
- **MN-4:** Must not write an event's outbox row to a datastore other than the one holding its originating state change.
- **MN-5:** Must not own EF migration history — the developer generates/applies migrations.
- **MN-6:** This spec does not cover the MediatR/native-mediator **CQRS request pipeline** (command/query handling); only the mediator as an event-handling transport.

### Nice to Have

- The producer/processor topology split (AC-21) was pulled into scope during spec review and **shipped** in 3.2.0. Payload protection (AC-19) and first-class metrics (AC-18) were also accepted into scope but were **deferred and did not ship in 3.2.0** — they remain roadmap items (see Open Questions).

## Technical Constraints

- **Target version:** 3.2.0 (breaking changes accepted).
- **Frameworks:** multi-target net8.0/net9.0/net10.0; MassTransit and Wolverine (and their EF outbox packages) differ per TFM, so transport wrappers may require conditional compilation.
- **Persistence:** EF Core via `RCommonDbContext`/`IDataStoreFactory`; unit of work built on `System.Transactions.TransactionScope`.
- **Serialization:** `IOutboxSerializer` (default `JsonOutboxSerializer`).
- **Container runtime for integration tests/examples:** **Podman** (not Docker); Testcontainers targets Podman via its Podman support (rootless socket / `DOCKER_HOST`).
- `[from: project CLAUDE.md]` TDD is mandatory; RCommon unit tests written first (red-green).

## Resilience

External dependencies: one relational database per registered datastore; optionally a message broker (e.g. RabbitMQ) via MassTransit/Wolverine.

- **Database offline / commit failure:** `CommitAsync` fails and the transaction does not complete — all-or-nothing (AC-6). Outbox rows exist only if the commit succeeded, so state and intent-to-publish never diverge.
- **Broker offline — recipe 2a (RCommon outbox):** outbox rows remain `ProcessedAtUtc IS NULL`; the per-datastore poller retries with exponential backoff (`OutboxOptions.BackoffBaseDelay`/`BackoffMaxDelay`/`BackoffMultiplier`), dead-letters after `MaxRetries`, and dead-letters are replayable (`ReplayDeadLetterAsync`).
- **Broker offline — recipe 2b (native broker outbox):** the broker's own delivery service retries per its configuration; RCommon's role ends at atomic staging.
- **In-process handler failure (pre-commit):** rolls the whole transaction back (AC-6).
- **Duplicate delivery / idempotency:** the inbox (`IInboxStore`) dedups redelivery on the consuming side.
- **Recovery:** automatic — the poller resumes draining unprocessed rows when a dependency returns; dead-letters require explicit replay. No manual intervention for transient outages.

## Observability

- **Warnings (fail-loud):** poller draining an event type with zero matching subscribers (once per type); outbox routing overridden by a later registration (startup diagnostic); missing outbox schema on a registered datastore (startup diagnostic); cycle-breaker generation limit exceeded; best-effort relay/dispatch failure (before retry); dead-lettering.
- **Debug/Info:** dispatch counts per commit, poller poll cycles and claim counts, `ImmediateDispatch` skip on producer-only hosts.
- **Metrics (first-class, opt-in) — DEFERRED, not shipped in 3.2.0 (AC-18).** Planned: a `RCommon.Outbox` `Meter` (System.Diagnostics.Metrics) exposing per-datastore outbox pending depth and oldest-unprocessed age; relay success/failure counts; dead-letter rate; dispatch-queue depth and max cascade generation reached. Host-agnostic — the application wires the meter into OpenTelemetry/Prometheus/etc. Until it ships, observe via the `__OutboxMessages` tables or the poller's warnings/logs.
- **Alerting:** outbox backlog age exceeding a threshold and dead-letter rate are the primary signals; thresholds are host-owned.

## Security

- **Attack surface:** event payloads are serialized into the outbox (JSON) and to brokers, then deserialized on relay/consume. Type resolution goes through `IOutboxSerializer`; only known/registered event types should be deserialized.
- **Data protection:** outbox rows live in the application database, inside the same trust boundary and at-rest protection as business data. `TenantId` is recorded per row for multi-tenant isolation. Payloads are plaintext. The `IOutboxPayloadProtector` field/payload-protection seam (AC-19) is **DEFERRED — not shipped in 3.2.0**; applications needing payload protection must handle it above the event model until it ships.
- **Deserialization safety — DEFERRED, not shipped in 3.2.0 (AC-20).** Planned: the serializer enforces an allow-list of registered event types and a tampered/unknown `EventType` is logged and dead-lettered rather than deserialized. **Current state:** the serializer resolves the type named in the outbox row; deploy the outbox only within a trusted boundary until the allow-list ships.
- **Auth/authz:** not applicable at the library level; consumers execute in their own DI scope. Inbox idempotency prevents duplicate side effects from replays.
- **Compliance:** none imposed by the library; applications remain responsible for any PII/regulatory handling of event payloads.

## Performance & Scalability

- **Pre-commit dispatch cost:** proportional to (events × matching in-process handlers); sync handlers are sequential, so they add commit latency proportional to handler work. Guardrail (documented): pre-commit in-transaction handlers must be in-process and side-effect-light (no external I/O).
- **Dispatch queue:** O(1) enqueue/dequeue; single pass.
- **Poller throughput:** per-datastore, bounded by `OutboxOptions.BatchSize` (default 100) and `PollingInterval` (default 5s); multiple datastores poll independently.
- **Scaling:** horizontal — multiple processor hosts claim rows with locking (`LockedByInstanceId`/`LockedUntilUtc`); producer-only hosts persist without a poller (`ImmediateDispatch = false`).
- **Testing:** correctness under real engines via the Podman/Testcontainers harness, plus sanity throughput assertions (e.g. the poller drains a batch within an expected window; dispatch of N events completes in order). No formal micro-benchmark suite in 3.2.0.

## Design Detail

See `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md` for the full model, the reordered commit pipeline, the FIFO dispatch queue, per-datastore outbox threading, the fluent API and canonical durability verbs, the MassTransit/Wolverine wrappers with the verification-gated coordination spike, and the five recipes with mermaid diagrams. Key breaking changes: `IOutboxStore.SaveAsync` gains `dataStoreName`; `CommitAsync` phase reorder (pre-commit domain dispatch); new fluent verbs; `AddEntity` datastore overload; `Consume` rename with `[Obsolete]` `AddSubscriber` alias on broker builders.

## Testing Strategy

- **RCommon `Tests/` — authoritative gate (TDD, written first):** unit tests for FIFO ordering, pre-commit dispatch + cycle-breaker, per-datastore grouping/routing, target-producer recording, datastore capture, multi-datastore poller, startup diagnostics; a **new Podman/Testcontainers harness (Postgres + RabbitMQ)** for cross-datastore atomicity, `TransactionScope` enlistment, and the recipe-2b coordination spike (AC-15).
- **Examples `+ .Tests` — proof-of-work:** each recipe (AC-16) is a runnable example with an end-to-end test asserting the documented wiring composes and behaves.

## Migration (3.2.0)

Breaking changes are softened with shims (AC-17). Deliverables: a migration guide (old→new mapping), the recipe conceptual guide + diagrams, and the Podman/Testcontainers CI change. Phasing (detailed by the plan): (0) harness + coordination spike; (1) datastore-aware tracker/router/store/poller; (2) pipeline reorder + FIFO queue + ordering; (3) fluent API; (4) MT/Wolverine wrappers + recipes 2a/2b; (5) recipes as examples + e2e; (6) docs + migration guide.

## Open Questions

Scope was resolved during spec review (2026-07-22), but three accepted items (AC-18/19/20) were **deferred and did not ship in 3.2.0** — reopened below as roadmap items:

- **OQ-1 (metrics) → DEFERRED:** first-class, opt-in `RCommon.Outbox` `Meter` (AC-18) is designed but not implemented in 3.2.0.
- **OQ-2 (payload protection) → DEFERRED:** optional `IOutboxPayloadProtector` (AC-19) is designed but not implemented in 3.2.0.
- **OQ-3 (perf) → resolved:** correctness-focused + sanity throughput assertions; no formal benchmark suite in 3.2.0.
- **OQ-4 (cycle-breaker default) → resolved:** 16 generations, configurable (AC-4).
- **OQ-5 (deserialization allow-list) → DEFERRED:** the registered-type allow-list (AC-20) is designed but not implemented in 3.2.0.
- **OQ-6 (topology API) → resolved:** fold `AddOutboxProducer`/`AddOutboxProcessor` into 3.2.0 (AC-21).
- **OQ-7 (example names) → resolved:** accept the proposed names, including `Examples.EventHandling.Outbox.MultiDataStore`, `Examples.Messaging.MassTransit.NativeOutbox`, `Examples.Messaging.Wolverine.NativeOutbox`, `Examples.EventHandling.TransactionScript`, `Examples.EventHandling.NoUnitOfWork`.

## Feature Breakdown

- Producer auto-registration & silent-routing fixes → `./producer-auto-registration.md` (3.1.x; shipped)
- Uniform event model & route-based semantics — this spec (§Core AC-1, AC-2)
- Pre-commit ordered dispatch + FIFO queue — this spec (AC-3–AC-6)
- Per-datastore transactional outbox (B4/U5) — this spec (AC-7–AC-11); complex, may warrant its own feature spec during planning
- Fluent configuration API & transports — this spec (AC-12–AC-14)
- MassTransit/Wolverine outbox wrappers & coordination — this spec (AC-14, AC-15); complex, may warrant its own feature spec during planning
- Recipes & examples — this spec (AC-16)
