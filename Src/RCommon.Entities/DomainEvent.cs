using System;

namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base record for domain events. Provides default values for EventId and OccurredOn.
    /// Use as a base for all concrete domain events.
    /// </summary>
    public abstract record DomainEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
