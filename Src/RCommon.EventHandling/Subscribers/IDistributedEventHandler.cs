
using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{
    public interface IDistributedEventHandler<in TDistributedEvent> : IDistributedEventHandler
        where TDistributedEvent : DistributedEvent
    {

    }

    public interface IDistributedEventHandler
    {
    }
}
