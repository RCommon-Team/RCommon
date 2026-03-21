using System;

namespace RCommon.Persistence.Outbox;

public interface IOutboxMessage
{
    Guid Id { get; }
    string EventType { get; }
    string EventPayload { get; }
    DateTimeOffset CreatedAtUtc { get; }
    DateTimeOffset? ProcessedAtUtc { get; set; }
    DateTimeOffset? DeadLetteredAtUtc { get; set; }
    string? ErrorMessage { get; set; }
    int RetryCount { get; set; }
    string? CorrelationId { get; set; }
    string? TenantId { get; set; }
}
