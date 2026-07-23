using RCommon.EventHandling.Subscribers;

namespace Examples.EventHandling.TransactionScript;

public class StockAdjustedHandler : ISubscriber<StockAdjustedEvent>
{
    public static int HandledCount { get; private set; }

    public Task HandleAsync(StockAdjustedEvent @event, CancellationToken cancellationToken = default)
    {
        HandledCount++;
        Console.WriteLine($"  [subscriber] Stock {@event.Sku} adjusted to {@event.Quantity}");
        return Task.CompletedTask;
    }
}
