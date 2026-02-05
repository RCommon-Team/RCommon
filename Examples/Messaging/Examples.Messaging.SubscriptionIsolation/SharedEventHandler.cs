using RCommon.EventHandling.Subscribers;

namespace Examples.Messaging.SubscriptionIsolation
{
    public class SharedEventHandler : ISubscriber<SharedEvent>
    {
        public SharedEventHandler()
        {
        }

        public async Task HandleAsync(SharedEvent notification, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("[SharedHandler] Handled SharedEvent: {0} | {1}",
                notification.DateTime, notification.Guid);
            await Task.CompletedTask;
        }
    }
}
