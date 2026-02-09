using RCommon.EventHandling.Producers;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
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
    /// </remarks>
    public class InMemoryEntityEventTracker : IEntityEventTracker
    {
        private readonly ICollection<IBusinessEntity> _businessEntities = new List<IBusinessEntity>();
        private readonly IEventRouter _eventRouter;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryEntityEventTracker"/>.
        /// </summary>
        /// <param name="eventRouter">The event router used to dispatch collected transactional events.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="eventRouter"/> is <c>null</c>.</exception>
        public InMemoryEntityEventTracker(IEventRouter eventRouter)
        {
            this._eventRouter = eventRouter ?? throw new ArgumentNullException(nameof(eventRouter));
        }

        /// <inheritdoc />
        public void AddEntity(IBusinessEntity entity)
        {
            Guard.Against<ArgumentNullException>(entity == null, $"Entity of type {entity?.GetGenericTypeName()} cannot be null");

            // Only track entities that have opted in to event tracking
            if (entity!.AllowEventTracking)
            {
                _businessEntities.Add(entity);
            }

        }

        /// <inheritdoc />
        public ICollection<IBusinessEntity> TrackedEntities { get => _businessEntities; }

        /// <inheritdoc />
        /// <remarks>
        /// Traverses the object graph of each tracked entity to discover nested <see cref="IBusinessEntity"/>
        /// instances, collects their local events, and routes all events through the <see cref="IEventRouter"/>.
        /// </remarks>
        public async Task<bool> EmitTransactionalEventsAsync()
        {
            // Walk each tracked root entity and traverse its object graph for nested IBusinessEntity instances
            foreach (var entity in this._businessEntities)
            {
                var entityGraph = entity.TraverseGraphFor<IBusinessEntity>();

                // Collect local events from every entity in the graph (root + children)
                foreach (var graphEntity in entityGraph)
                {
                    _eventRouter.AddTransactionalEvents(graphEntity.LocalEvents);
                }
            }
            await _eventRouter.RouteEventsAsync();
            return await Task.FromResult(true);
        }
    }
}
