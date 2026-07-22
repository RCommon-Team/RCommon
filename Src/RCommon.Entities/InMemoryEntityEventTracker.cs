using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    /// <summary>
    /// In-memory implementation of <see cref="IEntityEventTracker"/> that collects entities and
    /// emits their transactional events through an <see cref="IEventRouter"/>.
    /// </summary>
    /// <remarks>
    /// Entities are held in memory for the lifetime of this tracker instance. When
    /// <see cref="EmitTransactionalEventsAsync"/> is called, the tracker traverses each entity's
    /// object graph to collect all local events and routes them via the configured
    /// <see cref="IEventRouter"/>.
    /// <para>
    /// As of AC-8, each entity is stored alongside its associated datastore name so that
    /// downstream outbox routing can group events by datastore. A null datastore name is a
    /// sentinel meaning "use default, resolve downstream" — no dependency on
    /// <c>DefaultDataStoreOptions</c> or <c>RCommon.Persistence</c> is introduced here.
    /// </para>
    /// </remarks>
    public class InMemoryEntityEventTracker : IEntityEventTracker
    {
        private readonly List<(IBusinessEntity Entity, string? DataStoreName)> _trackedPairs
            = new List<(IBusinessEntity Entity, string? DataStoreName)>();

        private readonly IEventRouter _eventRouter;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryEntityEventTracker"/>.
        /// </summary>
        /// <param name="eventRouter">The event router used to dispatch collected transactional events.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventRouter"/> is <c>null</c>.</exception>
        public InMemoryEntityEventTracker(IEventRouter eventRouter)
        {
            _eventRouter = eventRouter ?? throw new ArgumentNullException(nameof(eventRouter));
        }

        /// <inheritdoc />
        /// <remarks>
        /// Delegates to <see cref="AddEntity(IBusinessEntity, string?)"/> with a null datastore
        /// sentinel (AC-17 back-compat — resolves default downstream).
        /// </remarks>
        public void AddEntity(IBusinessEntity entity) => AddEntity(entity, null);

        /// <inheritdoc />
        public void AddEntity(IBusinessEntity entity, string? dataStoreName)
        {
            Guard.Against<ArgumentNullException>(entity == null, $"Entity of type {entity?.GetGenericTypeName()} cannot be null");

            // Only track entities that have opted in to event tracking
            if (entity!.AllowEventTracking)
            {
                _trackedPairs.Add((entity, dataStoreName));
            }
        }

        /// <inheritdoc />
        /// <remarks>
        /// Back-compat projection over <see cref="TrackedEntitiesWithDataStore"/>.
        /// </remarks>
        public ICollection<IBusinessEntity> TrackedEntities
            => _trackedPairs.Select(p => p.Entity).ToList();

        /// <inheritdoc />
        public IReadOnlyCollection<(IBusinessEntity Entity, string? DataStoreName)> TrackedEntitiesWithDataStore
            => _trackedPairs.AsReadOnly();

        /// <inheritdoc />
        /// <remarks>
        /// The in-memory implementation is a no-op. The transactional outbox decorator
        /// (<c>OutboxEntityEventTracker</c>) overrides this to persist events within the active
        /// transaction before it is committed.
        /// </remarks>
        public Task PersistEventsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        /// <inheritdoc />
        /// <remarks>
        /// Traverses the object graph of each tracked entity to discover nested <see cref="IBusinessEntity"/>
        /// instances, collects their local events, and routes all events through the <see cref="IEventRouter"/>.
        /// </remarks>
        public async Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default)
        {
            // Walk each tracked root entity and traverse its object graph for nested IBusinessEntity instances
            foreach (var (entity, _) in _trackedPairs)
            {
                var entityGraph = entity.TraverseGraphFor<IBusinessEntity>();

                // Collect local events from every entity in the graph (root + children)
                foreach (var graphEntity in entityGraph)
                {
                    _eventRouter.AddTransactionalEvents(graphEntity.LocalEvents);
                }
            }
            await _eventRouter.RouteEventsAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
