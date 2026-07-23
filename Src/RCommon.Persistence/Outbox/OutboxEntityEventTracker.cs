using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// A decorator over <see cref="InMemoryEntityEventTracker"/> that implements the two-phase
/// transactional outbox pattern for domain event persistence.
/// </summary>
/// <remarks>
/// <para>This tracker adds two-phase commit behaviour on top of the in-memory tracker:</para>
/// <list type="bullet">
///   <item><description>
///     <see cref="PersistEventsAsync"/> (Phase 1, within transaction): Walks each tracked entity's
///     object graph to collect domain events, adds them to the <see cref="OutboxEventRouter"/> buffer,
///     then calls <see cref="OutboxEventRouter.PersistBufferedEventsAsync"/> to flush them to the
///     <see cref="IOutboxStore"/> within the active transaction.
///   </description></item>
///   <item><description>
///     <see cref="EmitTransactionalEventsAsync"/> (Phase 3, post-commit): Delegates to
///     <see cref="OutboxEventRouter.RouteEventsAsync()"/> which reads pending messages from the store
///     and dispatches them to registered event producers.
///   </description></item>
/// </list>
/// </remarks>
public class OutboxEntityEventTracker : IEntityEventTracker
{
    private readonly InMemoryEntityEventTracker _inner;
    private readonly OutboxEventRouter _outboxRouter;
    private readonly InMemoryTransactionalEventRouter _inProcessRouter;
    private readonly IEventRoutingRegistry _routingRegistry;

    /// <summary>
    /// Initializes a new instance of <see cref="OutboxEntityEventTracker"/>.
    /// </summary>
    /// <param name="inner">The inner in-memory tracker that manages the entity collection.</param>
    /// <param name="outboxRouter">The outbox router used to buffer and persist events (durable dispatch).</param>
    /// <param name="inProcessRouter">
    /// The in-process transactional router used to dispatch transient events through the Phase-2 FIFO
    /// drain. Injected as the CONCRETE type (not the <see cref="IEventRouter"/> alias) so the tracker's
    /// transient dispatcher is independent of whatever <see cref="IEventRouter"/> resolves to in the host.
    /// </param>
    /// <param name="routingRegistry">The registry describing which event types are durable and their target datastore.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/>, <paramref name="outboxRouter"/>, <paramref name="inProcessRouter"/>,
    /// or <paramref name="routingRegistry"/> is <c>null</c>.
    /// </exception>
    public OutboxEntityEventTracker(
        InMemoryEntityEventTracker inner,
        OutboxEventRouter outboxRouter,
        InMemoryTransactionalEventRouter inProcessRouter,
        IEventRoutingRegistry routingRegistry)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _outboxRouter = outboxRouter ?? throw new ArgumentNullException(nameof(outboxRouter));
        _inProcessRouter = inProcessRouter ?? throw new ArgumentNullException(nameof(inProcessRouter));
        _routingRegistry = routingRegistry ?? throw new ArgumentNullException(nameof(routingRegistry));
    }

    /// <inheritdoc />
    public void AddEntity(IBusinessEntity entity) => _inner.AddEntity(entity);

    /// <inheritdoc />
    public void AddEntity(IBusinessEntity entity, string? dataStoreName) => _inner.AddEntity(entity, dataStoreName);

    /// <inheritdoc />
    public ICollection<IBusinessEntity> TrackedEntities => _inner.TrackedEntities;

    /// <inheritdoc />
    public IReadOnlyCollection<(IBusinessEntity Entity, string? DataStoreName)> TrackedEntitiesWithDataStore
        => _inner.TrackedEntitiesWithDataStore;

    /// <inheritdoc />
    /// <remarks>
    /// Persists exactly the DURABLE events that were buffered into the <see cref="OutboxEventRouter"/> during
    /// <see cref="DispatchDomainEventsAsync"/> (including any durable events raised by a handler mid-dispatch —
    /// AC-5) to the <see cref="IOutboxStore"/> within the active transaction (Phase 1). Entity graphs are NOT
    /// re-harvested here — durability partitioning already happened during dispatch.
    /// </remarks>
    public async Task PersistEventsAsync(CancellationToken cancellationToken = default)
    {
        await _outboxRouter.PersistBufferedEventsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="OutboxEventRouter.RouteEventsAsync()"/> which reads pending messages
    /// from the <see cref="IOutboxStore"/> and dispatches them to registered event producers
    /// (Phase 3, post-commit).
    /// </remarks>
    public async Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default)
    {
        await _outboxRouter.RouteEventsAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Pre-commit dispatch with per-event durability partitioning (Phase 3a). Traverses each tracked entity's
    /// object graph. For every graph node it subscribes to <see cref="BusinessEntity.TransactionalEventAdded"/>
    /// (so events raised by a handler DURING the drain are partitioned too) and seeds each already-present
    /// <see cref="IBusinessEntity.LocalEvents"/> through the route-by-durability rule. It then drains the
    /// in-process FIFO so transient events are dispatched pre-commit (ordered, cascade-limited). Handlers are
    /// always unsubscribed afterwards.
    /// </para>
    /// <para>
    /// Route-by-durability: a durable event (one with an outbox route registered in the
    /// <see cref="IEventRoutingRegistry"/>) is buffered to the <see cref="OutboxEventRouter"/> — never enqueued
    /// into the in-process FIFO, so it is never dispatched pre-commit (persisted here, relayed post-commit). A
    /// transient event is enqueued into the in-process FIFO and dispatched pre-commit only.
    /// </para>
    /// </remarks>
    public async Task DispatchDomainEventsAsync(CancellationToken cancellationToken = default)
    {
        // Map every graph node (root or child) to the datastore of the aggregate it belongs to, so a
        // handler-raised event mid-dispatch can be routed against the raising entity's datastore. Falls back
        // to the null/default path when the raising entity is not a tracked graph node (documented choice).
        var entityDataStore = new Dictionary<IBusinessEntity, string?>(ReferenceEqualityComparer.Instance);
        var subscribed = new List<BusinessEntity>();

        // Route a single event by durability. store non-null => durable (buffer to outbox); else transient (FIFO).
        void Route(RCommon.Models.Events.ISerializableEvent e, string? dataStoreName)
        {
            if (_routingRegistry.TryGetOutboxStore(e.GetType(), out var store) && store is not null)
            {
                // Co-location / atomicity: if the aggregate is tracked under a datastore, it MUST match the
                // event's declared outbox store — otherwise the outbox row cannot be written in the same
                // transaction as the aggregate. Fail loud rather than silently split the transaction.
                if (!string.IsNullOrWhiteSpace(dataStoreName)
                    && !string.Equals(dataStoreName, store, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Event '{e.GetType().FullName}' is configured via Publish<...>().UseOutbox(\"{store}\") " +
                        $"but its aggregate is tracked under datastore \"{dataStoreName}\". Co-location (atomicity) " +
                        $"requires the durable event's outbox store to match the aggregate's datastore. Change the " +
                        $"UseOutbox store to \"{dataStoreName}\" (or move the aggregate to \"{store}\") so the outbox " +
                        $"row is written in the same transaction as the aggregate.");
                }

                // Co-location wins: persist to the ENTITY's datastore when known; otherwise use the declared store.
                _outboxRouter.AddTransactionalEvent(e, string.IsNullOrWhiteSpace(dataStoreName) ? store : dataStoreName);
            }
            else
            {
                _inProcessRouter.AddTransactionalEvent(e);
            }
        }

        void Handler(object? sender, TransactionalEventsChangedEventArgs args)
        {
            // Resolve the raising entity's datastore; default (null) path if it is not a known tracked node.
            entityDataStore.TryGetValue(args.Entity, out var raisingStore);
            Route(args.EventData, raisingStore);
        }

        try
        {
            foreach (var (entity, dataStoreName) in _inner.TrackedEntitiesWithDataStore)
            {
                foreach (var graphEntity in entity.TraverseGraphFor<IBusinessEntity>())
                {
                    entityDataStore[graphEntity] = dataStoreName;

                    if (graphEntity is BusinessEntity businessEntity)
                    {
                        businessEntity.TransactionalEventAdded += Handler;
                        subscribed.Add(businessEntity);
                    }

                    foreach (var localEvent in graphEntity.LocalEvents)
                    {
                        Route(localEvent, dataStoreName); // seed => generation 0 (transient) or outbox buffer (durable)
                    }
                }
            }

            // Drain transient events pre-commit. Durable events were buffered to the outbox and are NOT in this FIFO.
            await _inProcessRouter.RouteEventsAsync(cancellationToken).ConfigureAwait(false);
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
