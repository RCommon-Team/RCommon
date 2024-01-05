
namespace RCommon.EventHandling.Producers
{
    public interface IEventProducerRegistry
    {
        ICollection<IEventProducer> GetEventProducersForEvent(Type @event);
        void RegisterEventProducer<TEventProducer>(Type @event, TEventProducer eventProducer) where TEventProducer : IEventProducer;
    }
}