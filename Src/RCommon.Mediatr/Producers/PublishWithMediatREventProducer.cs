using MediatR;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Mediator;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Producers
{
    public class PublishWithMediatREventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;
        private readonly ILogger<PublishWithMediatREventProducer> _logger;

        public PublishWithMediatREventProducer(IMediatorService mediatorService, ILogger<PublishWithMediatREventProducer> logger)
        {
            _mediatorService = mediatorService ?? throw new ArgumentNullException(nameof(mediatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            _logger.LogInformation("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event });
            await _mediatorService.Publish(@event, cancellationToken);
        }
    }
}
