using Microsoft.Extensions.Logging;
using RCommon.Models.Events;
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
        private readonly EventSubscriptionManager _subscriptionManager;

        public PublishWithEventBusEventProducer(IEventBus eventBus, ILogger<PublishWithEventBusEventProducer> logger,
            EventSubscriptionManager subscriptionManager)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default)
            where T : ISerializableEvent
        {
            try
            {
                Guard.IsNotNull(@event, nameof(@event));

                if (!_subscriptionManager.ShouldProduceEvent(this.GetType(), typeof(T)))
                {
                    _logger.LogDebug("{0} skipping event {1} - not subscribed to this producer",
                        new object[] { this.GetGenericTypeName(), typeof(T).Name });
                    return;
                }
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
                }
                else
                {
                    _logger.LogDebug("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event });
                }

                // This should already be using a Scoped publish method
                await _eventBus.PublishAsync(@event).ConfigureAwait(false);
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
