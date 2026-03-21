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

namespace RCommon.Persistence.Outbox;

public class OutboxProcessingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessingService> _logger;

    public OutboxProcessingService(
        IServiceProvider serviceProvider,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessingService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

        var pending = await store.GetPendingAsync(_options.BatchSize, cancellationToken).ConfigureAwait(false);

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

                var @event = serializer.Deserialize(message.EventType, message.EventPayload);
                var filteredProducers = subscriptionManager.HasSubscriptions
                    ? subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                    : producers;

                foreach (var producer in filteredProducers)
                {
                    // Use dynamic dispatch so ProduceEventAsync<T> is invoked with the concrete
                    // runtime type of the event rather than the ISerializableEvent interface type.
                    await producer.ProduceEventAsync((dynamic)@event, cancellationToken).ConfigureAwait(false);
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
                    await store.MarkFailedAsync(message.Id, ex.Message, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // Periodic cleanup
        await store.DeleteProcessedAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
        await store.DeleteDeadLetteredAsync(_options.CleanupAge, cancellationToken).ConfigureAwait(false);
    }
}
