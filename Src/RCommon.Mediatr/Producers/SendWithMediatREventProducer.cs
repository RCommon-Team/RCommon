using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace RCommon.MediatR.Producers
{
    public class SendWithMediatREventProducer : IEventProducer
    {
        private readonly IMediator _mediatorService;

        public SendWithMediatREventProducer(IMediator mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) 
            where T : ISerializableEvent
        {
            await _mediatorService.Send(@event, cancellationToken);
        }
    }
}
