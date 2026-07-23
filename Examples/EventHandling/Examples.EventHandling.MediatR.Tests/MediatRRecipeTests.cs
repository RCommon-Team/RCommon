using Examples.EventHandling.MediatR;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon;
using RCommon.EventHandling.Producers;
using RCommon.MediatR;
using Xunit;

namespace Examples.EventHandling.MediatR.Tests;

/// <summary>
/// Recipe 5: DDD + in-process mediator (MediatR).
///
/// An aggregate raises a domain event which is dispatched IN-PROCESS through MediatR to an
/// <see cref="RCommon.EventHandling.Subscribers.ISubscriber{T}"/> handler. There is no broker and no
/// transport -- MediatR is in-process only. MediatR itself is self-registered by
/// <c>WithEventHandling&lt;MediatREventHandlingBuilder&gt;</c> (it calls <c>services.AddMediatR(...)</c>
/// internally), so the test wires no mediator by hand.
///
/// Wiring requires BOTH verbs: <c>Publish&lt;OrderPlacedEvent&gt;()</c> registers
/// <see cref="RCommon.MediatR.Producers.PublishWithMediatREventProducer"/> (the in-process producer),
/// and <c>AddSubscriber&lt;OrderPlacedEvent, OrderPlacedEventHandler&gt;()</c> bridges the RCommon
/// subscriber to a MediatR notification handler. AddSubscriber alone does not register the producer.
///
/// Dispatch shape: the event is produced by resolving the registered <see cref="IEventProducer"/>
/// (the MediatR publish producer) and calling <c>ProduceEventAsync</c> with the CONCRETE event type.
/// This is the shape the runnable example uses. The MediatR publish producer wraps the event in
/// <c>MediatRNotification&lt;TEvent&gt;</c> using the compile-time generic argument, so the event must
/// reach the producer as its concrete type (<c>OrderPlacedEvent</c>) for MediatR to match the
/// registered <c>INotificationHandler&lt;MediatRNotification&lt;OrderPlacedEvent&gt;&gt;</c>.
/// </summary>
public class MediatRRecipeTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRCommon()
            .WithSimpleGuidGenerator()
            .WithEventHandling<MediatREventHandlingBuilder>(events =>
            {
                // Publish<T>() registers PublishWithMediatREventProducer (the in-process producer);
                // AddSubscriber<T,H>() bridges the RCommon ISubscriber to a MediatR notification
                // handler. Both are required to produce AND handle the event in-process.
                events.Publish<OrderPlacedEvent>();
                events.AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>();
            });
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProduceEvent_DispatchesDomainEvent_ToInProcessMediatRSubscriber()
    {
        using var provider = BuildProvider();

        var handledCountBefore = OrderPlacedEventHandler.HandledCount;

        // Aggregate raises the domain event via the DDD API.
        var order = new Order { CustomerName = "Ada Lovelace", Total = 249.99m };
        order.Place();
        var domainEvent = (OrderPlacedEvent)order.LocalEvents.Single();

        using (var scope = provider.CreateScope())
        {
            // The MediatR builder registers exactly one IEventProducer: the publish producer.
            var producer = scope.ServiceProvider.GetServices<IEventProducer>().Single();

            // Produce with the concrete event type so the producer wraps it as
            // MediatRNotification<OrderPlacedEvent>, which MediatR routes in-process to the
            // registered handler (which invokes OrderPlacedEventHandler).
            await producer.ProduceEventAsync(domainEvent);
        }

        OrderPlacedEventHandler.HandledCount.Should().Be(handledCountBefore + 1,
            "producing the domain event must dispatch it in-process via MediatR to the ISubscriber");
    }
}
