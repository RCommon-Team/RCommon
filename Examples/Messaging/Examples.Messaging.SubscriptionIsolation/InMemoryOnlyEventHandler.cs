using RCommon.EventHandling.Subscribers;

namespace Examples.Messaging.SubscriptionIsolation
{
    public class InMemoryOnlyEventHandler : ISubscriber<InMemoryOnlyEvent>
    {
        public InMemoryOnlyEventHandler()
        {
        }

        public async Task HandleAsync(InMemoryOnlyEvent notification, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[InMemoryEventBus] Handled InMemoryOnlyEvent: {0} | {1}",
                notification.DateTime, notification.Guid);
            await Task.CompletedTask;
        }
    }
}
