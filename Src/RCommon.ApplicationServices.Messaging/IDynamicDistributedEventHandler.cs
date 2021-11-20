using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Messaging
{
    public interface IDynamicDistributedEventHandler
    {
        Task Handle(dynamic eventData);
    }
}
