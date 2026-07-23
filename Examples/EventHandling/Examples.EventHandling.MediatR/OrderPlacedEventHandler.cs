using RCommon.EventHandling.Subscribers;

namespace Examples.EventHandling.MediatR;

// In-process MediatR subscriber. WithEventHandling<MediatREventHandlingBuilder> self-registers
// MediatR, and AddSubscriber<OrderPlacedEvent, OrderPlacedEventHandler>() bridges this ISubscriber
// to a MediatR INotificationHandler so it is invoked when the event is Publish<T>()'d in-process.
public class OrderPlacedEventHandler : ISubscriber<OrderPlacedEvent>
{
    public static int HandledCount { get; private set; }

    public Task HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        HandledCount++;
        Console.WriteLine($"  [subscriber] Order {@event.OrderId} placed for {@event.Total:C}");
        return Task.CompletedTask;
    }
}
