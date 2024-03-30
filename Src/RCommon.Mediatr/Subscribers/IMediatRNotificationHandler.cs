using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR.Subscribers
{
    public interface IMediatRNotificationHandler
    {
    }

    public interface IMediatREventHandler<in TDistributedEvent> : IMediatRNotificationHandler 
        where TDistributedEvent : class, ISerializableEvent
    {
    }
}
