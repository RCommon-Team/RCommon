using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using RCommon.MediatR.Subscribers;
using RCommon.Mediator;

namespace RCommon.MediatR.Producers
{
    public class SendWithMediatREventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;

        public SendWithMediatREventProducer(IMediatorService mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public async Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            await _mediatorService.Send(@event, cancellationToken);
        }
    }
}
