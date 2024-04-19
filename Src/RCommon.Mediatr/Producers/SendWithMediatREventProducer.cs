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
using Microsoft.Extensions.Logging;

namespace RCommon.MediatR.Producers
{
    public class SendWithMediatREventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;
        private readonly ILogger<SendWithMediatREventProducer> _logger;

        public SendWithMediatREventProducer(IMediatorService mediatorService, ILogger<SendWithMediatREventProducer> logger)
        {
            _mediatorService = mediatorService ?? throw new ArgumentNullException(nameof(mediatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
            where TEvent : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            _logger.LogInformation("{0} sending event: {1}", new object[] { this.GetGenericTypeName(), @event });
            await _mediatorService.Send(@event, cancellationToken);
        }
    }
}
