using System;

namespace RCommon.Persistence.Outbox;

public class OutboxOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 5;
    public TimeSpan CleanupAge { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    public string TableName { get; set; } = "__OutboxMessages";
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan BackoffBaseDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan BackoffMaxDelay { get; set; } = TimeSpan.FromMinutes(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public string InboxTableName { get; set; } = "__InboxMessages";

    /// <summary>
    /// The name of the datastore that owns this outbox table. <c>null</c> means "use the configured
    /// default datastore". Set via <see cref="OnDataStore"/>.
    /// </summary>
    public string? DataStoreName { get; private set; }

    /// <summary>
    /// Names the datastore that owns this outbox table (AC-21). Fluent, returns this instance.
    /// </summary>
    /// <param name="dataStoreName">The owning datastore name. Must not be null, empty, or whitespace.</param>
    public OutboxOptions OnDataStore(string dataStoreName)
    {
        if (string.IsNullOrWhiteSpace(dataStoreName))
            throw new ArgumentException("Data store name must not be null, empty, or whitespace.", nameof(dataStoreName));

        DataStoreName = dataStoreName;
        return this;
    }

    /// <summary>
    /// Controls whether the unit of work attempts best-effort in-process dispatch of outbox events
    /// immediately after commit (Phase 3), in addition to persisting them for the background poller.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c> (the default, preserving single-host behaviour), committing a unit of work
    /// persists events to the outbox and then immediately dispatches them in-process, marking each
    /// row processed on success. This gives low-latency delivery when the producer and every
    /// subscriber run in the same process.
    /// </para>
    /// <para>
    /// When <c>false</c>, the unit of work persists events to the outbox but skips Phase 3 entirely —
    /// no in-process dispatch and no marking rows processed. The durable poller
    /// (<see cref="OutboxProcessingService"/>) becomes the sole dispatcher and marker.
    /// </para>
    /// <para>
    /// <b>Set this to <c>false</c> on any host that produces outbox events but does not itself run the
    /// poller (a "producer-only" host in a producer/processor topology).</b> If a subscriber for an
    /// event type runs on a different process than the producer, an immediate producer-side dispatch
    /// would mark the row processed before that remote subscriber ever sees it, and the poller —
    /// which only claims rows where <c>ProcessedAtUtc IS NULL</c> — would never deliver it. Leaving
    /// this at the default on a producer-only host silently defeats cross-host delivery.
    /// </para>
    /// </remarks>
    public bool ImmediateDispatch { get; set; } = true;
}

