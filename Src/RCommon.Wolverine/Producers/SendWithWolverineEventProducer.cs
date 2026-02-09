using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// An <see cref="IEventProducer"/> implementation that sends events to a single handler endpoint
    /// using Wolverine's <see cref="IMessageBus.SendAsync{T}(T)"/> method (point-to-point pattern).
    /// </summary>
    /// <remarks>
    /// Use this producer for command-style messaging where only one handler should process the event.
    /// For fan-out delivery to all handlers, use <see cref="PublishWithWolverineEventProducer"/> instead.
    /// </remarks>
    public class SendWithWolverineEventProducer : IEventProducer
    {
        private readonly IMessageBus _messageBus;
        private readonly ILogger<SendWithWolverineEventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Initializes a new instance of <see cref="SendWithWolverineEventProducer"/>.
        /// </summary>
        /// <param name="messageBus">The Wolverine message bus used to send events.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="serviceProvider">Service provider for creating scoped services during event production.</param>
        /// <param name="subscriptionManager">Manages event-to-producer subscriptions for routing decisions.</param>
        public SendWithWolverineEventProducer(IMessageBus messageBus, ILogger<SendWithWolverineEventProducer> logger,
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

                // Create a scoped service context for the send operation
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
                    await _messageBus.SendAsync(@event);
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
