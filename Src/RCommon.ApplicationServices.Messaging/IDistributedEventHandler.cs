
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging
{
    public interface IDistributedEventHandler<in TDistributedEvent> : IDistributedEventHandler
        where TDistributedEvent : DistributedEvent
    {
        Task Handle(TDistributedEvent @event);
    }

    public interface IDistributedEventHandler
    {
    }
}
