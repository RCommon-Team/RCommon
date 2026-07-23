# Event Handling Phase 4a — Broker Fluent Verbs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the first-class fluent verbs `Publish<T>()`, `Send<T>()`, `Consume<T,H>()`, and the builder-level `UseRCommonOutbox("store")` to BOTH the MassTransit and Wolverine event-handling builders, and retain `AddSubscriber<T,H>()` as an `[Obsolete]` alias forwarding to `Consume` (spec AC-12/AC-13 broker, AC-17). This is the broker analogue of the Phase-3b mediator work.

**Architecture:** The verbs are thin, builder-agnostic wrappers that reuse the already-extracted `RCommon.Core` helpers `EventRouteRegistrationExtensions.RecordPublishRoute(...)` / `ApplyBuilderOutboxDefault(...)` and the existing `AddProducer<T>()` helper. `Publish`/`Send` register the (already-existing) broker producers, record the event→producer subscription, and return an `IEventRouteHandle` so `.UseOutbox("store")` can mark the route durable. `UseRCommonOutbox` sets the builder-level default (recipe 2a). `Consume<T,H>()` IS the current `AddSubscriber` behaviour (inbound broker consumer + subscription); `AddSubscriber` becomes an `[Obsolete]` forwarding alias. NO broker-native outbox here — `UseBrokerOutbox` is Phase 4b. NO new pipeline/router code — durable broker routes reuse the Phase-3a outbox pipeline unchanged.

**Tech Stack:** .NET net8/9/10 (Src), net10.0 (Tests); MassTransit 8.5.9; WolverineFx 5.39.1; xUnit 2.9.3; AwesomeAssertions 7.2.1 (namespace `FluentAssertions`).

---

## Context for the implementer

Branch `feature/event-handling-outbox-recipes` is already checked out — do NOT switch branches, do NOT commit to main. TDD is mandatory (red → green → refactor). Never sign commits with a Claude/AI signature or co-author line.

### The reusable helpers (do NOT reimplement — call them)

In `Src/RCommon.Core/EventHandling/Routing/EventRouteRegistrationExtensions.cs` (public, builder-agnostic):
- `IEventRouteHandle RecordPublishRoute(this IServiceCollection services, Type builderType, Type eventType)` — records the event as published on that builder, applies any builder-level default, returns a handle whose `.UseOutbox("store")` marks the route durable.
- `void ApplyBuilderOutboxDefault(this IServiceCollection services, Type builderType, string dataStoreName)` — sets the builder-level default store and retroactively marks already-published (non-explicit) events durable. Order-independent; per-event `.UseOutbox` always wins.

In `Src/RCommon.Core/EventHandling/EventHandlingBuilderExtensions.cs`:
- `AddProducer<T>()` on any `IEventHandlingBuilder` — registers `T` as a singleton `IEventProducer`, idempotently, and records the producer for the builder. Both broker builders inherit `IEventHandlingBuilder`, so `builder.AddProducer<...>()` is available.
- `GetSubscriptionManager()` on `IServiceCollection` — returns the `EventSubscriptionManager` (used to record `AddSubscription(builderType, eventType)`).

This is EXACTLY how the mediator verbs are implemented in `Src/RCommon.Mediatr/MediatREventHandlingBuilderExtensions.cs` (lines 122–198). Mirror that shape.

**Ordering matters:** in `Publish`/`Send`, call `AddProducer<...>()` BEFORE `RecordPublishRoute`/`AddSubscription`. The producer must be recorded on the builder first so the subscription copies it into the event→producer map; if you reorder (subscription first), the `ShouldProduceEvent == true` assertions break. The provided code has the correct order — keep it.

### The existing broker producers to register (they already exist)

- MassTransit: `RCommon.MassTransit.Producers.PublishWithMassTransitEventProducer`, `RCommon.MassTransit.Producers.SendWithMassTransitEventProducer`.
- Wolverine: `RCommon.Wolverine.Producers.PublishWithWolverineEventProducer`, `RCommon.Wolverine.Producers.SendWithWolverineEventProducer`.

### `Consume` vs `AddSubscriber` (AC-17)

`Consume<T,H>()` is the inbound broker consumer registration. Its body is exactly the current `AddSubscriber<T,H>()` body (which already wires the broker consumer + subscription). `AddSubscriber` is then rewritten to `[Obsolete(...)]` and simply calls `Consume<T,H>()`. The repo does NOT treat warnings as errors, so the `[Obsolete]` attribute will not break the build, but you MUST migrate existing in-repo call sites (below) to `Consume` to keep the build warning-clean, except one deliberate alias test that suppresses CS0618.

### Warning hygiene — verify no obsolete call sites remain (expected: none)

The repo does NOT set `TreatWarningsAsErrors` anywhere, so the `[Obsolete]` attribute cannot break the build. As of this writing there are also NO existing `AddSubscriber<...>` calls in the broker test projects (verified), so no migration is expected. Still, after marking `AddSubscriber` `[Obsolete]`, run:
`grep -rn "AddSubscriber<" Tests/RCommon.MassTransit.Tests Tests/RCommon.Wolverine.Tests`
and if any hit exists (other than the one deliberate alias test you add under `#pragma warning disable CS0618`), switch it to `Consume<...>` (behaviour-identical) to keep the build warning-clean. Do NOT touch `Examples/` (not in `Src/RCommon.sln`; handled in Phase 5).

### Test harness pattern (mirror the mediator tests)

The mediator tests in `Tests/RCommon.Mediatr.Tests/MediatREventHandlingPublishSendTests.cs` are the template. Setup per test:
```csharp
var services = new ServiceCollection();
services.AddLogging();
var rcommonBuilder = new RCommonBuilder(services);
rcommonBuilder.WithEventHandling<MassTransitEventHandlingBuilder>(events => { /* verbs */ });
```
`new RCommonBuilder(services)` registers the routing registry + subscription manager (so `services.GetRoutingRegistry()` and `services.GetSubscriptionManager()` are non-null). Assertions inspect DI descriptors (producer registration) and the routing registry (`IsDurable` / `TryGetOutboxStore`) exactly as the mediator tests do. If the existing `Tests/RCommon.MassTransit.Tests/MassTransitEventHandlingBuilderTests.cs` uses a different builder-construction pattern, follow whichever actually compiles and runs — but prefer the `RCommonBuilder + WithEventHandling` pattern above since it exercises the real registration path.

Test events implement `ISyncEvent` (which is an `ISerializableEvent`) — same as the mediator tests.

### Build / test commands

- Build MassTransit: `dotnet build Src/RCommon.MassTransit/RCommon.MassTransit.csproj`
- Build Wolverine: `dotnet build Src/RCommon.Wolverine/RCommon.Wolverine.csproj`
- MassTransit tests: `dotnet test Tests/RCommon.MassTransit.Tests/RCommon.MassTransit.Tests.csproj --filter "Category!=Integration"`
- Wolverine tests: `dotnet test Tests/RCommon.Wolverine.Tests/RCommon.Wolverine.Tests.csproj --filter "Category!=Integration"`

### Files

- Modify: `Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs`
- Modify: `Src/RCommon.Wolverine/WolverineEventHandlingBuilderExtensions.cs`
- Test (create): `Tests/RCommon.MassTransit.Tests/MassTransitEventHandlingVerbsTests.cs`
- Test (create): `Tests/RCommon.Wolverine.Tests/WolverineEventHandlingVerbsTests.cs`
- Migrate call sites: existing files under `Tests/RCommon.MassTransit.Tests` / `Tests/RCommon.Wolverine.Tests` that call `AddSubscriber<...>`.

Do NOT touch `UseBrokerOutbox`, the outbox assemblies, the producers, or any pipeline/router/tracker code. Phase 4a is additive verbs + one obsolete alias only.

---

## Task 1: MassTransit broker verbs

**Files:**
- Modify: `Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs`
- Test: `Tests/RCommon.MassTransit.Tests/MassTransitEventHandlingVerbsTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `MassTransitEventHandlingVerbsTests.cs`. Mirror the mediator test suite, substituting the MassTransit builder + producers. Include at minimum: Publish registers exactly one `PublishWithMassTransitEventProducer` (and is idempotent when called twice); Publish records the subscription (`ShouldProduceEvent` true); Publish alone leaves the event transient; `Publish().UseOutbox("Orders")` marks it durable→"Orders"; Send registers exactly one `SendWithMassTransitEventProducer`; Send durability mirror; Publish+Send register both producers; the five `UseRCommonOutbox` ordering scenarios (before/after Publish, per-event-wins, explicit-not-clobbered, none⇒transient); `Consume<T,H>()` registers `ISubscriber<T>`→H and records the subscription; and one alias test that `AddSubscriber<T,H>()` (under `#pragma warning disable CS0618`) has the same effect as `Consume`.

```csharp
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.MassTransit;
using RCommon.MassTransit.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.MassTransit.Tests;

public class MassTransitEventHandlingVerbsTests
{
    private static (ServiceCollection services, RCommonBuilder builder) NewHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return (services, new RCommonBuilder(services));
    }

    [Fact]
    public void Publish_RegistersExactlyOnePublishProducer()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishEvent>());

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithMassTransitEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void Publish_CalledTwice_RegistersProducerExactlyOnce()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.Publish<PublishTwiceEvent>();
            e.Publish<PublishTwiceEvent>();
        });

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithMassTransitEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void Publish_RecordsSubscription()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishSubEvent>());

        var sm = services.GetSubscriptionManager();
        sm.Should().NotBeNull();
        sm!.ShouldProduceEvent(typeof(PublishWithMassTransitEventProducer), typeof(PublishSubEvent))
            .Should().BeTrue();
    }

    [Fact]
    public void Publish_Alone_LeavesEventTransient()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishTransientEvent>());

        services.GetRoutingRegistry()!.IsDurable(typeof(PublishTransientEvent)).Should().BeFalse();
    }

    [Fact]
    public void Publish_WithUseOutbox_MarksEventDurable()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishDurableEvent>().UseOutbox("Orders"));

        var registry = services.GetRoutingRegistry()!;
        registry.IsDurable(typeof(PublishDurableEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(PublishDurableEvent), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void Send_RegistersExactlyOneSendProducer()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Send<SendEvent>());

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(SendWithMassTransitEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void PublishAndSend_SameEvent_RegistersBothProducers()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.Publish<BothEvent>();
            e.Send<BothEvent>();
        });

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithMassTransitEventProducer)).Should().ContainSingle();
        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(SendWithMassTransitEventProducer)).Should().ContainSingle();
    }

    [Fact]
    public void UseRCommonOutbox_ThenPublish_MarksDurable()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.UseRCommonOutbox("Orders");
            e.Publish<OutboxBeforeEvent>();
        });

        var registry = services.GetRoutingRegistry()!;
        registry.IsDurable(typeof(OutboxBeforeEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(OutboxBeforeEvent), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void Publish_ThenUseRCommonOutbox_RetroactivelyMarksDurable()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.Publish<OutboxAfterEvent>();
            e.UseRCommonOutbox("Orders");
        });

        services.GetRoutingRegistry()!.IsDurable(typeof(OutboxAfterEvent)).Should().BeTrue();
    }

    [Fact]
    public void PerEventUseOutbox_WinsOverBuilderDefault_EitherOrder()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.UseRCommonOutbox("Orders");
            e.Publish<PerEventWinsEvent>().UseOutbox("Billing");
        });

        services.GetRoutingRegistry()!.TryGetOutboxStore(typeof(PerEventWinsEvent), out var store);
        store.Should().Be("Billing");
    }

    [Fact]
    public void Consume_RegistersSubscriberAndSubscription()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Consume<ConsumeEvent, ConsumeHandler>());

        // Consume's observable effect is the inbound subscriber registration + a recorded subscription.
        // NOTE: do NOT assert ShouldProduceEvent(...) == false here. EventSubscriptionManager.ShouldProduceEvent
        // returns TRUE (backward-compat fallback) for any event type with no producer-map entry, which is the
        // case when only Consume (no Publish/Send) ran. So a BeFalse() assertion would fail. Assert the
        // subscriber DI binding and that a subscription was recorded (HasSubscriptions) instead.
        services.Any(d => d.ServiceType == typeof(ISubscriber<ConsumeEvent>)
            && d.ImplementationType == typeof(ConsumeHandler)).Should().BeTrue();
        services.GetSubscriptionManager()!.HasSubscriptions.Should().BeTrue();
    }

    [Fact]
    public void AddSubscriber_ObsoleteAlias_BehavesLikeConsume()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
#pragma warning disable CS0618 // AddSubscriber is the obsolete alias under test
            e.AddSubscriber<AliasEvent, AliasHandler>();
#pragma warning restore CS0618
        });

        services.Any(d => d.ServiceType == typeof(ISubscriber<AliasEvent>)
            && d.ImplementationType == typeof(AliasHandler)).Should().BeTrue();
    }

    public class PublishEvent : ISyncEvent { }
    public class PublishTwiceEvent : ISyncEvent { }
    public class PublishSubEvent : ISyncEvent { }
    public class PublishTransientEvent : ISyncEvent { }
    public class PublishDurableEvent : ISyncEvent { }
    public class SendEvent : ISyncEvent { }
    public class BothEvent : ISyncEvent { }
    public class OutboxBeforeEvent : ISyncEvent { }
    public class OutboxAfterEvent : ISyncEvent { }
    public class PerEventWinsEvent : ISyncEvent { }
    public class ConsumeEvent : ISyncEvent { }
    public class AliasEvent : ISyncEvent { }

    public class ConsumeHandler : ISubscriber<ConsumeEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(ConsumeEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }
    public class AliasHandler : ISubscriber<AliasEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(AliasEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests/RCommon.MassTransit.Tests/RCommon.MassTransit.Tests.csproj --filter "FullyQualifiedName~MassTransitEventHandlingVerbsTests"`
Expected: compile failure (`Publish`/`Send`/`Consume`/`UseRCommonOutbox` do not exist).

- [ ] **Step 3: Implement the verbs**

Edit `Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs`. Add these usings if missing: `using RCommon.EventHandling.Routing;`, `using RCommon.MassTransit.Producers;`. Replace the existing `AddSubscriber` with the obsolete alias + `Consume`, and add `Publish`/`Send`/`UseRCommonOutbox`:

```csharp
/// <summary>
/// Declares that <typeparamref name="TEvent"/> is published via MassTransit fan-out (<c>IBus.Publish</c>).
/// Registers <see cref="PublishWithMassTransitEventProducer"/> (idempotent), records the event→producer
/// subscription, and returns a handle so the route can be made durable via <c>.UseOutbox("store")</c>.
/// </summary>
public static IEventRouteHandle Publish<TEvent>(this IMassTransitEventHandlingBuilder builder)
    where TEvent : class, ISerializableEvent
{
    builder.AddProducer<PublishWithMassTransitEventProducer>();
    builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
    return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
}

/// <summary>
/// Declares that <typeparamref name="TEvent"/> is sent point-to-point via MassTransit (<c>IBus.Send</c>).
/// Registers <see cref="SendWithMassTransitEventProducer"/> (idempotent), records the subscription, and
/// returns a handle for <c>.UseOutbox("store")</c>.
/// </summary>
public static IEventRouteHandle Send<TEvent>(this IMassTransitEventHandlingBuilder builder)
    where TEvent : class, ISerializableEvent
{
    builder.AddProducer<SendWithMassTransitEventProducer>();
    builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
    return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
}

/// <summary>
/// Sets a builder-level default RCommon outbox store (recipe 2a) for every outbound route on this builder
/// that has no explicit per-event <c>.UseOutbox()</c>. Order-independent; per-event overrides win.
/// </summary>
public static IMassTransitEventHandlingBuilder UseRCommonOutbox(this IMassTransitEventHandlingBuilder builder, string dataStoreName)
{
    builder.Services.ApplyBuilderOutboxDefault(builder.GetType(), dataStoreName);
    return builder;
}

/// <summary>
/// Registers an inbound MassTransit consumer for <typeparamref name="TEvent"/> handled by
/// <typeparamref name="TEventHandler"/>, and records the event→producer subscription for routing.
/// </summary>
public static void Consume<TEvent, TEventHandler>(this IMassTransitEventHandlingBuilder builder)
    where TEvent : class, ISerializableEvent
    where TEventHandler : class, ISubscriber<TEvent>
{
    builder.Services.AddTransient<ISubscriber<TEvent>, TEventHandler>();
    builder.AddConsumer<MassTransitEventHandler<TEvent>>();
    builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
}

/// <summary>
/// Obsolete alias for <see cref="Consume{TEvent,TEventHandler}"/>, retained for continuity (spec AC-17).
/// </summary>
[System.Obsolete("Use Consume<TEvent, TEventHandler>() instead. AddSubscriber is retained as a forwarding alias (AC-17).")]
public static void AddSubscriber<TEvent, TEventHandler>(this IMassTransitEventHandlingBuilder builder)
    where TEvent : class, ISerializableEvent
    where TEventHandler : class, ISubscriber<TEvent>
    => builder.Consume<TEvent, TEventHandler>();
```

(`MassTransitEventHandler<TEvent>` and the `AddConsumer<>` call are exactly what the old `AddSubscriber` used — preserve them. Keep the existing `WithEventHandling`/`AddMassTransit`/`AddHostedService`/`AddInstrumentation` members untouched.)

- [ ] **Step 4: Verify no un-suppressed obsolete call sites in the MassTransit test project**

Run `grep -rn "AddSubscriber<" Tests/RCommon.MassTransit.Tests`. Expected: only the deliberate alias test in the new file (which is `#pragma`-suppressed). If any other hit exists, switch it to `Consume<...>`. Confirm the build reports no un-suppressed CS0618.

- [ ] **Step 5: Run to verify PASS**

Run: `dotnet test Tests/RCommon.MassTransit.Tests/RCommon.MassTransit.Tests.csproj --filter "Category!=Integration"`
Expected: all tests PASS (new verbs + pre-existing tests). Output clean.

- [ ] **Step 6: Commit**

```bash
git add Src/RCommon.MassTransit/MassTransitEventHandlingBuilderExtensions.cs Tests/RCommon.MassTransit.Tests/
git commit -m "feat(masstransit): add Publish/Send/Consume/UseRCommonOutbox verbs; AddSubscriber->[Obsolete] alias (AC-12/13/17)"
```

---

## Task 2: Wolverine broker verbs

**Files:**
- Modify: `Src/RCommon.Wolverine/WolverineEventHandlingBuilderExtensions.cs`
- Test: `Tests/RCommon.Wolverine.Tests/WolverineEventHandlingVerbsTests.cs`

Same shape as Task 1, using `IWolverineEventHandlingBuilder`, `PublishWithWolverineEventProducer` / `SendWithWolverineEventProducer`. Note the current Wolverine `AddSubscriber` has TWO overloads (plain + factory) and constrains `TEvent : class` (not `ISerializableEvent`). Preserve that: `Consume` gets a plain + a factory overload; keep the `TEvent : class` constraint on `Consume`/`AddSubscriber`. For `Publish`/`Send`, constrain `TEvent : class, ISerializableEvent` (the outbound route needs a serializable event) — this is consistent with the other transports.

- [ ] **Step 1: Write the failing tests**

Create `Tests/RCommon.Wolverine.Tests/WolverineEventHandlingVerbsTests.cs`, mirroring the MassTransit verbs test file but with the Wolverine builder + producers. Setup uses `new RCommonBuilder(services)` + `builder.WithEventHandling<WolverineEventHandlingBuilder>(e => ...)`. If Wolverine's `WithEventHandling` requires extra host wiring that the existing `WolverineEventHandlingBuilderTests` sets up differently, follow that file's construction pattern instead. Include the same coverage: Publish/Send producer registration + idempotency, subscription recording, transient-vs-durable, `UseRCommonOutbox` ordering, `Consume<T,H>()` (plain + factory), and the obsolete `AddSubscriber` alias (both overloads) under `#pragma warning disable CS0618`.

**Same C1 caveat as Task 1:** in the `Consume` test, do NOT assert `ShouldProduceEvent(...) == false` — it returns `true` via the backward-compat fallback when no producer is registered. Assert the `ISubscriber<T>`→handler DI binding and `GetSubscriptionManager()!.HasSubscriptions == true` instead.

- [ ] **Step 2: Run to verify they fail**

Run: `dotnet test Tests/RCommon.Wolverine.Tests/RCommon.Wolverine.Tests.csproj --filter "FullyQualifiedName~WolverineEventHandlingVerbsTests"`
Expected: compile failure.

- [ ] **Step 3: Implement the verbs**

Edit `Src/RCommon.Wolverine/WolverineEventHandlingBuilderExtensions.cs`. Add usings `using RCommon.EventHandling.Routing;`, `using RCommon.Wolverine.Producers;`, `using RCommon.Models.Events;`. Implement:

```csharp
public static IEventRouteHandle Publish<TEvent>(this IWolverineEventHandlingBuilder builder)
    where TEvent : class, ISerializableEvent
{
    builder.AddProducer<PublishWithWolverineEventProducer>();
    builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
    return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
}

public static IEventRouteHandle Send<TEvent>(this IWolverineEventHandlingBuilder builder)
    where TEvent : class, ISerializableEvent
{
    builder.AddProducer<SendWithWolverineEventProducer>();
    builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
    return builder.Services.RecordPublishRoute(builder.GetType(), typeof(TEvent));
}

public static IWolverineEventHandlingBuilder UseRCommonOutbox(this IWolverineEventHandlingBuilder builder, string dataStoreName)
{
    builder.Services.ApplyBuilderOutboxDefault(builder.GetType(), dataStoreName);
    return builder;
}

public static void Consume<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder)
    where TEvent : class
    where TEventHandler : class, ISubscriber<TEvent>
{
    builder.Services.AddScoped<ISubscriber<TEvent>, TEventHandler>();
    builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
}

public static void Consume<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
    where TEvent : class
    where TEventHandler : class, ISubscriber<TEvent>
{
    builder.Services.TryAddScoped(getSubscriber);
    builder.Services.GetSubscriptionManager()?.AddSubscription(builder.GetType(), typeof(TEvent));
}

[System.Obsolete("Use Consume<TEvent, TEventHandler>() instead. AddSubscriber is retained as a forwarding alias (AC-17).")]
public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder)
    where TEvent : class
    where TEventHandler : class, ISubscriber<TEvent>
    => builder.Consume<TEvent, TEventHandler>();

[System.Obsolete("Use Consume<TEvent, TEventHandler>(Func<IServiceProvider, TEventHandler>) instead. Retained as a forwarding alias (AC-17).")]
public static void AddSubscriber<TEvent, TEventHandler>(this IWolverineEventHandlingBuilder builder, Func<IServiceProvider, TEventHandler> getSubscriber)
    where TEvent : class
    where TEventHandler : class, ISubscriber<TEvent>
    => builder.Consume<TEvent, TEventHandler>(getSubscriber);
```

Keep the `TryAddScoped(getSubscriber)` factory behaviour identical to the current code. (`TryAddScoped` for the factory overload comes from `Microsoft.Extensions.DependencyInjection.Extensions` — that using is already present.)

- [ ] **Step 4: Verify no un-suppressed obsolete call sites in the Wolverine test project**

Run `grep -rn "AddSubscriber<" Tests/RCommon.Wolverine.Tests`. Expected: only the deliberate alias test(s) in the new file (pragma-suppressed). Switch any other hit to `Consume<...>`.

- [ ] **Step 5: Run to verify PASS**

Run: `dotnet test Tests/RCommon.Wolverine.Tests/RCommon.Wolverine.Tests.csproj --filter "Category!=Integration"`
Expected: all PASS, clean output.

- [ ] **Step 6: Commit**

```bash
git add Src/RCommon.Wolverine/WolverineEventHandlingBuilderExtensions.cs Tests/RCommon.Wolverine.Tests/
git commit -m "feat(wolverine): add Publish/Send/Consume/UseRCommonOutbox verbs; AddSubscriber->[Obsolete] alias (AC-12/13/17)"
```

---

## Task 3: Full fast-lane verification

**Files:** none (verification only)

- [ ] **Step 1: Release build**

Run: `dotnet build Src/RCommon.sln -c Release`
Expected: 0 errors. No NEW warnings beyond the pre-existing set (the migrated call sites should mean no new CS0618 warnings except the pragma-suppressed alias tests).

- [ ] **Step 2: Full fast lane**

Run: `dotnet test Src/RCommon.sln --filter "Category!=Integration"`
Expected: all projects PASS, 0 failures.

- [ ] **Step 3: (No commit)** — report results to the controller.

---

## Notes / out of scope for Phase 4a

- `UseBrokerOutbox` (MassTransit native EF outbox, recipe 2b) and Wolverine's guided-`NotSupportedException` `UseBrokerOutbox` are **Phase 4b**.
- The AC-15 Podman recipe-2b atomicity integration test is **Phase 4c**.
- No inbox-idempotency wiring is added here — `Consume` preserves the existing consumer behaviour; inbox dedup is existing poller/consumer infrastructure.
- Durable broker routes (`Publish<T>().UseOutbox("store")` / `UseRCommonOutbox`) reuse the Phase-3a outbox pipeline with ZERO new pipeline code — the verb only records the route + durability in the registry, exactly as the bus and mediator verbs do.
