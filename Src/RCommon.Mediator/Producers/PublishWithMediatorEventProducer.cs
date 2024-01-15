using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Producers
{
    public class PublishWithMediatorEventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;

        public PublishWithMediatorEventProducer(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public async Task ProduceEventAsync<T>(T @event)
            where T : ISerializableEvent
        {
            await _mediatorService.Publish<T>(@event);
        }
    }
}
