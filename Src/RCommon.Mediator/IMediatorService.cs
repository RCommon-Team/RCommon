using RCommon.EventHandling;

namespace RCommon.Mediator
{
    public interface IMediatorService
    {
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : ISerializableEvent;
        Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : ISerializableEvent;
    }
}