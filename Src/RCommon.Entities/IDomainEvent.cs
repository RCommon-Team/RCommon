using System;
using RCommon.Models.Events;

namespace RCommon.Entities
{
    /// <summary>
    /// Represents a domain event raised by an aggregate root.
    /// Extends ISerializableEvent for compatibility with the existing event routing pipeline.
    /// </summary>
    public interface IDomainEvent : ISerializableEvent
    {
        /// <summary>
        /// Unique identifier for this event instance.
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// The date and time when this event occurred.
        /// </summary>
        DateTimeOffset OccurredOn { get; }
    }
}
