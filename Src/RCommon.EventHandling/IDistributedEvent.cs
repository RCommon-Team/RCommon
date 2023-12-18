
namespace RCommon.EventHandling
{
    public interface IDistributedEvent
    {
        DateTime CreationDate { get; }
        Guid Id { get; }
    }
}
