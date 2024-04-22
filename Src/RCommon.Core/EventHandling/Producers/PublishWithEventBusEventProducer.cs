using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    public class PublishWithEventBusEventProducer : IEventProducer
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<PublishWithEventBusEventProducer> _logger;

        public PublishWithEventBusEventProducer(IEventBus eventBus, ILogger<PublishWithEventBusEventProducer> logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) 
            where T : ISerializableEvent
        {
            Guard.IsNotNull(@event, nameof(@event));
            _logger.LogInformation("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });

            // This should already be using a Scoped publish method
            await _eventBus.PublishAsync(@event);
        }
    }
}
