# Event Handling: Producer Auto-Registration & Silent-Routing Fixes

**Branch:** bugfix/consumer-feedback-hardening
**Date:** 2026-07-15
**Status:** Approved
**Breaking Change:** No

## Overview

`AddSubscriber<TEvent,THandler>()` on `IInMemoryEventBusBuilder` (`Src/RCommon.Core/EventHandling/InMemoryEventBusBuilderExtensions.cs:28-56`, both the plain and factory-delegate overloads) registers `ISubscriber<TEvent>` and records the subscription with `EventSubscriptionManager`, but never registers an `IEventProducer`. `InMemoryTransactionalEventRouter` (`Src/RCommon.Core/EventHandling/Producers/InMemoryTransactionalEventRouter.cs`) only ever dispatches through `IServiceProvider.GetServices<IEventProducer>()` — with zero producers registered, that collection is empty, the routing loop completes zero iterations, and the subscriber's `HandleAsync` is never invoked. There is no exception, no warning, not even a Debug-level "0 producers found" line — the router's own "routing N transactional events to event producers" `LogInformation` line fires *before* the producer list is even fetched, so it logs identically whether the routing is about to succeed or silently do nothing. A separate, undocumented call — `AddProducer<PublishWithEventBusEventProducer>()` — is the only thing that makes it work, and nothing connects the two calls for a reader of `AddSubscriber`'s own documentation.

Verified: the canonical `website/docs/event-handling/in-memory.mdx` page *does* always pair `AddSubscriber` with `AddProducer` correctly — the doc gap the original field report described is real, but on a different page (`website/docs/cqrs-mediator/wolverine.mdx`, which shows its own builder's subscriber registration used alone in its primary example). This spec fixes the underlying silent-failure mechanism at the code layer, which helps regardless of which doc page a consumer read.

## Personas

- **Library consumer wiring the in-memory event bus** — Calls `WithEventHandling<InMemoryEventBusBuilder>(eh => eh.AddSubscriber<TeamCreatedEvent, TeamCreatedEventHandler>())`, expects the handler to fire when the event is raised. Should not need to know a separate `AddProducer` call exists at all for the common case.
- **RCommon contributor building a new event-handling provider package** (Wolverine, MassTransit, or a future one) — Needs the same "subscription without a producer is always a mistake" invariant enforced generally, not reimplemented per builder.

## Core Requirements

### Must Have

- Both `AddSubscriber<TEvent,TEventHandler>()` overloads on `IInMemoryEventBusBuilder` automatically ensure `PublishWithEventBusEventProducer` is registered as an `IEventProducer` for that builder, by calling the already-idempotent `AddProducer<PublishWithEventBusEventProducer>()` (`IInMemoryEventBusBuilder : IEventHandlingBuilder`, so `AddProducer<T>` — defined on `IEventHandlingBuilder` — is already directly callable from within `AddSubscriber`'s own body; no new interface relationship needed). This closes the gap for the specific, common scenario the field report described with zero required consumer action — calling `AddSubscriber` alone is now sufficient.
- A general, cross-builder safety net: `RCommonBootstrapDiagnosticsHostedService` (already extended once in this release for Spec 2's data-store check) additionally inspects `EventSubscriptionManager` at startup — for every builder type with at least one recorded subscription (`HasSubscriptions`-style check, generalized per-builder-type rather than the existing global flag), if that same builder type has zero recorded producers, emit a single `LogWarning` naming the builder type and the missing producer registration. This catches the same class of misconfiguration for any current or future `IEventHandlingBuilder` implementation (Wolverine, MassTransit, a custom one) that doesn't auto-register its own producer the way this spec fixes for the in-memory builder specifically.
- `InMemoryTransactionalEventRouter.RouteEventsAsync`'s "routing N transactional events to event producers" `LogInformation` line is moved to fire *after* `eventProducers` is fetched, and a new `LogWarning` fires specifically when there is at least one event to route but zero producers are found for it — replacing today's total silence in that specific case with an explicit, correctly-timed signal, independent of the auto-registration fix (defense in depth for any path that reaches the router with a builder type outside the general startup check's coverage, e.g. dynamic reconfiguration scenarios).

### Must Not Do

- Must not change `ISubscriber<TEvent>`, `IEventProducer`, `IEventRouter`, or any other public event-handling interface. All three fixes above are additive behavior inside existing method bodies plus one new startup-diagnostics check; no signature changes anywhere.
- Must not make the cross-builder startup check a hard failure. Per the investigation, there is no known legitimate reason to have subscriptions with zero producers, which might argue for a hard fail — but turning a previously-silent (non-crashing, if secretly broken) misconfiguration into a startup exception is a real behavior change for any app currently in this state, however buggy. A `LogWarning` gives full visibility with zero risk of newly breaking a running app on upgrade; a harder failure mode can be reconsidered for a future major version once this warning has had a release or two to surface existing instances of the problem.
- Must not change `AddProducer<T>`'s existing idempotency/dedup logic — `AddSubscriber` calling it is just an additional call site exercising the same, already-correct logic.

## Technical Constraints

- No new package dependencies. Reuses `EventSubscriptionManager`'s existing per-builder-type tracking (`AddSubscription`, `AddProducerForBuilder`) rather than introducing a parallel bookkeeping structure.

## Resilience

Not applicable — in-process DI/configuration concern, no I/O.

## Observability

- New `LogWarning` (startup, cross-builder check) and `LogWarning` (router, zero-producers-for-this-event case) are the two new observability surfaces. Both follow this repo's existing convention (see `docs/specs/bootstrapping/bootstrapping.md`'s Observability section) of reserving `LogWarning` for conditions that are almost certainly a real misconfiguration, as opposed to `LogInformation` for merely-worth-knowing-about inferences (c.f. Spec 2's data-store auto-infer, which is `LogInformation` because it's a successful, non-broken outcome).

## Security

Not applicable — no new trust boundary; this only affects whether an already-configured in-process event is delivered and logged.

## Performance & Scalability

Negligible. `AddProducer<T>`'s idempotency check (a `Services.Any(...)` scan) already runs once per `AddSubscriber` call at configuration time, not per-event at runtime; the router's log-ordering change and new conditional `LogWarning` are per-routing-call, same cost class as the logging already present there.

## Design Detail

### `AddSubscriber` auto-registers the producer

```csharp
public static void AddSubscriber<TEvent, TEventHandler>(this IInMemoryEventBusBuilder builder)
    where TEvent : class, ISerializableEvent
    where TEventHandler : class, ISubscriber<TEvent>
{
    builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();

    // A subscriber with no registered IEventProducer for this builder is never invoked, with no
    // error of any kind (see docs/specs/event-handling/producer-auto-registration.md). There is
    // no scenario where a consumer wants a subscriber wired but not routed, so ensure the
    // standard in-memory producer is registered -- idempotent via AddProducer<T>'s own
    // already-registered check, so calling this repeatedly across multiple AddSubscriber calls
    // is a no-op after the first.
    builder.AddProducer<PublishWithEventBusEventProducer>();

    var subscriptionManager = builder.Services.GetSubscriptionManager();
    subscriptionManager?.AddSubscription(builder.GetType(), typeof(TEvent));
}
```

The factory-delegate overload (`AddSubscriber<TEvent,TEventHandler>(this IInMemoryEventBusBuilder builder, Func<IServiceProvider,TEventHandler> getSubscriber)`) gets the identical one-line addition.

### Cross-builder startup check

Extends `RCommonBootstrapDiagnosticsHostedService.StartAsync` (already touched once in this release for Spec 2) with a second, independent check:

```csharp
var subscriptionManager = _services.GetSubscriptionManager();
if (subscriptionManager is not null)
{
    foreach (var builderType in subscriptionManager.GetBuilderTypesWithSubscriptions())
    {
        if (!subscriptionManager.HasProducerForBuilder(builderType))
        {
            _loggerFactory?.CreateLogger<IRCommonBuilder>().LogWarning(
                "RCommon found event subscriptions registered for {BuilderType} with no matching " +
                "IEventProducer -- subscribers on this builder will never be invoked. Call " +
                "AddProducer<T>() for this builder, or (for InMemoryEventBusBuilder) this is " +
                "handled automatically as of this version.",
                builderType.Name);
        }
    }
}
```

(`GetBuilderTypesWithSubscriptions()` is a small new read-only accessor on `EventSubscriptionManager` exposing the existing internal per-builder-type subscription map; `HasProducerForBuilder` already exists.)

### Router log-ordering fix

```csharp
var eventProducers = _serviceProvider.GetServices<IEventProducer>().ToList();

if (transactionalEvents.Any())
{
    if (!eventProducers.Any())
    {
        _logger.LogWarning(
            "{Router} has {Count} transactional event(s) to route but no IEventProducer is " +
            "registered -- these events will not be delivered to any subscriber.",
            this.GetGenericTypeName(), transactionalEvents.Count());
    }
    else
    {
        _logger.LogInformation(
            "{Router} is routing {Count} transactional events to event producers.",
            this.GetGenericTypeName(), transactionalEvents.Count());
    }
    // ... existing per-event Debug-level routing loop, unchanged
}
```

## Testing Strategy

1. `AddSubscriber<TEvent,THandler>()` alone (no explicit `AddProducer` call) results in the subscriber's `HandleAsync` firing when the event is raised through a repository `AddAsync`/`UpdateAsync` call — the core regression test for the originally reported bug.
2. Calling `AddSubscriber` for multiple event types on the same builder registers `PublishWithEventBusEventProducer` exactly once (idempotency).
3. Cross-builder startup check: a builder type with a recorded subscription and zero recorded producers produces exactly one `LogWarning` naming that builder type; a builder type with both present produces none.
4. Router log-ordering: zero producers with at least one event to route produces the new `LogWarning`, not the old always-fires `LogInformation`; nonzero producers produces the `LogInformation` as before.

## File Summary

| File | Action | Location |
|------|--------|----------|
| `InMemoryEventBusBuilderExtensions.cs` | Modify (both `AddSubscriber` overloads) | `Src/RCommon.Core/EventHandling/` |
| `EventSubscriptionManager.cs` | Modify — add `GetBuilderTypesWithSubscriptions()` read-only accessor | `Src/RCommon.Core/EventHandling/` |
| `RCommonBootstrapDiagnosticsHostedService.cs` | Modify — add cross-builder producer check | `Src/RCommon.Core/` |
| `InMemoryTransactionalEventRouter.cs` | Modify — log-ordering fix | `Src/RCommon.Core/EventHandling/Producers/` |
| `in-memory.mdx` | Modify — note that `AddProducer` is now automatic, kept documented for explicitness | `website/docs/event-handling/` |
| `wolverine.mdx` | Modify — fix the primary example to pair subscriber + producer registration (the actual doc gap found) | `website/docs/cqrs-mediator/` |
| Test files (per Testing Strategy above) | Create | `Tests/RCommon.Core.Tests/` |
