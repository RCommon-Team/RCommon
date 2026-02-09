using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models.Events
{
    /// <summary>
    /// Marker interface indicating that an event can be serialized for transport
    /// across process boundaries (e.g., message queues, event stores, distributed systems).
    /// </summary>
    /// <seealso cref="IAsyncEvent"/>
    /// <seealso cref="ISyncEvent"/>
    public interface ISerializableEvent
    {
    }
}
