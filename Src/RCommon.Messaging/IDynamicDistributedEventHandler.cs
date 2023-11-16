using System.Threading.Tasks;

namespace RCommon.Messaging
{
    public interface IDynamicDistributedEventHandler
    {
        Task Handle(dynamic eventData);
    }
}
