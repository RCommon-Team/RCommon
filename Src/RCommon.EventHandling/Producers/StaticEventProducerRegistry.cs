using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{

    public class StaticEventProducerRegistry : IEventProducerRegistry
    {
        private readonly IServiceProvider _serviceProvider;

        public StaticEventProducerRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public ICollection<IEventProducer> GetEventProducersForEvent(Type @event)
        {
            var eventProducerTypes = StaticEventProducerStore.EventProducers
                .Where(x => x.Key == @event).Select(x => x.Value);
            var eventProducers = new List<IEventProducer>();

            foreach (var type in eventProducerTypes)
            {
                IEventProducer eventProducer = (IEventProducer)this._serviceProvider.GetService(type);
                eventProducers.Add(eventProducer);
            }
            return eventProducers;
        }



        public void RegisterEventProducer<TEventProducer>(Type @event, TEventProducer eventProducer)
            where TEventProducer : IEventProducer
        {
            if (!StaticEventProducerStore.EventProducers.TryAdd(@event, typeof(TEventProducer)))
            {
                throw new UnsupportedEventProducerException($"The StaticEventProducerStore refused to add the new EventProducer of type: {eventProducer.GetType().AssemblyQualifiedName} for event of type: {@event.GetType().AssemblyQualifiedName}");
            }
        }
    }
}
