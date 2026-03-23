using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Security.Claims;

namespace RCommon.Persistence.Outbox;

/// <summary>
/// An <see cref="IEventRouter"/> implementation that persists events to an outbox store
/// before dispatching them to <see cref="IEventProducer"/> instances.
/// </summary>
/// <remarks>
/// <para>This router follows the transactional outbox pattern:</para>
/// <list type="bullet">
///   <item><description><see cref="AddTransactionalEvent"/> and <see cref="AddTransactionalEvents"/> buffer events
///   in-memory without touching the store (called during business logic).</description></item>
///   <item><description><see cref="PersistBufferedEventsAsync"/> drains the buffer and writes
///   <see cref="OutboxMessage"/> rows to <see cref="IOutboxStore"/> within the active transaction (Phase 1).</description></item>
///   <item><description><see cref="RouteEventsAsync()"/> dispatches the retained events (kept in memory after
///   <see cref="PersistBufferedEventsAsync"/>) to producers and marks each message processed on success — no store
///   reads are performed; failures are logged and retried by the background processor (Phase 3, post-commit).</description></item>
///   <item><description><see cref="RouteEventsAsync(IEnumerable{ISerializableEvent}, CancellationToken)"/> performs direct
///   dispatch without touching the store (for non-outbox routing scenarios).</description></item>
/// </list>
/// <para>This should be registered as a scoped dependency.</para>
/// </remarks>
public class OutboxEventRouter : IEventRouter
{
    private readonly IOutboxStore _outboxStore;
    private readonly IOutboxSerializer _serializer;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ITenantIdAccessor _tenantIdAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly EventSubscriptionManager _subscriptionManager;
    private readonly ILogger<OutboxEventRouter> _logger;
    private readonly OutboxOptions _options;
    private readonly ConcurrentQueue<ISerializableEvent> _buffer = new();
    private readonly List<(Guid MessageId, ISerializableEvent Event)> _persistedEvents = new();

    public OutboxEventRouter(
        IOutboxStore outboxStore,
        IOutboxSerializer serializer,
        IGuidGenerator guidGenerator,
        ITenantIdAccessor tenantIdAccessor,
        IServiceProvider serviceProvider,
        EventSubscriptionManager subscriptionManager,
        ILogger<OutboxEventRouter> logger,
        IOptions<OutboxOptions> options)
    {
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _guidGenerator = guidGenerator ?? throw new ArgumentNullException(nameof(guidGenerator));
        _tenantIdAccessor = tenantIdAccessor ?? throw new ArgumentNullException(nameof(tenantIdAccessor));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public void AddTransactionalEvent(ISerializableEvent serializableEvent)
    {
        Guard.IsNotNull(serializableEvent, nameof(serializableEvent));
        _buffer.Enqueue(serializableEvent);
    }

    /// <inheritdoc />
    public void AddTransactionalEvents(IEnumerable<ISerializableEvent> serializableEvents)
    {
        Guard.IsNotNull(serializableEvents, nameof(serializableEvents));
        foreach (var e in serializableEvents)
        {
            AddTransactionalEvent(e);
        }
    }

    /// <summary>
    /// Drains the in-memory buffer and writes each event as an <see cref="OutboxMessage"/> to the
    /// <see cref="IOutboxStore"/>. This must be called within the active database transaction (UnitOfWork Phase 1).
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task PersistBufferedEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<ISerializableEvent>();
        while (_buffer.TryDequeue(out var e))
        {
            events.Add(e);
        }

        foreach (var @event in events)
        {
            var message = new OutboxMessage
            {
                Id = _guidGenerator.Create(),
                EventType = _serializer.GetEventTypeName(@event),
                EventPayload = _serializer.Serialize(@event),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                TenantId = _tenantIdAccessor.GetTenantId()
                // Note: CorrelationId population is left for a future enhancement (V2)
            };

            _logger.LogDebug("Persisting outbox message {Id} for event {EventType}", message.Id, message.EventType);
            await _outboxStore.SaveAsync(message, cancellationToken).ConfigureAwait(false);
            _persistedEvents.Add((message.Id, @event));
        }
    }

    /// <summary>
    /// Dispatches retained events that were persisted during <see cref="PersistBufferedEventsAsync"/> to registered
    /// <see cref="IEventProducer"/> instances, and marks each message processed on success. Events are dispatched
    /// from the in-memory retained list — no store reads are performed. This should be called post-commit
    /// (UnitOfWork Phase 3). If dispatch fails for a message, a warning is logged and the background processor
    /// will retry via <see cref="IOutboxStore.ClaimAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task RouteEventsAsync(CancellationToken cancellationToken = default)
    {
        if (_persistedEvents.Count == 0) return;

        _logger.LogInformation("OutboxEventRouter dispatching {Count} retained messages", _persistedEvents.Count);

        var producers = _serviceProvider.GetServices<IEventProducer>();

        foreach (var (messageId, @event) in _persistedEvents)
        {
            try
            {
                var filteredProducers = _subscriptionManager.HasSubscriptions
                    ? _subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                    : producers;

                foreach (var producer in filteredProducers)
                {
                    await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
                }

                await _outboxStore.MarkProcessedAsync(messageId, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Best-effort dispatch failed for message {Id}; background processor will retry", messageId);
            }
        }

        _persistedEvents.Clear();
    }

    /// <summary>
    /// Dispatches the provided events directly to registered <see cref="IEventProducer"/> instances
    /// without interacting with the outbox store. Used for non-outbox routing scenarios.
    /// </summary>
    /// <param name="transactionalEvents">The events to dispatch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task RouteEventsAsync(IEnumerable<ISerializableEvent> transactionalEvents, CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(transactionalEvents, nameof(transactionalEvents));

        var producers = _serviceProvider.GetServices<IEventProducer>();

        foreach (var @event in transactionalEvents)
        {
            var filteredProducers = _subscriptionManager.HasSubscriptions
                ? _subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                : producers;

            foreach (var producer in filteredProducers)
            {
                await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
