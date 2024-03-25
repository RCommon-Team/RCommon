using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    public class PublishWithEventBusEventProducer : IEventProducer
    {
        private readonly IEventBus _eventBus;

        public PublishWithEventBusEventProducer(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }
        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) 
            where T : ISerializableEvent
        {
            await _eventBus.PublishAsync(@event);
        }
    }
}
