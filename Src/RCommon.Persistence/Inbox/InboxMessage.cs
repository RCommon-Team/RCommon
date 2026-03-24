using System;

namespace RCommon.Persistence.Inbox;

public class InboxMessage : IInboxMessage
{
    public Guid MessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ConsumerType { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}
