using Microsoft.Extensions.Logging;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace RCommon.MassTransit.Subscribers
{
    /// <summary>
    /// Wolverine handler that bridges Wolverine message handling to the RCommon <see cref="ISubscriber{TEvent}"/> abstraction.
    /// Implements both <see cref="IWolverineEventHandler{TEvent}"/> and Wolverine's <see cref="IWolverineHandler"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type to handle. Must implement <see cref="ISerializableEvent"/>.</typeparam>
    public class WolverineEventHandler<TEvent> : IWolverineEventHandler<TEvent>, IWolverineHandler
        where TEvent : class, ISerializableEvent
    {
        private readonly ISubscriber<TEvent> _subscriber;
        private readonly ILogger<WolverineEventHandler<TEvent>> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="WolverineEventHandler{TEvent}"/>.
        /// </summary>
        /// <param name="subscriber">The RCommon subscriber that handles the event.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public WolverineEventHandler(ISubscriber<TEvent> subscriber, ILogger<WolverineEventHandler<TEvent>> logger)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{0} handling event {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
            await _subscriber.HandleAsync(@event);
        }
    }
}
