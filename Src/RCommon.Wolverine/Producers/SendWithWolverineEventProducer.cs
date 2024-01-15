using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wolverine;

namespace RCommon.Wolverine.Producers
{
    public class SendWithWolverineEventProducer : IEventProducer
    {
        private readonly IMessageBus _messageBus;

        public SendWithWolverineEventProducer(IMessageBus messageBus)
        {
            _messageBus = messageBus;
        }


        public async Task ProduceEventAsync<T>(T @event, CancellationToken cancellationToken = default) where T : ISerializableEvent
        {
            await _messageBus.SendAsync(@event);
        }
    }
}
