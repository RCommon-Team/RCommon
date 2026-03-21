using RCommon.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    /// <summary>
    /// Defines a mechanism for tracking entities and emitting their transactional (local) events
    /// through the event routing infrastructure.
    /// </summary>
    /// <seealso cref="InMemoryEntityEventTracker"/>
    public interface IEntityEventTracker
    {
        /// <summary>
        /// The collection of entities that each may store a collection of events.
        /// </summary>
        ICollection<IBusinessEntity> TrackedEntities { get; }

        /// <summary>
        /// Adds an entity that can be tracked for any new events associated with it.
        /// </summary>
        /// <param name="entity">The business entity to track for transactional events.</param>
        void AddEntity(IBusinessEntity entity);

        /// <summary>
        /// Persists domain events to the outbox (or equivalent durable store) within the active
        /// transaction, before the transaction is committed. The in-memory implementation is a no-op.
        /// </summary>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        Task PersistEventsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Publishes the events associated with each entity being tracked.
        /// </summary>
        /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
        /// <returns>True if successful</returns>
        Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default);
    }
}
