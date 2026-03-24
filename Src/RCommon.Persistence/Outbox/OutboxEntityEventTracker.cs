using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Entities;
using RCommon.EventHandling.Producers;

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

    /// <summary>
    /// Initializes a new instance of <see cref="OutboxEntityEventTracker"/>.
    /// </summary>
    /// <param name="inner">The inner in-memory tracker that manages the entity collection.</param>
    /// <param name="outboxRouter">The outbox router used to buffer and persist events.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="inner"/> or <paramref name="outboxRouter"/> is <c>null</c>.
    /// </exception>
    public OutboxEntityEventTracker(InMemoryEntityEventTracker inner, OutboxEventRouter outboxRouter)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _outboxRouter = outboxRouter ?? throw new ArgumentNullException(nameof(outboxRouter));
    }

    /// <inheritdoc />
    public void AddEntity(IBusinessEntity entity) => _inner.AddEntity(entity);

    /// <inheritdoc />
    public ICollection<IBusinessEntity> TrackedEntities => _inner.TrackedEntities;

    /// <inheritdoc />
    /// <remarks>
    /// Walks the object graph of each tracked entity to collect domain events, buffers them in the
    /// <see cref="OutboxEventRouter"/>, then flushes the buffer to the <see cref="IOutboxStore"/>
    /// within the active transaction (Phase 1).
    /// </remarks>
    public async Task PersistEventsAsync(CancellationToken cancellationToken = default)
    {
        // Walk entity graph and collect events into the router buffer
        foreach (var entity in _inner.TrackedEntities)
        {
            var entityGraph = entity.TraverseGraphFor<IBusinessEntity>();
            foreach (var graphEntity in entityGraph)
            {
                _outboxRouter.AddTransactionalEvents(graphEntity.LocalEvents);
            }
        }

        // Flush buffer to outbox store (within the active transaction)
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
}
