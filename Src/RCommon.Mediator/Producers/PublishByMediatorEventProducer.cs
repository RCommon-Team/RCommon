using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Producers
{
    public class PublishByMediatorEventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;

        public PublishByMediatorEventProducer(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public async Task ProduceEventAsync(ISerializableEvent @event)
        {
            await _mediatorService.Publish<ISerializableEvent>(@event);
        }
    }
}
