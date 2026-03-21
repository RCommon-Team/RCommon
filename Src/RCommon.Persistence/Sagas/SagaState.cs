using System;

namespace RCommon.Persistence.Sagas;

public abstract class SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string CurrentStep { get; set; } = default!;
    public bool IsCompleted { get; set; }
    public bool IsFaulted { get; set; }
    public string? FaultReason { get; set; }
    public int Version { get; set; }
}
