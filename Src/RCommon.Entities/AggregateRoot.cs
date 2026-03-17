using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base class for aggregate roots. Extends BusinessEntity to reuse event tracking,
    /// key support, and entity equality. Adds versioning for optimistic concurrency and typed
    /// domain event methods.
    /// </summary>
    /// <typeparam name="TKey">The type of the aggregate's identity.</typeparam>
    [Serializable]
    public abstract class AggregateRoot<TKey> : BusinessEntity<TKey>, IAggregateRoot<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>
        /// Initializes a new instance of <see cref="AggregateRoot{TKey}"/> with a default key.
        /// </summary>
        protected AggregateRoot() : base() { }

        /// <summary>
        /// Initializes a new instance of <see cref="AggregateRoot{TKey}"/> with the specified key.
        /// </summary>
        /// <param name="id">The primary key value for this aggregate root.</param>
        protected AggregateRoot(TKey id) : base(id) { }

        /// <summary>
        /// Version number for optimistic concurrency control. Incremented via <see cref="IncrementVersion"/>.
        /// Decorated with [ConcurrencyCheck] to signal ORM-level concurrency checking.
        /// </summary>
        [ConcurrencyCheck]
        public virtual int Version { get; protected set; }

        /// <summary>
        /// Returns the domain events that have been raised by this aggregate but not yet dispatched.
        /// </summary>
        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents
            => _domainEvents.AsReadOnly();

        /// <summary>
        /// Raises a domain event on this aggregate. The event is added to both the DomainEvents
        /// collection and the base LocalEvents collection for dispatch via the event tracking pipeline.
        /// </summary>
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
            AddLocalEvent(domainEvent);
        }

        /// <summary>
        /// Removes a previously raised domain event before it has been dispatched.
        /// </summary>
        protected void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
            RemoveLocalEvent(domainEvent);
        }

        /// <summary>
        /// Clears all pending domain events from this aggregate.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
            ClearLocalEvents();
        }

        /// <summary>
        /// Increments the version number for optimistic concurrency control.
        /// Call this when the aggregate's state changes.
        /// Note: This is not thread-safe. Aggregates are designed for single-threaded access.
        /// </summary>
        protected void IncrementVersion()
            => Version++;

        /// <summary>
        /// Determines whether this aggregate root is equal to another entity based on identity (Id).
        /// Two aggregate roots of the same type with the same Id are considered equal.
        /// This overrides the default <see cref="BusinessEntity.EntityEquals"/> binary comparison
        /// to support value-based identity equality consistent with DDD principles.
        /// </summary>
        /// <param name="other">The other entity to compare against.</param>
        /// <returns><c>true</c> if the entities have the same type and Id; otherwise, <c>false</c>.</returns>
        public new bool EntityEquals(IBusinessEntity other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (other is IBusinessEntity<TKey> typedOther)
                return Id.Equals(typedOther.Id);

            return false;
        }
    }
}
