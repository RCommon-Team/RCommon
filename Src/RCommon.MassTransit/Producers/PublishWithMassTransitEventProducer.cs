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
        private readonly IPublishEndpoint _publishEndpoint;

        public PublishWithMassTransitEventProducer(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
        {
            Console.WriteLine("{0} publishing event {1} to MassTransit", new object[] { this.GetGenericTypeName(), @event });
            await _publishEndpoint.Publish(@event, cancellationToken);
        }
    }
}
