using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using RCommon.Persistence.Inbox;

namespace RCommon.Persistence.Outbox;

public class OutboxProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessingService> _logger;
    private readonly IBackoffStrategy _backoffStrategy;
    private readonly string _dataStoreName;
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private DateTimeOffset _lastCleanupUtc = DateTimeOffset.MinValue;
    private readonly HashSet<string> _warnedEventTypesWithoutSubscriber = new();

    public OutboxProcessingService(
        IServiceProvider serviceProvider,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessingService> logger,
        IBackoffStrategy backoffStrategy,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backoffStrategy = backoffStrategy ?? throw new ArgumentNullException(nameof(backoffStrategy));

        // Default-datastore threading only: real multi-datastore polling is a later task. Guard against
        // a missing/empty configured name so store calls always receive a usable data store name.
        var defaultName = defaultDataStoreOptions?.Value?.DefaultDataStoreName;
        if (string.IsNullOrWhiteSpace(defaultName))
        {
            throw new InvalidOperationException(
                "No datastore name was supplied for the outbox processor and no default datastore is " +
                "configured. Call SetDefaultDataStore(\"<name>\") during persistence configuration, or " +
                "pass a dataStoreName to AddOutbox(...) so the poller knows which datastore to drain.");
        }
        _dataStoreName = defaultName;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessingService started. Polling every {Interval}s", _options.PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "OutboxProcessingService encountered an error during polling");
            }

            await Task.Delay(_options.PollingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
        var serializer = scope.ServiceProvider.GetRequiredService<IOutboxSerializer>();
        var producers = scope.ServiceProvider.GetServices<IEventProducer>().ToList();
        var subscriptionManager = scope.ServiceProvider.GetRequiredService<EventSubscriptionManager>();
        var inboxStore = scope.ServiceProvider.GetService<IInboxStore>();
        var registry = scope.ServiceProvider.GetService<IOutboxDataStoreRegistry>();

        // Drain every registered outbox datastore (AC-9). Each row is claimed/marked against its own
        // datastore name. Fall back to the configured default when no registry is present or it is empty
        // (Task 2 registers the default, but this keeps a mis-registered app working).
        var dataStoreNames = registry?.Registrations;
        if (dataStoreNames is null || dataStoreNames.Count == 0)
        {
            dataStoreNames = new[] { _dataStoreName };
        }

        var runCleanup = DateTimeOffset.UtcNow - _lastCleanupUtc >= _options.CleanupInterval;

        foreach (var dataStoreName in dataStoreNames)
        {
            await DrainDataStoreAsync(store, serializer, producers, subscriptionManager, inboxStore, dataStoreName, runCleanup, cancellationToken).ConfigureAwait(false);
        }

        if (runCleanup)
        {
            if (inboxStore != null)
            {
                await inboxStore.CleanupAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
            }
            _lastCleanupUtc = DateTimeOffset.UtcNow;
        }
    }

    private async Task DrainDataStoreAsync(
        IOutboxStore store,
        IOutboxSerializer serializer,
        IReadOnlyList<IEventProducer> producers,
        EventSubscriptionManager subscriptionManager,
        IInboxStore? inboxStore,
        string dataStoreName,
        bool runCleanup,
        CancellationToken cancellationToken)
    {
        var pending = await store.ClaimAsync(_instanceId, _options.BatchSize, _options.LockDuration, dataStoreName, cancellationToken).ConfigureAwait(false);

        foreach (var message in pending)
        {
            try
            {
                if (message.RetryCount >= _options.MaxRetries)
                {
                    _logger.LogWarning("Outbox message {Id} exceeded max retries ({Max}). Dead-lettering.",
                        message.Id, _options.MaxRetries);
                    await store.MarkDeadLetteredAsync(message.Id, dataStoreName, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Inbox auto-check: skip if already processed
                if (inboxStore != null && await inboxStore.ExistsAsync(message.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogDebug("Outbox message {Id} already in inbox, marking processed", message.Id);
                    await store.MarkProcessedAsync(message.Id, dataStoreName, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var @event = serializer.Deserialize(message.EventType, message.EventPayload);
                var filteredProducers = SelectProducers(message, producers, subscriptionManager, @event);

                // The poller is the terminal dispatcher: if no producer/subscriber on this host matches the
                // event, the row is marked processed below and nothing ever handles it. In a producer/processor
                // topology (ImmediateDispatch = false) this usually means a subscriber was registered on the
                // producer host but not on the poller host. Surface it (once per event type) instead of
                // silently dropping the event. (Only warn for the legacy/subscription path; a targeted row
                // with an unmatched producer set is an expected cross-host case, not a misconfiguration.)
                if (filteredProducers.Count == 0
                    && string.IsNullOrWhiteSpace(message.TargetProducers)
                    && _warnedEventTypesWithoutSubscriber.Add(message.EventType))
                {
                    _logger.LogWarning(
                        "Outbox poller drained event type {EventType} but no matching subscriber/producer is " +
                        "registered on this host; the message will be marked processed and not delivered. " +
                        "Ensure the subscriber for this event type is registered on the poller (processor) host.",
                        message.EventType);
                }

                foreach (var producer in filteredProducers)
                {
                    await producer.ProduceEventAsync((dynamic)@event, cancellationToken).ConfigureAwait(false);
                }

                // Record in inbox after successful dispatch
                if (inboxStore != null)
                {
                    await inboxStore.RecordAsync(new InboxMessage
                    {
                        MessageId = message.Id,
                        EventType = message.EventType,
                        ReceivedAtUtc = DateTimeOffset.UtcNow
                    }, cancellationToken).ConfigureAwait(false);
                }

                await store.MarkProcessedAsync(message.Id, dataStoreName, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to dispatch outbox message {Id} (retry {Retry})",
                    message.Id, message.RetryCount);

                if (message.RetryCount + 1 >= _options.MaxRetries)
                {
                    await store.MarkDeadLetteredAsync(message.Id, dataStoreName, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var delay = _backoffStrategy.ComputeDelay(message.RetryCount + 1);
                    await store.MarkFailedAsync(message.Id, ex.Message, DateTimeOffset.UtcNow + delay, dataStoreName, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Periodic cleanup (throttled by CleanupInterval) — run per datastore so each outbox is pruned.
        if (runCleanup)
        {
            await store.DeleteProcessedAsync(_options.CleanupAge, dataStoreName, cancellationToken).ConfigureAwait(false);
            await store.DeleteDeadLetteredAsync(_options.CleanupAge, dataStoreName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Selects the producers to dispatch a claimed message to. When the row records explicit
    /// <see cref="IOutboxMessage.TargetProducers"/> (comma-delimited producer <c>GetType().FullName</c>, written by
    /// the router at persist time), dispatch only to registered producers whose full name is in that set (AC-10).
    /// Otherwise fall back to the legacy resolve-all / subscription-filter path (AC-17 back-compat).
    /// </summary>
    private static List<IEventProducer> SelectProducers(
        IOutboxMessage message,
        IReadOnlyList<IEventProducer> producers,
        EventSubscriptionManager subscriptionManager,
        ISerializableEvent @event)
    {
        if (!string.IsNullOrWhiteSpace(message.TargetProducers))
        {
            var targets = message.TargetProducers
                .Split(',')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

            return producers
                .Where(p => p.GetType().FullName is { } name && targets.Contains(name))
                .ToList();
        }

        return (subscriptionManager.HasSubscriptions
            ? subscriptionManager.GetProducersForEvent(producers, @event.GetType())
            : producers).ToList();
    }
}
