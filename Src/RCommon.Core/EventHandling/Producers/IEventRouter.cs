using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.EventHandling.Producers
{
    public interface IEventRouter
    {
        Task RouteEvents(IEnumerable<ISerializableEvent> localEvents);
    }
}