using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Producers
{
    public class SendByMediatorEventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;

        public SendByMediatorEventProducer(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public async Task ProduceEventAsync(ISerializableEvent @event)
        {
            await _mediatorService.Send(@event);
        }
    }
}
