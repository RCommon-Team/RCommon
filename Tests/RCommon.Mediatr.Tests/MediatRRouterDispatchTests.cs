using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.MediatR;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Mediatr.Tests;

/// <summary>
/// Regression guard for the MediatR event-integration type-erasure bug (Phase 5 / Recipe 5).
///
/// The transactional event router (<c>InMemoryTransactionalEventRouter.ProduceSyncEvents/ProduceAsyncEvents</c>)
/// iterates events as <see cref="ISerializableEvent"/> and invokes each producer's
/// <c>ProduceEventAsync(@event)</c> with that STATIC type — so <c>TEvent</c> is <see cref="ISerializableEvent"/>,
/// not the concrete event type. Before the fix, <see cref="MediatRAdapter"/> wrapped the payload as
/// <c>MediatRNotification&lt;ISerializableEvent&gt;</c> (baked from the compile-time generic), which never
/// matched the <c>MediatRNotification&lt;TConcrete&gt;</c> notification handler registered by
/// <c>AddSubscriber</c>, so the event was silently dropped — breaking the canonical DDD + UnitOfWork +
/// in-process-MediatR flow. The in-memory bus never had this bug because it keys on <c>@event.GetType()</c>.
///
/// This test drives the exact erasure point: it invokes the registered MediatR producer with the event
/// statically typed as <see cref="ISerializableEvent"/> and asserts the concrete <see cref="ISubscriber{T}"/>
/// still runs.
/// </summary>
public class MediatRRouterDispatchTests
{
    public sealed class WidgetCreated : ISyncEvent
    {
    }

    public sealed class WidgetCreatedSubscriber : ISubscriber<WidgetCreated>
    {
        public static int HandledCount { get; private set; }

        public Task HandleAsync(WidgetCreated @event, CancellationToken cancellationToken = default)
        {
            HandledCount++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Producer_invoked_with_base_typed_event_dispatches_to_concrete_subscriber()
    {
        // Arrange — a full RCommon MediatR event-handling pipeline with a published + subscribed event.
        // Uses the AddRCommon() chain so the MediatR WithEventHandling overload (which registers
        // IMediatorService + AddMediatR) is selected rather than the core builder overload.
        var services = new ServiceCollection();
        services.AddLogging();
        // Explicitly-typed lambda parameter forces the MediatR WithEventHandling overload (which registers
        // IMediatorService + AddMediatR) rather than the core builder overload — the two are otherwise
        // ambiguous when both RCommon and RCommon.MediatR namespaces are in scope.
        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithEventHandling<MediatREventHandlingBuilder>((IMediatREventHandlingBuilder events) =>
            {
                events.Publish<WidgetCreated>();                               // registers the MediatR producer
                events.AddSubscriber<WidgetCreated, WidgetCreatedSubscriber>(); // registers ISubscriber + bridge handler
            });
        using var provider = services.BuildServiceProvider();

        var handledBefore = WidgetCreatedSubscriber.HandledCount;

        using var scope = provider.CreateScope();
        var producer = scope.ServiceProvider.GetServices<IEventProducer>().Single();

        // Act — mimic the router: hand the producer the event STATICALLY typed as ISerializableEvent.
        ISerializableEvent baseTyped = new WidgetCreated();
        await producer.ProduceEventAsync(baseTyped);

        // Assert — the concrete subscriber must still be invoked exactly once.
        WidgetCreatedSubscriber.HandledCount.Should().Be(handledBefore + 1,
            "an event routed through the pipeline as ISerializableEvent must still reach ISubscriber<WidgetCreated> " +
            "(the MediatR adapter must wrap by the event's runtime type, not the compile-time generic)");
    }
}
