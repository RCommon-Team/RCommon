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
    public class MassTransitEventHandler<TEvent> : IMassTransitEventHandler<TEvent>, IConsumer<TEvent>
        where TEvent : class, ISerializableEvent
    {
        private readonly ILogger<MassTransitEventHandler<TEvent>> _logger;
        private readonly ISubscriber<TEvent> _subscriber;

        public MassTransitEventHandler(ILogger<MassTransitEventHandler<TEvent>> logger, ISubscriber<TEvent> subscriber)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
        }

        public async Task Consume(ConsumeContext<TEvent> context)
        {
            _logger.LogDebug("{0} handling event {1}", new object[] { this.GetGenericTypeName(), context.Message });
            await _subscriber.HandleAsync(context.Message);
        }
    }
}
