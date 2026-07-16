using RCommon.EventHandling.Subscribers;

namespace Examples.EventHandling.Outbox;

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
