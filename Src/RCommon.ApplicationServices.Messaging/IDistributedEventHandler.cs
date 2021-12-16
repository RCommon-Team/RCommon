
using MassTransit;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging
{
    public interface IDistributedEventHandler<in TDistributedEvent> : IDistributedEventHandler, IConsumer<TDistributedEvent>
        where TDistributedEvent : DistributedEvent
    {
        
    }

    public interface IDistributedEventHandler
    {
    }
}
