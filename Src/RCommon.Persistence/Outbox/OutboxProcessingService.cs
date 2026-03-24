using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private DateTimeOffset _lastCleanupUtc = DateTimeOffset.MinValue;

    public OutboxProcessingService(
        IServiceProvider serviceProvider,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessingService> logger,
        IBackoffStrategy backoffStrategy)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backoffStrategy = backoffStrategy ?? throw new ArgumentNullException(nameof(backoffStrategy));
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
        var producers = scope.ServiceProvider.GetServices<IEventProducer>();
        var subscriptionManager = scope.ServiceProvider.GetRequiredService<EventSubscriptionManager>();
        var inboxStore = scope.ServiceProvider.GetService<IInboxStore>();

        var pending = await store.ClaimAsync(_instanceId, _options.BatchSize, _options.LockDuration, cancellationToken).ConfigureAwait(false);

        foreach (var message in pending)
        {
            try
            {
                if (message.RetryCount >= _options.MaxRetries)
                {
                    _logger.LogWarning("Outbox message {Id} exceeded max retries ({Max}). Dead-lettering.",
                        message.Id, _options.MaxRetries);
                    await store.MarkDeadLetteredAsync(message.Id, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                // Inbox auto-check: skip if already processed
                if (inboxStore != null && await inboxStore.ExistsAsync(message.Id, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogDebug("Outbox message {Id} already in inbox, marking processed", message.Id);
                    await store.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var @event = serializer.Deserialize(message.EventType, message.EventPayload);
                var filteredProducers = subscriptionManager.HasSubscriptions
                    ? subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                    : producers;

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

                await store.MarkProcessedAsync(message.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to dispatch outbox message {Id} (retry {Retry})",
                    message.Id, message.RetryCount);

                if (message.RetryCount + 1 >= _options.MaxRetries)
                {
                    await store.MarkDeadLetteredAsync(message.Id, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var delay = _backoffStrategy.ComputeDelay(message.RetryCount + 1);
                    await store.MarkFailedAsync(message.Id, ex.Message, DateTimeOffset.UtcNow + delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Periodic cleanup (throttled by CleanupInterval)
        if (DateTimeOffset.UtcNow - _lastCleanupUtc >= _options.CleanupInterval)
        {
            await store.DeleteProcessedAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
            await store.DeleteDeadLetteredAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
            if (inboxStore != null)
            {
                await inboxStore.CleanupAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
            }
            _lastCleanupUtc = DateTimeOffset.UtcNow;
        }
    }
}
