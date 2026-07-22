# Event Handling 3.2.0 — Phase 2: Pipeline Reorder + FIFO Dispatch Queue + Ordering (AC-3–AC-6) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorder the unit-of-work commit pipeline so in-process domain handlers run **pre-commit, inside the transaction, in raise-order**, model in-process dispatch as a **single FIFO queue drained to empty** (replacing the re-harvest loop), and add a **cascade generation cycle-breaker** (default 16, fail-loud) — delivering AC-3, AC-4, AC-6 end-to-end on the in-process (non-outbox) path and AC-5's transactional-translation ordering guarantee at the pipeline level.

**Architecture:** Today `UnitOfWork.CommitAsync` runs *persist-outbox (pre-commit) → commit → dispatch (post-commit)*, and in-process dispatch is a re-harvest loop (`InMemoryTransactionalEventRouter.RouteEventsAsync()` snapshots the queue, routes, then dequeues with retry). This phase (a) introduces a new pre-commit tracker step `DispatchDomainEventsAsync` invoked **first** in `CommitAsync`, before `PersistEventsAsync` and before `TransactionScope.Complete()`; (b) reshapes `InMemoryTransactionalEventRouter` into a single FIFO drain where each event carries a **cascade generation**, `ISyncEvent`/untagged events dispatch sequentially in raise-order and a contiguous run of same-generation `IAsyncEvent`s is awaited concurrently, and events raised mid-dispatch enqueue to the *same* FIFO at generation+1; (c) adds `EventHandlingOptions.MaxDispatchGenerations` (default 16) and throws a descriptive `DispatchGenerationLimitException` when a cascade exceeds it; and (d) wires the in-memory tracker to seed the FIFO from tracked-entity graphs and subscribe to each entity's `TransactionalEventAdded` so handler-raised events flow into the same drain.

**Scope decision (mechanism-first — confirmed with the design owner 2026-07-22):** The route map that decides, per event, "in-process domain (pre-commit dispatch)" vs "durable (outbox → relay)" is **Phase 3's** fluent API. Phase 2 builds the reorder + FIFO + cycle-breaker **machinery** and applies pre-commit dispatch to the **in-process (non-outbox) tracker path only**. The **outbox tracker path is left exactly as Phase 1** (`PersistEventsAsync` pre-commit → commit → `EmitTransactionalEventsAsync` relay post-commit); its `DispatchDomainEventsAsync` is a no-op. AC-3/AC-4/AC-6 are proven end-to-end on the in-process path; AC-5 (transactional translation) is proven at the **pipeline-ordering** level (dispatch runs before persist runs before commit), with the full single-host domain+durable separation deferred to Phase 3. This keeps Phase 1's cross-datastore outbox test green and does not front-run the route map.

**Tech Stack:** .NET 10 (Src multi-targets net8/9/10; Tests are net10.0), xUnit 2.9.3, AwesomeAssertions 7.2.1 (`FluentAssertions` namespace), Moq 4.20.72. No new packages.

**Spec:** `docs/specs/event-handling/event-handling.md` — this phase implements **AC-3** (pre-commit ordered dispatch), **AC-4** (FIFO drain + cycle-breaker), **AC-5** (transactional translation — pipeline-ordering proof), **AC-6** (atomicity / all-or-nothing). **Design:** `docs/superpowers/specs/2026-07-22-event-handling-outbox-recipes-design.md` §2 (the reordered pipeline + FIFO dispatch queue). **Branch:** `feature/event-handling-outbox-recipes` (Phases 0 and 1 already on this branch; do not switch branches).

---

## Scope & boundaries

**In scope (AC-3–AC-6, mechanism-first):**
- New `EventHandlingOptions.MaxDispatchGenerations` (default 16) + default registration.
- New `DispatchGenerationLimitException` (fail-loud cycle-breaker).
- `InMemoryTransactionalEventRouter` reshaped into a single generation-tracked FIFO drain replacing the re-harvest/`RemoveEvents` loop.
- New `IEntityEventTracker.DispatchDomainEventsAsync` pre-commit step; in-memory implementation (seed + subscribe + drain); outbox implementation is a no-op.
- `UnitOfWork.CommitAsync` reordered: `DispatchDomainEventsAsync` (pre-commit) → `PersistEventsAsync` (pre-commit) → `Complete()` → `EmitTransactionalEventsAsync` (post-commit).
- Tests: raise-order sequencing, async concurrency, cascade + cycle-breaker, pre-commit dispatch, atomicity/rollback, and the pipeline-order guarantee.

**Explicitly out of scope (later phases — do NOT build here):**
- The per-event route map / fluent verbs (`Publish`/`Send`/`Consume`/`AddSubscriber`/`.UseOutbox`/`UseRCommonOutbox`) and the domain-vs-durable split in a *single outbox host* (**Phase 3**, AC-12/13/21). In Phase 2 the outbox tracker path is unchanged.
- MassTransit/Wolverine wrappers + recipes (**Phase 4**), examples/e2e (**Phase 5**), docs/migration guide (**Phase 6**). The pre-vs-post-commit semantic change is *noted* here but the migration guide is written in Phase 6.
- First-class metrics (AC-18: dispatch-queue depth / max cascade generation reached) — the `Meter` lands in a later phase; Phase 2 only exposes the behavior, not the meter.
- Changing `ISyncEvent`/`IAsyncEvent` definitions or adding validation that an event is both.

**Key simplifying decisions (YAGNI):**
- **Generations form contiguous FIFO blocks by construction.** Seeds are generation 0; a handler dispatched at generation *n* enqueues raised events at *n+1* to the back of the queue. Because generation *n* is fully drained before generation *n+1* is reached, same-generation events are naturally contiguous — so "maximal contiguous run of same-generation async events" is well-defined without a separate per-generation data structure.
- **The subscription is attached only for the duration of the drain** (in `DispatchDomainEventsAsync`), over the full entity graph traversed at seed time, and removed in a `finally`. Normal pre-commit aggregate work (events raised before `DispatchDomainEventsAsync`) is captured by the seed traversal; events raised *during* the drain by handlers mutating a seeded entity are captured by the subscription. **Documented Phase-2 limitation:** an event raised mid-dispatch on a *brand-new* entity that a handler creates and did not exist in the graph at seed time is captured only if the handler raises it via `IEventRouter.AddTransactionalEvent` (router-added path) — not via `AddLocalEvent` on the new entity. This is an accepted edge for Phase 2; Phase 3's route map revisits mid-dispatch entity tracking.
- **Generation counting:** generation 0 = seeds. A cascade is "too deep" when an event would be enqueued at generation `> MaxDispatchGenerations`. With the default of 16, generations 0..16 are allowed; enqueuing generation 17 throws. This is asserted explicitly in the cycle-breaker test so the boundary is unambiguous.

---

## File structure

**Core event-handling (`Src/RCommon.Core/EventHandling/`)**
- Create `EventHandlingOptions.cs` (namespace `RCommon.EventHandling`) — `int MaxDispatchGenerations { get; set; } = 16`.
- Create `Producers/DispatchGenerationLimitException.cs` (namespace `RCommon.EventHandling.Producers`) — `: GeneralException`, records the limit.
- Modify `Producers/InMemoryTransactionalEventRouter.cs` — inject `IOptions<EventHandlingOptions>`; queue carries `(ISerializableEvent Event, int Generation)`; reshape the no-arg `RouteEventsAsync()` into the generation-tracked FIFO drain; enqueue at gen 0 (seed) or `_currentGeneration + 1` (mid-drain); throw `DispatchGenerationLimitException` past the limit; delete `RemoveEvents`/`RemoveEvent`.
- Modify `RCommonBuilder.cs:47` — register `Services.AddOptions<EventHandlingOptions>();` alongside the router registration so the default (16) always exists.

**Entities (`Src/RCommon.Entities/`)**
- Modify `IEntityEventTracker.cs` — add `Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)`.
- Modify `InMemoryEntityEventTracker.cs` — implement `DispatchDomainEventsAsync` (seed the router from tracked-entity graphs, subscribe each graph entity's `TransactionalEventAdded` → `_eventRouter.AddTransactionalEvent`, drain via `RouteEventsAsync()`, unsubscribe in `finally`); make `EmitTransactionalEventsAsync` a no-op (dispatch moved pre-commit) that returns `true`.

**Persistence (`Src/RCommon.Persistence/`)**
- Modify `Outbox/OutboxEntityEventTracker.cs` — implement `DispatchDomainEventsAsync` as a no-op (`Task.CompletedTask`); `PersistEventsAsync`/`EmitTransactionalEventsAsync` unchanged (Phase 1 behavior preserved).
- Modify `Transactions/UnitOfWork.cs:129-164` — reorder `CommitAsync`: call `DispatchDomainEventsAsync` first (pre-commit), then `PersistEventsAsync` (pre-commit), then `Complete()`/dispose, then `EmitTransactionalEventsAsync` (post-commit). Update the comments to describe the 4-step order.

**Tests**
- Create `Tests/RCommon.Core.Tests/EventHandlingOptionsTests.cs`.
- Create `Tests/RCommon.Core.Tests/DispatchGenerationLimitExceptionTests.cs`.
- Modify `Tests/RCommon.Core.Tests/InMemoryTransactionalEventRouterTests.cs` — add FIFO raise-order, cascade single-pass, async-concurrency (rendezvous), and cycle-breaker tests; update any test that depended on `RemoveEvents` internals.
- Create `Tests/RCommon.Persistence.Tests/InMemoryEntityEventTrackerDispatchTests.cs` (or extend existing tracker tests) — pre-commit dispatch, handler-raised cascade, subscription capture.
- Modify `Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs` — assert `DispatchDomainEventsAsync` is a no-op (does not touch store/router).
- Modify `Tests/RCommon.Persistence.Tests/UnitOfWorkCommitAsyncTests.cs` — update the ordering test to `DispatchDomainEventsAsync` → `PersistEventsAsync` → `EmitTransactionalEventsAsync`; add atomicity (handler throws → rollback, no persist, no emit) test.
- Create `Tests/RCommon.Persistence.Tests/PreCommitDispatchPipelineTests.cs` — holistic proof through a real `UnitOfWork` + `InMemoryEntityEventTracker` + `InMemoryTransactionalEventRouter` + `InMemoryEventBus`: raise-order pre-commit, cascade drain, cycle-breaker, rollback-on-throw. (Plain unit test — no containers, runs in the fast lane; **no** `[Trait("Category","Integration")]`.)

> **Interface-break discipline.** Adding `DispatchDomainEventsAsync` to `IEntityEventTracker` (Task 4) breaks both implementations (`InMemoryEntityEventTracker`, `OutboxEntityEventTracker`) and every `Mock<IEntityEventTracker>` / test double at once. Task 4 modifies the interface **and** both implementations **and** the affected test doubles in a single commit so the solution never lands non-building. Verify with a full `dotnet build Src/RCommon.sln` before committing Task 4.

> **CI trait convention (unchanged from Phases 0/1).** Every new/modified test in this phase is a plain unit test that runs in the fast lane (`dotnet test Src/RCommon.sln --filter "Category!=Integration"`). Do **not** add `[Trait("Category","Integration")]` to any Phase 2 test — none of them start containers.

---

## Recommended task order

1. `EventHandlingOptions.MaxDispatchGenerations` (+ default registration).
2. `DispatchGenerationLimitException`.
3. FIFO drain + generation cycle-breaker in `InMemoryTransactionalEventRouter`.
4. `IEntityEventTracker.DispatchDomainEventsAsync` + in-memory dispatch + outbox no-op (interface break, single commit).
5. Reorder `UnitOfWork.CommitAsync` + ordering/atomicity tests.
6. Holistic pre-commit dispatch pipeline test (AC-3/AC-4/AC-6 through real units).

---

## Task 1: `EventHandlingOptions.MaxDispatchGenerations` (AC-4 config knob)

**Files:**
- Create: `Src/RCommon.Core/EventHandling/EventHandlingOptions.cs`
- Modify: `Src/RCommon.Core/RCommonBuilder.cs:47`
- Test: `Tests/RCommon.Core.Tests/EventHandlingOptionsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// Tests/RCommon.Core.Tests/EventHandlingOptionsTests.cs
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
using Xunit;

namespace RCommon.Core.Tests;

public class EventHandlingOptionsTests
{
    [Fact]
    public void MaxDispatchGenerations_Defaults_To_16()
    {
        var options = new EventHandlingOptions();
        options.MaxDispatchGenerations.Should().Be(16);
    }

    [Fact]
    public void MaxDispatchGenerations_Is_Configurable_Via_Options()
    {
        var services = new ServiceCollection();
        services.AddOptions<EventHandlingOptions>().Configure(o => o.MaxDispatchGenerations = 4);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<EventHandlingOptions>>()
            .Value.MaxDispatchGenerations.Should().Be(4);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~EventHandlingOptionsTests"`
Expected: FAIL — `EventHandlingOptions` does not exist (compile error).

- [ ] **Step 3: Write minimal implementation**

```csharp
// Src/RCommon.Core/EventHandling/EventHandlingOptions.cs
namespace RCommon.EventHandling
{
    /// <summary>
    /// Options controlling in-process event dispatch behaviour.
    /// </summary>
    public class EventHandlingOptions
    {
        /// <summary>
        /// The maximum number of cascade generations allowed during a single pre-commit domain-event
        /// drain before the cycle-breaker fails loud. A generation is the cascade depth: the event that
        /// triggered a handler is generation <c>n</c>; events that handler raises are generation
        /// <c>n + 1</c>. A wide fan-out at one generation is fine — only unbounded chains (A→B→A…) trip
        /// this limit. Seeds are generation 0; enqueuing an event beyond this limit throws
        /// <see cref="Producers.DispatchGenerationLimitException"/>. Default: 16.
        /// </summary>
        public int MaxDispatchGenerations { get; set; } = 16;
    }
}
```

Register the default alongside the router (so it always exists even with no explicit config):

```csharp
// Src/RCommon.Core/RCommonBuilder.cs — right after Services.AddScoped<IEventRouter, InMemoryTransactionalEventRouter>();
Services.AddOptions<EventHandlingOptions>();
```

(Add `using RCommon.EventHandling;` if not already present in `RCommonBuilder.cs`.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~EventHandlingOptionsTests"`
Expected: PASS (2/2).

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Core/EventHandling/EventHandlingOptions.cs Src/RCommon.Core/RCommonBuilder.cs Tests/RCommon.Core.Tests/EventHandlingOptionsTests.cs
git commit -m "feat(events): add EventHandlingOptions.MaxDispatchGenerations (default 16) (AC-4)"
```

---

## Task 2: `DispatchGenerationLimitException` (AC-4 fail-loud)

**Files:**
- Create: `Src/RCommon.Core/EventHandling/Producers/DispatchGenerationLimitException.cs`
- Test: `Tests/RCommon.Core.Tests/DispatchGenerationLimitExceptionTests.cs`

Model on the existing `EventProductionException : GeneralException` (same folder/namespace).

- [ ] **Step 1: Write the failing test**

```csharp
// Tests/RCommon.Core.Tests/DispatchGenerationLimitExceptionTests.cs
using FluentAssertions;
using RCommon.EventHandling.Producers;
using Xunit;

namespace RCommon.Core.Tests;

public class DispatchGenerationLimitExceptionTests
{
    [Fact]
    public void Records_The_Limit_And_Describes_The_Cascade()
    {
        var ex = new DispatchGenerationLimitException(16);

        ex.MaxDispatchGenerations.Should().Be(16);
        ex.Message.Should().Contain("16");
        ex.Message.Should().Contain("cascade", Exactly.Ignore()); // descriptive, mentions the runaway cascade
    }
}
```

> If `Exactly.Ignore()` is not available in AwesomeAssertions, use `ex.Message.ToLowerInvariant().Should().Contain("cascade");`.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~DispatchGenerationLimitExceptionTests"`
Expected: FAIL — type does not exist.

- [ ] **Step 3: Write minimal implementation**

```csharp
// Src/RCommon.Core/EventHandling/Producers/DispatchGenerationLimitException.cs
using System;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Thrown when the pre-commit domain-event drain exceeds the configured cascade generation limit
    /// (<see cref="EventHandlingOptions.MaxDispatchGenerations"/>). A generation is the cascade depth of
    /// events-raising-events; exceeding the limit indicates an unbounded cascade (e.g. A→B→A…). This is a
    /// fail-loud safety net, not the drain's termination mechanism (empty-queue is).
    /// </summary>
    public class DispatchGenerationLimitException : GeneralException
    {
        /// <summary>The configured maximum number of cascade generations that was exceeded.</summary>
        public int MaxDispatchGenerations { get; }

        public DispatchGenerationLimitException(int maxDispatchGenerations)
            : base($"The pre-commit domain-event dispatch cascade exceeded the configured limit of " +
                   $"{maxDispatchGenerations} generation(s). This usually means a handler cycle " +
                   $"(e.g. A raises B, B raises A). Break the cycle or raise " +
                   $"{nameof(EventHandlingOptions)}.{nameof(EventHandlingOptions.MaxDispatchGenerations)} " +
                   $"if the cascade is legitimately deep.")
        {
            MaxDispatchGenerations = maxDispatchGenerations;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~DispatchGenerationLimitExceptionTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Core/EventHandling/Producers/DispatchGenerationLimitException.cs Tests/RCommon.Core.Tests/DispatchGenerationLimitExceptionTests.cs
git commit -m "feat(events): add DispatchGenerationLimitException cycle-breaker (AC-4)"
```

---

## Task 3: FIFO drain + generation cycle-breaker in `InMemoryTransactionalEventRouter` (AC-3, AC-4)

**Files:**
- Modify: `Src/RCommon.Core/EventHandling/Producers/InMemoryTransactionalEventRouter.cs`
- Test: `Tests/RCommon.Core.Tests/InMemoryTransactionalEventRouterTests.cs`

**Design of the drain (single FIFO, generation-tracked):**
- Field: `private readonly ConcurrentQueue<(ISerializableEvent Event, int Generation)> _queue`.
- Field: `private volatile bool _draining;` and `private int _currentGeneration;`.
- Constructor gains `IOptions<EventHandlingOptions> eventHandlingOptions`; store `_maxGenerations = eventHandlingOptions.Value.MaxDispatchGenerations`.
- `AddTransactionalEvent(evt)`: compute `gen = _draining ? _currentGeneration + 1 : 0`; if `gen > _maxGenerations` throw `DispatchGenerationLimitException(_maxGenerations)`; else enqueue `(evt, gen)`.
- `RouteEventsAsync(CancellationToken)` (the no-arg drain) replaces the old re-harvest loop:
  1. Set `_draining = true` in a `try`, reset in `finally`.
  2. Resolve `eventProducers = _serviceProvider.GetServices<IEventProducer>().ToList()` once (keep the existing zero-producer warning behavior).
  3. Loop while `_queue.TryPeek(out var head)`:
     - `_currentGeneration = head.Generation;`
     - **Always dequeue the head first** (guarantees forward progress — the loop can never spin on a peeked-but-not-consumed head).
     - If `head.Event is IAsyncEvent`: after consuming the head, extend it into a **maximal contiguous run** by dequeuing each subsequent event while `Generation == head.Generation && Event is IAsyncEvent`; dispatch the whole run concurrently (`ProduceAsyncEvents(run, eventProducers, ct)`).
     - Else (`ISyncEvent` or untagged→sync): dispatch the single consumed head via `ProduceSyncEvents(new[]{ headEvent }, eventProducers, ct)`.
  4. Keep the existing `try/catch(EventProductionException)` / `catch(Exception)` wrapping semantics from the batch `RouteEventsAsync(IEnumerable)` so a handler exception still surfaces as before (and, crucially, propagates out so `CommitAsync` can roll back — AC-6). **`DispatchGenerationLimitException` must propagate unwrapped.** The limit check lives in `AddTransactionalEvent`, which handlers call **on their own threads during a concurrent `ProduceAsyncEvents` run** — so the exception can arrive at the drain loop already unwrapped via `Task.WhenAll`. The `catch(Exception)` clause therefore **must exclude it**: write `catch (Exception ex) when (ex is not DispatchGenerationLimitException)` (and likewise do not let `ProduceAsyncEvents`/`ProduceSyncEvents` wrap it). Otherwise a limit trip inside an async handler gets re-wrapped as `EventProductionException` and the cycle-breaker's type is lost.
- Delete `RemoveEvents` and `RemoveEvent` (the retry-dequeue helpers) — the drain dequeues directly.
- Keep the batch `RouteEventsAsync(IEnumerable<ISerializableEvent>, ct)` method (used by `OutboxEventRouter`/direct callers) **as-is** — it does not participate in generation tracking.

> **Behavior-change note (do NOT "harmonize" the two methods).** The retained batch `RouteEventsAsync(IEnumerable)` partitions into *all* sync then *all* async (current lines 51-53), so raise-order `[syncA, asyncB, syncC]` dispatches as `[syncA, syncC, asyncB]`. The new FIFO drain deliberately does **not** do this — it preserves raise-order via per-generation contiguous runs, which is precisely what AC-3 requires. AC-3's sequencing is delivered **only** by the new drain, not the batch overload; leave the batch overload's partition logic alone (its callers don't need raise-order).

> **Contiguous-run helper:** after consuming the head, dequeue into a `List<ISerializableEvent> run` (seeded with the head event) while `_queue.TryPeek(out var next) && next.Generation == head.Generation && next.Event is IAsyncEvent`, calling `_queue.TryDequeue(...)` for each. This preserves raise-order within the async run and stops at the first sync event or generation change.

> **Thread-safety (verified during plan review):** during a concurrent async run the drain loop sets `_currentGeneration` once *before* dispatching and does not mutate it while awaiting `Task.WhenAll`, so concurrent handler reads of `_currentGeneration` in `AddTransactionalEvent` are stable; `_draining` is `volatile`; `ConcurrentQueue` handles concurrent enqueue. This is sound — do not add locking.

> **Call-site break (guaranteed, not conditional):** adding the 4th constructor parameter (`IOptions<EventHandlingOptions>`) breaks **every** `new InMemoryTransactionalEventRouter(...)` in `InMemoryTransactionalEventRouterTests.cs` (~18 three-arg call sites) plus the null-arg guard tests. Step 3 MUST update all of them (pass `Options.Create(new EventHandlingOptions())`, or a per-test override) and add a null-check guard test for the new `IOptions` param following the existing guard-test pattern, so the Core test project compiles.

- [ ] **Step 1: Write the failing tests** (add to `InMemoryTransactionalEventRouterTests.cs`)

Sketches — follow the existing test file's fixture/producer-fake conventions (it already fakes `IEventProducer` + `EventSubscriptionManager`). Construct the router with an `IOptions<EventHandlingOptions>` (use `Options.Create(new EventHandlingOptions{ MaxDispatchGenerations = ... })`).

```csharp
// (a) Sync events drain in raise-order (FIFO)
[Fact]
public async Task Drain_Dispatches_Sync_Events_In_Raise_Order()
{
    // Arrange: a recording producer that appends each event to a shared list.
    // Enqueue three ISyncEvent instances E1, E2, E3 via AddTransactionalEvent.
    // Act: await router.RouteEventsAsync();
    // Assert: recorded order == [E1, E2, E3].
}

// (b) A handler raising an event mid-dispatch is drained in the same pass (single FIFO)
[Fact]
public async Task Drain_Processes_Events_Raised_By_Handlers_In_Same_Pass()
{
    // Arrange: producer for E1 raises E2 (via router.AddTransactionalEvent) when it handles E1.
    // Enqueue E1 (generation 0).
    // Act: await router.RouteEventsAsync();
    // Assert: both E1 and E2 were dispatched; queue is empty at the end.
}

// (c) Cascade exceeding the generation limit fails loud (SYNC cascade)
[Fact]
public async Task Drain_Throws_DispatchGenerationLimitException_On_Runaway_Sync_Cascade()
{
    // Arrange: MaxDispatchGenerations = 3; a producer that ALWAYS raises a new sync event
    //          (each raise => generation+1) => unbounded cascade.
    // Act: Func<Task> act = () => router.RouteEventsAsync();
    // Assert: await act.Should().ThrowAsync<DispatchGenerationLimitException>()
    //         .Which.MaxDispatchGenerations.Should().Be(3);
}

// (c2) Cascade limit fails loud via an ASYNC cascade too — proves the exception is NOT re-wrapped
//      as EventProductionException when it surfaces through Task.WhenAll on a handler thread.
[Fact]
public async Task Drain_Throws_DispatchGenerationLimitException_On_Runaway_Async_Cascade()
{
    // Arrange: MaxDispatchGenerations = 3; a producer whose async handler ALWAYS raises a new
    //          IAsyncEvent (each raise => generation+1) => unbounded async cascade.
    // Act: Func<Task> act = () => router.RouteEventsAsync();
    // Assert: await act.Should().ThrowAsync<DispatchGenerationLimitException>();  // NOT EventProductionException
}

// (d) Async events in a contiguous run are awaited concurrently (rendezvous — deterministic)
[Fact]
public async Task Drain_Awaits_A_Run_Of_Async_Events_Concurrently()
{
    // Arrange: two IAsyncEvent instances whose handlers each await a shared TaskCompletionSource
    //          that only completes once BOTH handlers have started (a rendezvous / CountdownEvent-style
    //          barrier for 2). If dispatched sequentially, the first handler would block forever.
    // Act: await router.RouteEventsAsync().WaitAsync(TimeSpan.FromSeconds(5));
    // Assert: completes without timing out => both handlers ran concurrently.
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~InMemoryTransactionalEventRouterTests"`
Expected: **FAIL at compile time first** — the new tests reference the 4-arg constructor (`Options.Create(...)`) and `DispatchGenerationLimitException` (from Tasks 1-2), which the current router/test call sites don't satisfy. Do NOT expect a clean runtime red for the cascade tests under the old code — the old re-harvest loop has no generation concept and a self-raising producer would **loop forever and hang the test run**, not fail. Getting the project to compile (Step 3's call-site updates) is what turns the cascade/concurrency tests into real reds/greens.

- [ ] **Step 3: Implement the drain** (per the design above). Update the constructor, add the fields, reshape `RouteEventsAsync()`, update `AddTransactionalEvent`, delete `RemoveEvents`/`RemoveEvent`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj --filter "FullyQualifiedName~InMemoryTransactionalEventRouterTests"`
Expected: PASS — new tests green **and** all pre-existing router tests still green.

Then run the whole Core test project to catch fallout from the constructor change:
Run: `dotnet test Tests/RCommon.Core.Tests/RCommon.Core.Tests.csproj`
Expected: PASS. (If other tests construct the router directly, update them to pass `Options.Create(new EventHandlingOptions())`.)

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Core/EventHandling/Producers/InMemoryTransactionalEventRouter.cs Tests/RCommon.Core.Tests/InMemoryTransactionalEventRouterTests.cs
git commit -m "feat(events): FIFO generation-tracked drain replaces re-harvest loop (AC-3, AC-4)"
```

---

## Task 4: `IEntityEventTracker.DispatchDomainEventsAsync` — pre-commit dispatch (AC-3)

> **Single commit — interface break.** This task adds a method to `IEntityEventTracker` and updates BOTH implementations plus all test doubles in one commit.

**Files:**
- Modify: `Src/RCommon.Entities/IEntityEventTracker.cs`
- Modify: `Src/RCommon.Entities/InMemoryEntityEventTracker.cs`
- Modify: `Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs`
- Test: `Tests/RCommon.Persistence.Tests/InMemoryEntityEventTrackerDispatchTests.cs` (create), `Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs` (modify)

**In-memory implementation of `DispatchDomainEventsAsync`:**
```csharp
public async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
{
    // Subscribe to every entity in every tracked graph so events RAISED DURING the drain (by handlers
    // mutating a seeded entity) flow into the SAME router FIFO at generation+1. Seed the FIFO from the
    // events already present at commit time. Drain to empty. Always unsubscribe.
    var subscribed = new List<IBusinessEntity>();
    void Handler(object? sender, TransactionalEventsChangedEventArgs args)
        => _eventRouter.AddTransactionalEvent(args.EventData);

    try
    {
        foreach (var (entity, _) in _trackedPairs)
        {
            foreach (var graphEntity in entity.TraverseGraphFor<IBusinessEntity>())
            {
                graphEntity.TransactionalEventAdded += Handler;
                subscribed.Add(graphEntity);
                foreach (var localEvent in graphEntity.LocalEvents)
                {
                    _eventRouter.AddTransactionalEvent(localEvent); // seed => generation 0
                }
            }
        }

        await _eventRouter.RouteEventsAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
        foreach (var entity in subscribed)
        {
            entity.TransactionalEventAdded -= Handler;
        }
    }
}
```
- `EmitTransactionalEventsAsync` on the in-memory tracker becomes a no-op returning `true` (dispatch moved to `DispatchDomainEventsAsync`). Keep the method (interface + outbox use it). Update its XML doc to say dispatch now happens pre-commit in `DispatchDomainEventsAsync`.

> **Note on `TransactionalEventsChangedEventArgs.EventData`:** the property that carries the added `ISerializableEvent` is `EventData` (verified: `TransactionalEventsChangedEventArgs.cs:36`). Use it directly.

> **Subscription-timing invariant (must hold):** the seed loop attaches `+= Handler` to *every* graph entity **and** the entire seed loop completes **before** `RouteEventsAsync()` is awaited. So by the time any handler runs, all seeded entities are already subscribed — an async handler for entity A that raises on entity B mid-drain is safely captured because B was subscribed during seeding. Do not interleave seeding with dispatch. Seeding does not call `AddLocalEvent` (it reads `LocalEvents` and enqueues), so no event is double-enqueued.

**Outbox implementation:** `public Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;` (no-op — Phase-2 mechanism-first; Phase-1 persist/relay behavior preserved). Add an XML-doc line stating pre-commit domain dispatch for outbox-owning datastores is wired by the Phase-3 route map.

- [ ] **Step 1: Write the failing tests**

```csharp
// Tests/RCommon.Persistence.Tests/InMemoryEntityEventTrackerDispatchTests.cs
// (a) DispatchDomainEventsAsync dispatches an entity's LocalEvents pre-commit (routes them through IEventRouter)
// (b) A handler that calls entity.AddLocalEvent during dispatch causes the raised event to be drained too
//     (subscription capture — assert both events routed)
// Use a fake/mock IEventRouter that records AddTransactionalEvent calls and whose RouteEventsAsync
// can invoke a supplied callback to simulate a handler raising a new event mid-drain.
```

```csharp
// OutboxEntityEventTrackerTests.cs — new test
[Fact]
public async Task DispatchDomainEventsAsync_Is_A_NoOp_For_The_Outbox_Tracker()
{
    // Arrange: OutboxEntityEventTracker with a strict Mock<OutboxEventRouter>/inner tracker.
    // Act: await tracker.DispatchDomainEventsAsync();
    // Assert: no interaction with the outbox router/store (Phase-1 behavior untouched).
}
```

- [ ] **Step 2: Run tests to verify they fail** — `DispatchDomainEventsAsync` not on the interface (compile error).

- [ ] **Step 3: Implement** — add the interface method (with XML doc), the in-memory implementation, the outbox no-op. **Then fix every `Mock<IEntityEventTracker>` and hand-written test double** across the solution (search: `IEntityEventTracker`) so they compile — Moq mocks auto-implement the new member, but any concrete test fake must add it.

- [ ] **Step 4: Verify the whole solution builds, then run the affected tests**

Run: `dotnet build Src/RCommon.sln`
Expected: 0 errors.
Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj`
Expected: PASS (new tracker tests green; Phase-1 tracker/outbox tests still green).

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Entities/IEntityEventTracker.cs Src/RCommon.Entities/InMemoryEntityEventTracker.cs Src/RCommon.Persistence/Outbox/OutboxEntityEventTracker.cs Tests/RCommon.Persistence.Tests/InMemoryEntityEventTrackerDispatchTests.cs Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs
git commit -m "feat(events)!: add IEntityEventTracker.DispatchDomainEventsAsync; in-memory dispatches pre-commit (AC-3)"
```

---

## Task 5: Reorder `UnitOfWork.CommitAsync` (AC-3, AC-5, AC-6)

**Files:**
- Modify: `Src/RCommon.Persistence/Transactions/UnitOfWork.cs:129-164`
- Test: `Tests/RCommon.Persistence.Tests/UnitOfWorkCommitAsyncTests.cs`

**New `CommitAsync` order:**
```csharp
_state = UnitOfWorkState.CommitAttempted;

if (_eventTracker != null)
{
    // Step 1 (PRE-COMMIT, in txn): dispatch in-process domain handlers, ordered, drained to empty.
    // A handler throwing here propagates out BEFORE Complete() => full rollback (AC-6).
    await _eventTracker.DispatchDomainEventsAsync(cancellationToken).ConfigureAwait(false);

    // Step 2 (PRE-COMMIT, in txn): persist outbox-bound events (incl. any a domain handler raised
    // via AddTransactionalEvent) so state + outbox rows commit atomically (AC-5).
    await _eventTracker.PersistEventsAsync(cancellationToken).ConfigureAwait(false);
}

// Step 3: commit (state + outbox rows atomic).
_transactionScope.Complete();
_transactionScope.Dispose();
_transactionScopeDisposed = true;
_state = UnitOfWorkState.Completed;

// Step 4 (POST-COMMIT): relay outbox rows to their producers (best-effort; poller retries).
if (_eventTracker != null)
{
    var dispatched = await _eventTracker.EmitTransactionalEventsAsync(cancellationToken).ConfigureAwait(false);
    if (!dispatched)
    {
        _logger.LogWarning("UnitOfWork {TransactionId}: domain event dispatch returned false.", TransactionId);
    }
}
```

- [ ] **Step 1: Update the existing ordering test + write the atomicity test** (`UnitOfWorkCommitAsyncTests.cs`)

Update `CommitAsync_With_Tracker_Calls_PersistEventsAsync_Before_Commit` → rename to `CommitAsync_Dispatches_Then_Persists_Then_Emits` and set up `DispatchDomainEventsAsync` on the mock to record `"DispatchDomainEventsAsync"`; assert:
```csharp
callOrder.Should().ContainInOrder("DispatchDomainEventsAsync", "PersistEventsAsync", "EmitTransactionalEventsAsync");
```

New atomicity test (AC-6):
```csharp
[Fact]
public async Task CommitAsync_When_PreCommit_Dispatch_Throws_Does_Not_Commit_Or_Persist_Or_Emit()
{
    var mockTracker = new Mock<IEntityEventTracker>();
    // Stub the other two members with non-null completed tasks so that BEFORE the reorder (the red run)
    // the current code path (PersistEventsAsync -> Complete -> EmitTransactionalEventsAsync) does not NRE
    // and mask the real assertion; AFTER the reorder they must never be reached.
    mockTracker.Setup(t => t.PersistEventsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    mockTracker.Setup(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    mockTracker.Setup(t => t.DispatchDomainEventsAsync(It.IsAny<CancellationToken>()))
               .ThrowsAsync(new InvalidOperationException("handler failed"));

    using var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);

    var act = () => uow.CommitAsync();

    await act.Should().ThrowAsync<InvalidOperationException>();
    uow.State.Should().NotBe(UnitOfWorkState.Completed);        // transaction not completed => rolls back
    mockTracker.Verify(t => t.PersistEventsAsync(It.IsAny<CancellationToken>()), Times.Never); // no outbox rows
    mockTracker.Verify(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>()), Times.Never); // no relay
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~UnitOfWorkCommitAsyncTests"`
Expected: FAIL — the renamed ordering test fails because `CommitAsync` does not yet call `DispatchDomainEventsAsync` (so `callOrder` lacks it); the atomicity test fails because pre-reorder `CommitAsync` never invokes `DispatchDomainEventsAsync`, so no exception is thrown and `ThrowAsync<InvalidOperationException>()` fails. (The stubs above keep the pre-reorder path from NRE-ing so the failure is the clean assertion, not a null-task crash.)

- [ ] **Step 3: Reorder `CommitAsync`** per the block above.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~UnitOfWorkCommitAsyncTests"`
Expected: PASS. Then run the full persistence project:
Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj`
Expected: PASS (Phase-1 outbox + UoW tests still green — the outbox path is unchanged).

- [ ] **Step 5: Commit**

```bash
git add Src/RCommon.Persistence/Transactions/UnitOfWork.cs Tests/RCommon.Persistence.Tests/UnitOfWorkCommitAsyncTests.cs
git commit -m "feat(events)!: reorder CommitAsync to dispatch domain handlers pre-commit (AC-3, AC-5, AC-6)"
```

---

## Task 6: Holistic pre-commit dispatch pipeline test (AC-3, AC-4, AC-6)

Proves the machinery end-to-end through real units (no mocks for the router/tracker/bus), in the fast lane.

**Files:**
- Create: `Tests/RCommon.Persistence.Tests/PreCommitDispatchPipelineTests.cs`

**Wiring:** build a real `IServiceProvider` with RCommon core event handling + an in-memory bus subscriber, resolve a scoped `InMemoryEntityEventTracker` (real `InMemoryTransactionalEventRouter`), track an aggregate that raises `ISyncEvent`s, and drive `DispatchDomainEventsAsync` directly (the pipeline-order guarantee that this runs pre-commit is already covered by Task 5, so this test may call the tracker's dispatch directly rather than spin a `TransactionScope`). Follow the existing DI-bootstrapping pattern used by other `RCommon.Persistence.Tests` integration-of-units tests (e.g. how `EventSubscriptionIsolationTests` / bootstrapping tests build a provider).

- [ ] **Step 1: Write the failing tests**

```csharp
// (a) Two aggregates each raising a sync event => handlers invoked in raise-order.
// (b) A handler that calls aggregate.AddLocalEvent(next) => 'next' is also handled in the same drain (AC-4).
// (c) MaxDispatchGenerations = 2 + a handler that always re-raises => DispatchGenerationLimitException (AC-4).
// (d) A subscriber that throws => DispatchDomainEventsAsync throws (so CommitAsync would roll back) (AC-6).
```

- [ ] **Step 2: Run to verify they fail** (red for the right reason — assert the specific behavior, not a compile error where avoidable).

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~PreCommitDispatchPipelineTests"`

- [ ] **Step 3: Implement** — no production changes expected (Tasks 1–5 provide the behavior); this task is proof. If a test reveals a gap, fix the relevant production file and note it.

- [ ] **Step 4: Run to verify they pass**

Run: `dotnet test Tests/RCommon.Persistence.Tests/RCommon.Persistence.Tests.csproj --filter "FullyQualifiedName~PreCommitDispatchPipelineTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Tests/RCommon.Persistence.Tests/PreCommitDispatchPipelineTests.cs
git commit -m "test(events): holistic pre-commit dispatch pipeline proof (AC-3, AC-4, AC-6)"
```

---

## Phase-completion checks

After Task 6, before handing back:

- [ ] Full fast-lane suite green: `dotnet test Src/RCommon.sln --filter "Category!=Integration"` (0 failures).
- [ ] Solution builds all TFMs: `dotnet build Src/RCommon.sln -c Release`.
- [ ] Phase-1 cross-datastore integration test still green on Podman (spot-check the reorder didn't disturb the outbox path):
  `$env:DOCKER_HOST = "npipe://./pipe/podman-machine-default"; $env:TESTCONTAINERS_RYUK_DISABLED = "true"; dotnet test Tests/RCommon.IntegrationTests --filter "FullyQualifiedName~CrossDataStoreOutboxTests"`.
- [ ] Final whole-branch code review (superpowers:code-reviewer) covering the Phase-2 diff.
- [ ] Confirm no Phase-2 test carries `[Trait("Category","Integration")]`.

## Migration note (captured here; written up in Phase 6)

**Breaking/semantic change:** in-process domain-event handlers now run **pre-commit, inside the transaction** (was post-commit). A handler that throws now rolls back the whole unit of work (AC-6). Handlers must be in-process and side-effect-light (no external I/O inside the transaction). The single-host domain-vs-durable route split arrives in Phase 3; until then the RCommon outbox path is unchanged from Phase 1.
