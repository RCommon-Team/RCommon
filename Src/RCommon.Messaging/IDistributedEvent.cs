
namespace RCommon.Messaging
{
    public interface IDistributedEvent
    {
        DateTime CreationDate { get; }
        Guid Id { get; }
    }
}
