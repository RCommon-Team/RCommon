using MassTransit;
using RCommon.Models.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit.Subscribers
{
    /// <summary>
    /// Non-generic marker interface for MassTransit event handlers within the RCommon framework.
    /// </summary>
    public interface IMassTransitEventHandler
    {
    }

    /// <summary>
    /// Generic marker interface for MassTransit event handlers that process a specific distributed event type.
    /// </summary>
    /// <typeparam name="TDistributedEvent">The distributed event type to handle. Must implement <see cref="ISerializableEvent"/>.</typeparam>
    public interface IMassTransitEventHandler<in TDistributedEvent> : IMassTransitEventHandler
        where TDistributedEvent : class, ISerializableEvent
    {
    }
}
