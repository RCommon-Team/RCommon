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
using Microsoft.Extensions.DependencyInjection;
using RCommon.Models.Events;

namespace RCommon.MediatR.Producers
{
    /// <summary>
    /// An <see cref="IEventProducer"/> implementation that sends events to a single request handler
    /// using MediatR's send (point-to-point) semantics via <see cref="IMediatorService"/>.
    /// </summary>
    /// <remarks>
    /// Use this producer for command-style messaging where only one handler should process the event.
    /// For fan-out delivery to all handlers, use <see cref="PublishWithMediatREventProducer"/> instead.
    /// </remarks>
    public class SendWithMediatREventProducer : IEventProducer
    {
        private readonly IMediatorService _mediatorService;
        private readonly ILogger<SendWithMediatREventProducer> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventSubscriptionManager _subscriptionManager;

        /// <summary>
        /// Initializes a new instance of <see cref="SendWithMediatREventProducer"/>.
        /// </summary>
        /// <param name="mediatorService">The mediator service used to send events.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="serviceProvider">Service provider for creating scoped services during event production.</param>
        /// <param name="subscriptionManager">Manages event-to-producer subscriptions for routing decisions.</param>
        public SendWithMediatREventProducer(IMediatorService mediatorService, ILogger<SendWithMediatREventProducer> logger,
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

                // Create a scoped service context for the send operation
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("{0} sending event: {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
                    }
                    else
                    {
                        _logger.LogDebug("{0} sending event: {1}", new object[] { this.GetGenericTypeName(), @event });
                    }
                    await _mediatorService.Send(@event, cancellationToken);
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
