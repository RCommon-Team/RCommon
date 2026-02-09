using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Events
{
    /// <summary>
    /// Marker interface for events that are dispatched and handled synchronously
    /// within the same process boundary.
    /// </summary>
    /// <remarks>
    /// Extends <see cref="ISerializableEvent"/> to allow synchronous events to
    /// optionally participate in serialization scenarios (e.g., logging, auditing).
    /// </remarks>
    /// <seealso cref="IAsyncEvent"/>
    /// <seealso cref="ISerializableEvent"/>
    public interface ISyncEvent : ISerializableEvent
    {
    }
}
