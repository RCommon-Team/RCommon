using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
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

        public WolverineEventHandler(ISubscriber<TEvent> subscriber)
        {
            _subscriber = subscriber;
        }

        public async Task HandleAsync(TEvent distributedEvent, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("{0} handling event {1} from MassTransit", new object[] { this.GetGenericTypeName(), distributedEvent });
            await _subscriber.HandleAsync(distributedEvent);
        }
    }
}
