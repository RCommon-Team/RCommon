using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
{
    public interface IMediatREventHandler
    {
    }

    public interface IMediatREventHandler<in TDistributedEvent> : IMediatREventHandler 
        where TDistributedEvent : class, ISerializableEvent
    {
    }
}
