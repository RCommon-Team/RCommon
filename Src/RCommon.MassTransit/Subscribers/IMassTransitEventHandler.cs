using MassTransit;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit.Subscribers
{
    public interface IMassTransitEventHandler
    {
    }

    public interface IMassTransitEventHandler<in TDistributedEvent> : IMassTransitEventHandler, IConsumer<TDistributedEvent> 
        where TDistributedEvent : class, ISerializableEvent
    {
    }
}
