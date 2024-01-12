using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RCommon;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Responsible for wiring up all <see cref="ISerializableEvent">local events</see> to their appropriate <see cref="IEventProducer"/>
    /// </summary>
    public class EventRouter
    {
        private readonly IEventProducerRegistry _eventProducerRegistry;

        public EventRouter(IEventProducerRegistry eventProducerRegistry)
        {
            _eventProducerRegistry = eventProducerRegistry ?? throw new ArgumentNullException(nameof(eventProducerRegistry));
        }

        public async Task RouteEvents(ICollection<ISerializableEvent> localEvents)
        {
            try
            {
                // Seperate Async events from Transactional Events
                var syncEvents = localEvents.Where(x => x is ISyncEvent);
                var asyncEvents = localEvents.Where(x => x is IAsyncEvent);

                // Produce the Transactional Events first
                foreach (var localEvent in localEvents)
                {
                    var eventProducers = _eventProducerRegistry.GetEventProducersForEvent(localEvent.GetType());
                    
                    foreach (var eventProducer in eventProducers)
                    {
                        await eventProducer.ProduceEventAsync(localEvent);
                    }
                }
                
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
