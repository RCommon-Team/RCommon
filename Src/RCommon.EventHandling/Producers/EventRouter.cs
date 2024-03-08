using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RCommon;

namespace RCommon.EventHandling.Producers
{
    /// <summary>
    /// Responsible for wiring up all <see cref="ISerializableEvent">local events</see> to their appropriate <see cref="IEventProducer"/>
    /// </summary>
    public class EventRouter : IEventRouter
    {
        private readonly IServiceProvider _serviceProvider;

        //private readonly IEventProducerRegistry _eventProducerRegistry;

        public EventRouter(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            //_eventProducerRegistry = eventProducerRegistry ?? throw new ArgumentNullException(nameof(eventProducerRegistry));
        }

        public async Task RouteEvents(IEnumerable<ISerializableEvent> localEvents)
        {
            try
            {
                // Seperate Async events from Transactional Events
                var syncEvents = localEvents.Where(x => x is ISyncEvent);
                var asyncEvents = localEvents.Where(x => x is IAsyncEvent);
                var eventProducers = _serviceProvider.GetServices<IEventProducer>();

                // Produce the Transactional Events first
                foreach (var localEvent in syncEvents)
                {
                    foreach (var producer in eventProducers)
                    {
                        await producer.ProduceEventAsync(localEvent);
                    }
                }

                foreach (var localEvent in asyncEvents)
                {
                    foreach (var producer in eventProducers)
                    {
                        await producer.ProduceEventAsync(localEvent).ConfigureAwait(false);
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
