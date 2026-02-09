using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling.Producers;
using RCommon.Mediator;
using RCommon.MediatR.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Producers
{
    /// <summary>
    /// An <see cref="IEventProducer"/> implementation that publishes events to all notification handlers
    /// using MediatR's publish (fan-out) semantics via <see cref="IMediatorService"/>.
    /// </summary>
    /// <remarks>
    /// Use this producer when you want an event to be delivered to all registered notification handlers.
    /// For point-to-point delivery, use <see cref="SendWithMediatREventProducer"/> instead.
    /// </remarks>
    public class PublishWithMediatREventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;
        private readonly ILogger<PublishWithMediatREventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Initializes a new instance of <see cref="PublishWithMediatREventProducer"/>.
        /// </summary>
        /// <param name="mediatorService">The mediator service used to publish events.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="serviceProvider">Service provider for creating scoped services during event production.</param>
        /// <param name="subscriptionManager">Manages event-to-producer subscriptions for routing decisions.</param>
        public PublishWithMediatREventProducer(IMediatorService mediatorService, ILogger<PublishWithMediatREventProducer> logger,
            IServiceProvider serviceProvider, EventSubscriptionManager subscriptionManager)
        {
            _mediatorService = mediatorService ?? throw new ArgumentNullException(nameof(mediatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        }

        /// <inheritdoc />
        public async Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            try
            {
                Guard.IsNotNull(@event, nameof(@event));

                // Check if this event type is subscribed to this producer; skip if not routed here
                if (!_subscriptionManager.ShouldProduceEvent(this.GetType(), typeof(TEvent)))
                {
                    _logger.LogDebug("{0} skipping event {1} - not subscribed to this producer",
                        new object[] { this.GetGenericTypeName(), typeof(TEvent).Name });
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
                    
                    
                    await _mediatorService.Publish(@event, cancellationToken);
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
