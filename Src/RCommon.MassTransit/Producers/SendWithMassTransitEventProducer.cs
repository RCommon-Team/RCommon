using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit.Producers
{
    public class SendWithMassTransitEventProducer : IEventProducer
    {
        public SendWithMassTransitEventProducer()
        {
                
        }

        public Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) 
            where T : ISerializableEvent
        {
            throw new NotImplementedException();
        }
    }
}
