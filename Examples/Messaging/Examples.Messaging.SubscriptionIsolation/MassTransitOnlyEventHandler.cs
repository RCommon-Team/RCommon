using RCommon.EventHandling.Subscribers;

namespace Examples.Messaging.SubscriptionIsolation
{
    public class MassTransitOnlyEventHandler : ISubscriber<MassTransitOnlyEvent>
    {
        public MassTransitOnlyEventHandler()
        {
        }

        public async Task HandleAsync(MassTransitOnlyEvent notification, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[MassTransit] Handled MassTransitOnlyEvent: {0} | {1}",
                notification.DateTime, notification.Guid);
            await Task.CompletedTask;
        }
    }
}
