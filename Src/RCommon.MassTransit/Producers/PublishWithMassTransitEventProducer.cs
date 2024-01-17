using MassTransit;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit.Producers
{
    public class PublishWithMassTransitEventProducer : IEventProducer
    {
        //private readonly IPublishEndpoint _publishEndpoint;
        private readonly IBus _bus;

        public PublishWithMassTransitEventProducer(IBus bus)
        {
           // _publishEndpoint = publishEndpoint;
            _bus = bus;
        }

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
        {
            Console.WriteLine("{0} publishing event {1} to MassTransit", new object[] { this.GetGenericTypeName(), @event });
            await _bus.Publish(@event, cancellationToken);
        }
    }
}
