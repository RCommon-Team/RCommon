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
            try
            {
                Guard.IsNotNull(@event, nameof(@event));
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
                }
                else
                {
                    _logger.LogDebug("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event });
                }

                // This should already be using a Scoped publish method
                await _eventBus.PublishAsync(@event);
            }
            catch (Exception ex)
            {
                throw new EventProductionException("An error occured in {0} while producing event {1}",
                    ex,
                    new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
            }

        }
    }
}
