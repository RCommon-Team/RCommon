using System.Threading.Tasks;

namespace RCommon.EventHandling.Subscribers
{
    public interface IDynamicDistributedEventHandler
    {
        Task Handle(dynamic eventData);
    }
}
