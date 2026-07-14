# Outbox Producer/Processor Topology Design Spec (Future Enhancement)

> **Scope:** Formalize a first-class producer-host vs processor-host topology for the RCommon built-in persistence outbox. This is a **proposed future enhancement**, not yet implemented.

**Date:** 2026-07-14
**Status:** Proposed — investigation / not scheduled
**Backward Compatibility:** Must be additive (no breaking changes to existing `AddOutbox<TOutboxStore>` callers).

---

## 1. Motivation

The cross-host outbox correctness bug (fixed in 3.1.1 via `OutboxOptions.ImmediateDispatch`) exposed a
structural gap: RCommon has no first-class notion of a **producer-only host** versus a **processor
(poller) host**.

Today `AddOutbox<TOutboxStore>` always registers *both* the producer-side services (store, router,
entity-event tracker, serializer) *and* the hosted background poller (`OutboxProcessingService`). A host
that should only *write* to the outbox — leaving dispatch to a dedicated poller host — cannot express that
intent through the RCommon API. Downstream consumers have worked around this by manually splitting the
registration into "producer" and "processor" halves (registering the poller only where wanted) and by
setting `ImmediateDispatch = false` on producer hosts.

The `ImmediateDispatch` flag makes the topology **correct**, but the topology itself is still implicit:

- Whether a host runs the poller depends on whether the caller happened to register the hosted service.
- Whether a host dispatches in-process (Phase 3) depends on a separate options flag.
- Nothing ties these two decisions together or fails fast when they are inconsistent
  (e.g. a producer-only host that forgets `ImmediateDispatch = false`).

## 2. Goals

1. Make "producer-only host" and "processor host" explicit, first-class registration choices.
2. Derive the correct `ImmediateDispatch` default from that choice, so the safe configuration is the
   default and the footgun disappears.
3. Preserve full backward compatibility: existing `AddOutbox<TOutboxStore>(...)` calls keep today's
   single-host behaviour (producer + processor + `ImmediateDispatch = true`).
4. Optionally fail fast on inconsistent configurations.

## 3. Proposed Shape (sketch — to be refined during design)

Introduce explicit registration entry points alongside the existing `AddOutbox<TOutboxStore>`:

- `AddOutboxProducer<TOutboxStore>(...)` — registers store, router, entity-event tracker, serializer;
  **does not** register the hosted poller; defaults `ImmediateDispatch = false`.
- `AddOutboxProcessor(...)` — registers the hosted `OutboxProcessingService`; intended to be combined with
  a producer registration on the poller host; leaves `ImmediateDispatch = true` for that host's own writes.
- `AddOutbox<TOutboxStore>(...)` — unchanged; equivalent to producer + processor on one host with
  `ImmediateDispatch = true`.

Open questions to resolve in the design phase:

- Naming and whether these compose (`AddOutboxProducer().AddOutboxProcessor()`) or are mutually exclusive.
- How/whether to fail fast when a host registers a producer with `ImmediateDispatch = true` but no poller
  and no processor host is detectable (can only be a warning — cross-host wiring is not observable from a
  single process).
- Interaction with multi-DbContext scenarios (a generic `EFCoreOutboxStore<TContext>` or an explicit
  store-name parameter would remove today's subclassing boilerplate and is a natural companion change).

## 4. Non-Goals

- This spec does not change the delivery semantics fixed in 3.1.1; `ImmediateDispatch` remains the
  correctness mechanism. This enhancement is ergonomics and safe-by-default configuration on top of it.
- No changes to the MassTransit/Wolverine transport-integrated outbox.

## 5. Related

- Conceptual guide: `website/docs/event-handling/outbox-producer-processor-topology.mdx`
- `OutboxOptions.ImmediateDispatch` (shipped 3.1.1)
- Prior outbox specs: `2026-03-21-transactional-outbox-design.md`, `2026-03-23-outbox-v2-design.md`
