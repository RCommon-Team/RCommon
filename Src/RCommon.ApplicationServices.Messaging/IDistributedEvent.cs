
namespace RCommon.ApplicationServices.Messaging
{
    public interface IDistributedEvent
    {
        DateTime CreationDate { get; }
        Guid Id { get; }
    }
}