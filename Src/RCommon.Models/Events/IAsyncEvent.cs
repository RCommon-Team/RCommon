using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Events
{
    /// <summary>
    /// Marker interface for events that are dispatched and handled asynchronously.
    /// Async events are typically published to a message bus or queue for eventual processing.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="ISerializableEvent"/> because asynchronous delivery mechanisms
    /// (e.g., message brokers) generally require events to be serializable.
    /// </remarks>
    /// <seealso cref="ISyncEvent"/>
    /// <seealso cref="ISerializableEvent"/>
    public interface IAsyncEvent : ISerializableEvent
    {
    }
}
