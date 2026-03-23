using System;

namespace RCommon.Persistence.Inbox;

public interface IInboxMessage
{
    Guid MessageId { get; }
    string EventType { get; }
    string? ConsumerType { get; }
    DateTimeOffset ReceivedAtUtc { get; }
}
