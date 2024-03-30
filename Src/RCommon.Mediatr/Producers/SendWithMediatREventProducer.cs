using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using RCommon.MediatR.Subscribers;

namespace RCommon.MediatR.Producers
{
    public class SendWithMediatREventProducer : IEventProducer
    {
        private readonly IMediator _mediatorService;

        public SendWithMediatREventProducer(IMediator mediatorService)
        {
            _mediatorService = mediatorService;
        }

        public async Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            await _mediatorService.Send(new MediatRRequest<TEvent>(@event), cancellationToken);
        }
    }
}
