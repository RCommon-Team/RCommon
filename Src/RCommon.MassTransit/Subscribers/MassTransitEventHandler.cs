using MassTransit;
using Microsoft.Extensions.Logging;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit.Subscribers
{
    /// <summary>
    /// MassTransit consumer that bridges MassTransit message consumption to the RCommon <see cref="ISubscriber{TEvent}"/> abstraction.
    /// Implements both <see cref="IMassTransitEventHandler{TEvent}"/> and MassTransit's <see cref="IConsumer{TEvent}"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type to consume. Must implement <see cref="ISerializableEvent"/>.</typeparam>
    public class MassTransitEventHandler<TEvent> : IMassTransitEventHandler<TEvent>, IConsumer<TEvent>
        where TEvent : class, ISerializableEvent
    {
        private readonly ILogger<MassTransitEventHandler<TEvent>> _logger;
        private readonly ISubscriber<TEvent> _subscriber;

        /// <summary>
        /// Initializes a new instance of <see cref="MassTransitEventHandler{TEvent}"/>.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="subscriber">The RCommon subscriber that handles the event.</param>
        public MassTransitEventHandler(ILogger<MassTransitEventHandler<TEvent>> logger, ISubscriber<TEvent> subscriber)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        /// <summary>
        /// Consumes a MassTransit message and delegates to the registered <see cref="ISubscriber{TEvent}"/>.
        /// </summary>
        /// <param name="context">The MassTransit consume context containing the event message.</param>
        public async Task Consume(ConsumeContext<TEvent> context)
        {
            _logger.LogDebug("{0} handling event {1}", new object[] { this.GetGenericTypeName(), context.Message });
            await _subscriber.HandleAsync(context.Message);
        }
    }
}
