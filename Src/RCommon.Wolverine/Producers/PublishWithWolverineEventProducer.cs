using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace RCommon.Wolverine.Producers
{
    /// <summary>
    /// An <see cref="IEventProducer"/> implementation that publishes events to all subscribed handlers
    /// using Wolverine's <see cref="IMessageBus.PublishAsync{T}(T)"/> method (fan-out pattern).
    /// </summary>
    /// <remarks>
    /// Use this producer when you want an event to be delivered to all Wolverine handlers subscribed
    /// to the event type. For point-to-point delivery, use <see cref="SendWithWolverineEventProducer"/> instead.
    /// </remarks>
    public class PublishWithWolverineEventProducer : IEventProducer
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger<PublishWithWolverineEventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Initializes a new instance of <see cref="PublishWithWolverineEventProducer"/>.
        /// </summary>
        /// <param name="messageBus">The Wolverine message bus used to publish events.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="serviceProvider">Service provider for creating scoped services during event production.</param>
        /// <param name="subscriptionManager">Manages event-to-producer subscriptions for routing decisions.</param>
        public PublishWithWolverineEventProducer(IMessageBus messageBus, ILogger<PublishWithWolverineEventProducer> logger,
            IServiceProvider serviceProvider, EventSubscriptionManager subscriptionManager)
        {
            _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        /// <inheritdoc />
        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
        {
            try
            {
                Guard.IsNotNull(@event, nameof(@event));

                // Check if this event type is subscribed to this producer; skip if not routed here
                if (!_subscriptionManager.ShouldProduceEvent(this.GetType(), typeof(T)))
                {
                    _logger.LogDebug("{0} skipping event {1} - not subscribed to this producer",
                        new object[] { this.GetGenericTypeName(), typeof(T).Name });
                    return;
                }

                // Create a scoped service context for the publish operation
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
                    }
                    else
                    {
                        _logger.LogDebug("{0} publishing event: {1}", new object[] { this.GetGenericTypeName(), @event });
                    }
                    await _messageBus.PublishAsync<T>(@event);
                }
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
