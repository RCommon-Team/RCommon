using MassTransit;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
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
        private readonly ISubscriber<TEvent> _subscriber;

        public MassTransitEventHandler(ISubscriber<TEvent> subscriber)
        {
            _subscriber = subscriber;
        }

        public async Task Consume(ConsumeContext<TEvent> context)
        {
            Console.WriteLine("{0} handling event {1} from MassTransit", new object[] { this.GetGenericTypeName(), context.Message });
            await _subscriber.HandleAsync(context.Message);
        }
    }
}
