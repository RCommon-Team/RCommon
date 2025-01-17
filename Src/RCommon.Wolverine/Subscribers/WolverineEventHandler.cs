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
    public class WolverineEventHandler<TEvent> : IWolverineEventHandler<TEvent>, IWolverineHandler
        where TEvent : class, ISerializableEvent
    {
        private readonly ISubscriber<TEvent> _subscriber;
        private readonly ILogger<WolverineEventHandler<TEvent>> _logger;

        public WolverineEventHandler(ISubscriber<TEvent> subscriber, ILogger<WolverineEventHandler<TEvent>> logger)
        {
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("{0} handling event {1}", new object[] { this.GetGenericTypeName(), @event.GetGenericTypeName() });
            await _subscriber.HandleAsync(@event);
        }
    }
}
