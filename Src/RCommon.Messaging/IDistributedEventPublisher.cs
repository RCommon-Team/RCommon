
namespace RCommon.Messaging
{
    public interface IDistributedEventPublisher
    {
        IReadOnlyCollection<object> DistributedEvents { get; }

        void AddDistributedEvent<T>(T distributedEvent) where T : IDistributedEvent;
        void ClearDistributedEvents();
        Task PublishDistributedEvents(CancellationToken cancellationToken);
        void RemoveDistributedEvent<T>(T distributedEvent) where T : IDistributedEvent;
    }
}
