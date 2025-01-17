using MassTransit;
using RCommon.Models.Events;
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

    public interface IMassTransitEventHandler<in TDistributedEvent> : IMassTransitEventHandler 
        where TDistributedEvent : class, ISerializableEvent
    {
    }
}
