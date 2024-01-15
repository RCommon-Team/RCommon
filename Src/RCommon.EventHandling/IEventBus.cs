using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    public interface IEventBus
    {
        Task PublishAsync<TEvent>(TEvent @event);
        IEventBus Subscribe<TEvent, TEventHandler>()
            where TEvent : class
            where TEventHandler : class, Subscribers.ISubscriber<TEvent>;
        IEventBus SubscribeAllHandledEvents<TEventHandler>() where TEventHandler : class;
    }
}