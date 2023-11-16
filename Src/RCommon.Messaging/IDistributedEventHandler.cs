
using System.Threading.Tasks;

namespace RCommon.Messaging
{
    public interface IDistributedEventHandler<in TDistributedEvent> : IDistributedEventHandler
        where TDistributedEvent : DistributedEvent
    {
        
    }

    public interface IDistributedEventHandler
    {
    }
}
