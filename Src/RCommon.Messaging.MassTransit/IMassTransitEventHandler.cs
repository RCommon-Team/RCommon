using MassTransit;
using RCommon.EventHandling;
using RCommon.EventHandling.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Messaging.MassTransit
{
    public interface IMassTransitEventHandler<in TDistributedEvent> : IDistributedEventHandler, IConsumer<TDistributedEvent>
        where TDistributedEvent : DistributedEvent
    {
    }
}
