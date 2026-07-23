using RCommon.Models.Events;

namespace Examples.EventHandling.NoUnitOfWork;

/// <summary>
/// A simple in-process event. Implements <see cref="ISyncEvent"/> (which itself is an
/// <see cref="ISerializableEvent"/>) so it satisfies the <c>AddSubscriber</c> constraint.
/// </summary>
public class NotificationRequested : ISyncEvent
{
    public NotificationRequested(Guid recipientId, string message)
    {
        RecipientId = recipientId;
        Message = message;
    }

    public Guid RecipientId { get; }

    public string Message { get; }
}
