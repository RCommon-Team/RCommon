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
        /// Back-compat projection — prefer <see cref="TrackedEntitiesWithDataStore"/> for datastore-aware access.
        /// </summary>
        ICollection<IBusinessEntity> TrackedEntities { get; }

        /// <summary>
        /// The collection of entity/datastore pairs captured at <see cref="AddEntity(IBusinessEntity,string?)"/> time.
        /// A null <c>DataStoreName</c> is a sentinel meaning "use default, resolve downstream" (AC-8, AC-17).
        /// </summary>
        IReadOnlyCollection<(IBusinessEntity Entity, string? DataStoreName)> TrackedEntitiesWithDataStore { get; }

        /// <summary>
        /// Adds an entity that can be tracked for any new events associated with it.
        /// The entity is recorded with a null datastore sentinel (resolve default downstream).
        /// </summary>
        /// <param name="entity">The business entity to track for transactional events.</param>
        void AddEntity(IBusinessEntity entity);

        /// <summary>
        /// Adds an entity that can be tracked for any new events associated with it,
        /// and associates it with the named datastore so events can later be persisted to
        /// the correct outbox store. Pass <c>null</c> to use the default store (resolved downstream).
        /// </summary>
        /// <param name="entity">The business entity to track for transactional events.</param>
        /// <param name="dataStoreName">
        /// The name of the datastore that owns this entity, or <c>null</c> to signal
        /// "use default, resolve downstream" (AC-8).
        /// </param>
        void AddEntity(IBusinessEntity entity, string? dataStoreName);

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
