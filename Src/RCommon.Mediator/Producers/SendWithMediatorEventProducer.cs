using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.Producers
{
    public class SendWithMediatorEventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;

        public SendWithMediatorEventProducer(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) 
            where T : ISerializableEvent
        {
            throw new NotImplementedException();
        }
    }
}
