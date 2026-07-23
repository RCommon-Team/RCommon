using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon;
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
    private readonly string _dataStoreName;
    private readonly ConcurrentQueue<(ISerializableEvent Event, string? DataStoreName)> _buffer = new();
    private readonly List<(Guid MessageId, ISerializableEvent Event, string DataStoreName)> _persistedEvents = new();

    // Separator used to join the FullName of each matched producer into the OutboxMessage.TargetProducers
    // column. The Task 9 poller splits on this same separator, so keep it a simple comma-delimited list.
    private const char TargetProducersSeparator = ',';

    public OutboxEventRouter(
        IOutboxStore outboxStore,
        IOutboxSerializer serializer,
        IGuidGenerator guidGenerator,
        ITenantIdAccessor tenantIdAccessor,
        IServiceProvider serviceProvider,
        EventSubscriptionManager subscriptionManager,
        ILogger<OutboxEventRouter> logger,
        IOptions<OutboxOptions> options,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
    {
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _guidGenerator = guidGenerator ?? throw new ArgumentNullException(nameof(guidGenerator));
        _tenantIdAccessor = tenantIdAccessor ?? throw new ArgumentNullException(nameof(tenantIdAccessor));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _subscriptionManager = subscriptionManager ?? throw new ArgumentNullException(nameof(subscriptionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        // Default-datastore threading only: real per-event grouping is a later task. Guard against
        // a missing/empty configured name so store calls always receive a usable data store name.
        var defaultName = defaultDataStoreOptions?.Value?.DefaultDataStoreName;
        if (string.IsNullOrWhiteSpace(defaultName))
        {
            throw new InvalidOperationException(
                "No datastore name was supplied for the outbox router and no default datastore is " +
                "configured. Call SetDefaultDataStore(\"<name>\") during persistence configuration, or " +
                "pass a dataStoreName to AddOutbox(...) so the outbox knows which datastore to write to.");
        }
        _dataStoreName = defaultName;
    }

    /// <inheritdoc />
    /// <remarks>Back-compat overload: buffers with a null datastore sentinel (resolves to the default at persist time).</remarks>
    public void AddTransactionalEvent(ISerializableEvent serializableEvent)
        => AddTransactionalEvent(serializableEvent, null);

    /// <inheritdoc />
    /// <remarks>Back-compat overload: buffers with a null datastore sentinel (resolves to the default at persist time).</remarks>
    public void AddTransactionalEvents(IEnumerable<ISerializableEvent> serializableEvents)
        => AddTransactionalEvents(serializableEvents, null);

    /// <summary>
    /// Buffers a single event associated with a specific datastore name. A null <paramref name="dataStoreName"/>
    /// is a "use-default, resolve-downstream" sentinel resolved to the configured default at persist time.
    /// </summary>
    public void AddTransactionalEvent(ISerializableEvent serializableEvent, string? dataStoreName)
    {
        Guard.IsNotNull(serializableEvent, nameof(serializableEvent));
        _buffer.Enqueue((serializableEvent, dataStoreName));
    }

    /// <summary>
    /// Buffers a set of events all associated with a specific datastore name. A null <paramref name="dataStoreName"/>
    /// is a "use-default, resolve-downstream" sentinel resolved to the configured default at persist time.
    /// </summary>
    public void AddTransactionalEvents(IEnumerable<ISerializableEvent> serializableEvents, string? dataStoreName)
    {
        Guard.IsNotNull(serializableEvents, nameof(serializableEvents));
        foreach (var e in serializableEvents)
        {
            AddTransactionalEvent(e, dataStoreName);
        }
    }

    /// <summary>
    /// Drains the in-memory buffer and writes each event as an <see cref="OutboxMessage"/> to the
    /// <see cref="IOutboxStore"/>. This must be called within the active database transaction (UnitOfWork Phase 1).
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation requests.</param>
    public async Task PersistBufferedEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = new List<(ISerializableEvent Event, string? DataStoreName)>();
        while (_buffer.TryDequeue(out var e))
        {
            events.Add(e);
        }

        // Resolve each event's effective datastore (null sentinel -> configured default) and persist the
        // row to THAT datastore, so each event lands in its own entity's store (AC-8). We also record the
        // set of producers this event targets at write time (AC-10) so the poller can route it later.
        var producers = GetRegisteredProducers();

        // Group by effective datastore name to make per-store persistence explicit.
        var grouped = events
            .Select(pair => (pair.Event, DataStoreName: ResolveDataStoreName(pair.DataStoreName)))
            .GroupBy(x => x.DataStoreName);

        foreach (var group in grouped)
        {
            var effectiveName = group.Key;
            foreach (var (@event, _) in group)
            {
                var targetProducers = ResolveTargetProducers(producers, @event);

                var message = new OutboxMessage
                {
                    Id = _guidGenerator.Create(),
                    EventType = _serializer.GetEventTypeName(@event),
                    EventPayload = _serializer.Serialize(@event),
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    TenantId = _tenantIdAccessor.GetTenantId(),
                    // Comma-delimited list of matched producer FullName(s); Task 9's poller splits on this.
                    TargetProducers = targetProducers.Count > 0
                        ? string.Join(TargetProducersSeparator, targetProducers)
                        : null
                    // Note: CorrelationId population is left for a future enhancement (V2)
                };

                _logger.LogDebug(
                    "Persisting outbox message {Id} for event {EventType} to datastore {DataStore}",
                    message.Id, message.EventType, effectiveName);
                await _outboxStore.SaveAsync(message, effectiveName, cancellationToken).ConfigureAwait(false);
                _persistedEvents.Add((message.Id, @event, effectiveName));
            }
        }
    }

    /// <summary>
    /// Resolves an effective datastore name from a buffered (possibly null) name. A null/empty value is the
    /// "use-default" sentinel and resolves to the configured <see cref="DefaultDataStoreOptions.DefaultDataStoreName"/>.
    /// </summary>
    private string ResolveDataStoreName(string? dataStoreName)
        => string.IsNullOrWhiteSpace(dataStoreName) ? _dataStoreName : dataStoreName!;

    /// <summary>
    /// Resolves the FullName of every <see cref="IEventProducer"/> that should handle the given event, using the
    /// same subscription filtering as <see cref="RouteEventsAsync()"/> (DRY between persist-time target recording
    /// and dispatch-time routing).
    /// </summary>
    private List<string> ResolveTargetProducers(IEnumerable<IEventProducer> producers, ISerializableEvent @event)
        => FilterProducers(producers, @event)
            .Select(p => p.GetType().FullName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();

    /// <summary>
    /// Filters the registered producers down to those subscribed to the event's type. When no subscriptions are
    /// registered, all producers are returned (backward-compatible fallback). Shared by target recording and dispatch.
    /// </summary>
    private List<IEventProducer> FilterProducers(IEnumerable<IEventProducer> producers, ISerializableEvent @event)
        => (_subscriptionManager.HasSubscriptions
                ? _subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                : producers).ToList();

    /// <summary>
    /// Resolves the registered <see cref="IEventProducer"/> instances, tolerating an unregistered
    /// <c>IEnumerable&lt;IEventProducer&gt;</c> by returning an empty sequence (matching real DI semantics,
    /// where <c>GetServices</c> yields empty rather than throwing). Persist-time target recording now runs
    /// even when no producers are registered, so it must not hard-fail in that case.
    /// </summary>
    private IEnumerable<IEventProducer> GetRegisteredProducers()
        => _serviceProvider.GetService(typeof(IEnumerable<IEventProducer>)) as IEnumerable<IEventProducer>
            ?? Enumerable.Empty<IEventProducer>();

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

        // Primary cross-host fix: on a producer-only host (ImmediateDispatch = false), skip Phase 3
        // entirely — no in-process dispatch and, crucially, no MarkProcessedAsync. The rows were already
        // persisted in Phase 1; the durable poller on the processor host is the sole dispatcher/marker.
        // Marking here would hide the rows from the poller (ClaimAsync filters ProcessedAtUtc IS NULL)
        // and silently defeat cross-host delivery.
        if (!_options.ImmediateDispatch)
        {
            _logger.LogDebug(
                "ImmediateDispatch is disabled; leaving {Count} persisted outbox message(s) for the poller",
                _persistedEvents.Count);
            _persistedEvents.Clear();
            return;
        }

        _logger.LogInformation("OutboxEventRouter dispatching {Count} retained messages", _persistedEvents.Count);

        var producers = GetRegisteredProducers();

        foreach (var (messageId, @event, dataStoreName) in _persistedEvents)
        {
            try
            {
                var filteredProducers = FilterProducers(producers, @event);

                // Secondary hardening (defense-in-depth): if nothing was dispatched in-process, do not
                // mark the row processed — leave it for the poller. This is NOT the cross-host fix (an
                // in-process producer that no-ops when no subscriber matches still counts as "dispatched"
                // here); it only guards the case where no matching producer exists on this host at all.
                if (filteredProducers.Count == 0)
                {
                    _logger.LogDebug(
                        "No in-process producer matched event {EventType} for message {Id}; leaving it for the poller",
                        @event.GetType().Name, messageId);
                    continue;
                }

                foreach (var producer in filteredProducers)
                {
                    await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
                }

                await _outboxStore.MarkProcessedAsync(messageId, dataStoreName, cancellationToken).ConfigureAwait(false);
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

        var producers = GetRegisteredProducers();

        foreach (var @event in transactionalEvents)
        {
            foreach (var producer in FilterProducers(producers, @event))
            {
                await producer.ProduceEventAsync(@event, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
