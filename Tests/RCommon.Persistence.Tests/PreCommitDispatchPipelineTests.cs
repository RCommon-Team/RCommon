using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Holistic proof that the Phase-2 pre-commit domain-event dispatch pipeline works end-to-end
/// through REAL units (real <see cref="InMemoryTransactionalEventRouter"/>, real
/// <see cref="InMemoryEntityEventTracker"/>, real <see cref="InMemoryEventBus"/> + subscribers) — no
/// mocks for the router/tracker/bus. Drives <see cref="IEntityEventTracker.DispatchDomainEventsAsync"/>
/// directly (the pipeline-ORDER guarantee is covered by the UnitOfWork ordering test).
/// Covers AC-3 (raise-order), AC-4 (single-pass cascade + cycle-breaker), AC-6 (handler throw surfaces
/// so CommitAsync would roll back).
/// </summary>
public class PreCommitDispatchPipelineTests
{
    // ---- Shared, thread-safe recorder (async handlers may run concurrently across tests) --------
    private static readonly ConcurrentQueue<string> Handled = new();

    // Optional cascade hook: a handler raises `Next` on `On` when set on the event it handles.
    // Carried on the event so the DI-resolved handler doesn't need to close over test state.

    private static ServiceProvider BuildProvider(Action<EventHandlingOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var builder = new RCommonBuilder(services);
        builder.WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
        {
            eventHandling.AddSubscriber<PipelineSyncEvent, PipelineSyncEventHandler>();
            eventHandling.AddSubscriber<ThrowingSyncEvent, ThrowingSyncEventHandler>();
        });

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        return services.BuildServiceProvider();
    }

    private static (IEntityEventTracker tracker, IServiceScope scope) NewScopedTracker(ServiceProvider provider)
    {
        var scope = provider.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<IEventRouter>();
        var tracker = new InMemoryEntityEventTracker(router);
        return (tracker, scope);
    }

    // ---- (a) Two aggregates each raising a sync event => handlers invoked in raise-order ---------
    [Fact]
    public async Task DispatchDomainEventsAsync_TwoAggregates_HandlersInvokedInRaiseOrder()
    {
        Handled.Clear();
        using var provider = BuildProvider();
        var (tracker, scope) = NewScopedTracker(provider);
        using (scope)
        {
            var first = new TestAggregate();
            var second = new TestAggregate();
            first.RaiseSync("first");   // raised before...
            second.RaiseSync("second"); // ...second

            tracker.AddEntity(first);
            tracker.AddEntity(second);

            await tracker.DispatchDomainEventsAsync();

            Handled.Should().Equal("first", "second");
        }
    }

    // ---- (b) Handler raises another event during dispatch => same-drain single-pass cascade ------
    [Fact]
    public async Task DispatchDomainEventsAsync_HandlerRaisesAnotherEvent_BothHandledInSameDrain()
    {
        Handled.Clear();
        using var provider = BuildProvider();
        var (tracker, scope) = NewScopedTracker(provider);
        using (scope)
        {
            var aggregate = new TestAggregate();
            // When "original" is handled, the handler raises "cascaded" on this same aggregate.
            aggregate.RaiseSync("original", cascadeTo: aggregate, cascadeMessage: "cascaded");

            tracker.AddEntity(aggregate);

            await tracker.DispatchDomainEventsAsync();

            Handled.Should().Contain("original");
            Handled.Should().Contain("cascaded");
        }
    }

    // ---- (c) MaxDispatchGenerations = 2 + always-re-raising handler => cycle-breaker throws -------
    [Fact]
    public async Task DispatchDomainEventsAsync_RunawayCascade_ThrowsDispatchGenerationLimitException()
    {
        Handled.Clear();
        using var provider = BuildProvider(o => o.MaxDispatchGenerations = 2);
        var (tracker, scope) = NewScopedTracker(provider);
        using (scope)
        {
            var aggregate = new TestAggregate();
            // reRaiseForever => every handling raises a fresh event on the same aggregate.
            aggregate.RaiseSync("boom", cascadeTo: aggregate, reRaiseForever: true);

            tracker.AddEntity(aggregate);

            var act = () => tracker.DispatchDomainEventsAsync();

            // OBSERVED REAL BEHAVIOR: the cycle-breaker fires when a *handler* re-raises during the
            // drain. Because handlers are dispatched through InMemoryEventBus via reflection, the
            // handler-thrown DispatchGenerationLimitException is wrapped by reflection
            // (TargetInvocationException) and then by the producer (EventProductionException). The
            // router's "when (ex is not DispatchGenerationLimitException)" guard only sees the bare
            // type when the producer raises directly (see the router's own unit tests) -- through the
            // full bus+subscriber pipeline it arrives already wrapped. What matters for AC-4 is that
            // dispatch THROWS (so CommitAsync rolls back) and the cycle-breaker is the root cause.
            var thrown = await act.Should().ThrowAsync<EventProductionException>();
            var rootCause = UnwrapToRoot(thrown.Which);
            rootCause.Should().BeOfType<DispatchGenerationLimitException>()
                     .Which.MaxDispatchGenerations.Should().Be(2);
        }
    }

    private static Exception UnwrapToRoot(Exception ex)
    {
        var current = ex;
        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }
        return current;
    }

    // ---- (d) A subscriber that throws => DispatchDomainEventsAsync throws (CommitAsync rolls back) -
    [Fact]
    public async Task DispatchDomainEventsAsync_SubscriberThrows_SurfacesEventProductionException()
    {
        Handled.Clear();
        using var provider = BuildProvider();
        var (tracker, scope) = NewScopedTracker(provider);
        using (scope)
        {
            var aggregate = new TestAggregate();
            aggregate.RaiseThrowing("kaboom");

            tracker.AddEntity(aggregate);

            var act = () => tracker.DispatchDomainEventsAsync();

            // OBSERVED REAL BEHAVIOR: the router wraps a generic handler exception in
            // EventProductionException (see InMemoryTransactionalEventRouter.RouteEventsAsync catch).
            // Because the in-memory bus invokes the subscriber via reflection, the handler's
            // InvalidOperationException is first wrapped in a TargetInvocationException, then in the
            // EventProductionException. What matters for AC-6 is that dispatch THROWS (so CommitAsync
            // would roll back) and the original handler exception is preserved at the root.
            var thrown = await act.Should().ThrowAsync<EventProductionException>();
            var rootCause = UnwrapToRoot(thrown.Which);
            rootCause.Should().BeOfType<InvalidOperationException>()
                     .Which.Message.Should().Be("kaboom");
        }
    }

    // ---------------------------------- Test doubles ----------------------------------------------

    /// <summary>A minimal aggregate that raises <see cref="ISyncEvent"/>s via <c>AddLocalEvent</c>.</summary>
    public sealed class TestAggregate : BusinessEntity<Guid>
    {
        public TestAggregate() : base(Guid.NewGuid()) { }

        public void RaiseSync(string message, TestAggregate? cascadeTo = null,
            string? cascadeMessage = null, bool reRaiseForever = false)
        {
            AddLocalEvent(new PipelineSyncEvent
            {
                Message = message,
                CascadeTo = cascadeTo,
                CascadeMessage = cascadeMessage,
                ReRaiseForever = reRaiseForever,
            });
        }

        public void RaiseThrowing(string message)
            => AddLocalEvent(new ThrowingSyncEvent { Message = message });
    }

    public sealed class PipelineSyncEvent : ISyncEvent
    {
        public string Message { get; set; } = string.Empty;

        // Cascade instructions (used by tests (b) and (c)); ignored when null/false.
        internal TestAggregate? CascadeTo { get; set; }
        internal string? CascadeMessage { get; set; }
        internal bool ReRaiseForever { get; set; }
    }

    public sealed class ThrowingSyncEvent : ISyncEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public sealed class PipelineSyncEventHandler : ISubscriber<PipelineSyncEvent>
    {
        public Task HandleAsync(PipelineSyncEvent @event, CancellationToken cancellationToken = default)
        {
            Handled.Enqueue(@event.Message);

            if (@event.CascadeTo is not null)
            {
                if (@event.ReRaiseForever)
                {
                    // Always raise a fresh cascading event => unbounded cascade (trips the limit).
                    @event.CascadeTo.RaiseSync(
                        @event.Message + "*",
                        cascadeTo: @event.CascadeTo,
                        reRaiseForever: true);
                }
                else if (@event.CascadeMessage is not null)
                {
                    // One-shot cascade: raise exactly one follow-up event, which itself does not cascade.
                    @event.CascadeTo.RaiseSync(@event.CascadeMessage);
                }
            }

            return Task.CompletedTask;
        }
    }

    public sealed class ThrowingSyncEventHandler : ISubscriber<ThrowingSyncEvent>
    {
        public Task HandleAsync(ThrowingSyncEvent @event, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(@event.Message);
    }
}
