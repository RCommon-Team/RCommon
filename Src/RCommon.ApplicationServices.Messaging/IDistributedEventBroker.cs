
namespace RCommon.ApplicationServices.Messaging
{
    public interface IDistributedEventBroker
    {
        IReadOnlyCollection<IDistributedEvent> DistributedEvents { get; }

        void AddDistributedEvent(IDistributedEvent distributedEvent);
        void ClearDistributedEvents();
        Task PublishDistributedEvents(CancellationToken cancellationToken);
        void RemoveDistributedEvent(IDistributedEvent distributedEvent);
    }
}