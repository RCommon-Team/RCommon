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
    /// <see cref="DispatchDomainEventsAsync"/> is called (pre-commit), the tracker traverses each entity's
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
        /// No-op for the in-memory tracker. In-process domain-event dispatch now happens pre-commit in
        /// <see cref="DispatchDomainEventsAsync"/>; this method is retained for interface and outbox-decorator
        /// compatibility and always reports success.
        /// </remarks>
        public Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        /// <inheritdoc />
        /// <remarks>
        /// Subscribes to <see cref="BusinessEntity.TransactionalEventAdded"/> on every entity in every tracked graph
        /// so that events raised DURING the drain (by a handler mutating a seeded entity) flow into the same
        /// <see cref="IEventRouter"/> FIFO. The queue is first seeded from the local events already present at commit
        /// time, then drained to empty via <see cref="IEventRouter.RouteEventsAsync(CancellationToken)"/>. Handlers
        /// are always unsubscribed afterwards.
        /// <para>
        /// The entire seed loop (attach handler + enqueue existing local events for every graph entity) completes
        /// before the drain is awaited, so by the time any handler runs all seeded entities are already subscribed.
        /// Seeding reads <see cref="IBusinessEntity.LocalEvents"/> directly (it does not call
        /// <c>AddLocalEvent</c>), so no event is double-enqueued. The mid-drain capture only applies to entities
        /// deriving from <see cref="BusinessEntity"/> (the source of the <c>TransactionalEventAdded</c> notification);
        /// their already-present local events are seeded regardless of concrete type.
        /// </para>
        /// </remarks>
        public async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
        {
            var subscribed = new List<BusinessEntity>();
            void Handler(object? sender, TransactionalEventsChangedEventArgs args)
                => _eventRouter.AddTransactionalEvent(args.EventData);

            try
            {
                foreach (var (entity, _) in _trackedPairs)
                {
                    foreach (var graphEntity in entity.TraverseGraphFor<IBusinessEntity>())
                    {
                        // Subscribe for mid-drain capture where the notification is available (BusinessEntity).
                        if (graphEntity is BusinessEntity businessEntity)
                        {
                            businessEntity.TransactionalEventAdded += Handler;
                            subscribed.Add(businessEntity);
                        }

                        foreach (var localEvent in graphEntity.LocalEvents)
                        {
                            _eventRouter.AddTransactionalEvent(localEvent); // seed => generation 0
                        }
                    }
                }

                await _eventRouter.RouteEventsAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                foreach (var entity in subscribed)
                {
                    entity.TransactionalEventAdded -= Handler;
                }
            }
        }
    }
}
