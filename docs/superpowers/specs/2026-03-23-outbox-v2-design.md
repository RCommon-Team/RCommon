# Outbox V2 Design Spec

> **Scope:** Exponential backoff, distributed locking, dead letter replay, inbox/idempotency for the RCommon transactional outbox.

**Date:** 2026-03-23
**Branch:** feature/ddd
**Backward Compatibility:** Breaking — `IOutboxStore`, `IOutboxMessage`, and `OutboxMessage` are modified directly.

---

## 1. Overview

V1 of the transactional outbox provides reliable event persistence and dispatch via `OutboxProcessingService`. V2 adds four capabilities:

1. **Exponential backoff** — failed messages wait progressively longer before retry
2. **Distributed locking** — multiple processor instances can run safely without double-dispatch
3. **Dead letter replay** — dead-lettered messages can be inspected and replayed
4. **Inbox/idempotency** — consumer-side deduplication via a separate inbox table

All four features build on the existing architecture. The breaking changes are confined to `IOutboxStore`, `IOutboxMessage`, and `OutboxMessage`.

---

## 2. Interface Changes

### 2.1 IOutboxMessage — 3 new properties

```csharp
public interface IOutboxMessage
{
    // Existing (unchanged)
    Guid Id { get; }
    string EventType { get; }
    string EventPayload { get; }
    DateTimeOffset CreatedAtUtc { get; }
    DateTimeOffset? ProcessedAtUtc { get; set; }
    DateTimeOffset? DeadLetteredAtUtc { get; set; }
    string? ErrorMessage { get; set; }
    int RetryCount { get; set; }
    string? CorrelationId { get; set; }
    string? TenantId { get; set; }

    // V2 additions
    DateTimeOffset? NextRetryAtUtc { get; set; }
    string? LockedByInstanceId { get; set; }
    DateTimeOffset? LockedUntilUtc { get; set; }
}
```

- `NextRetryAtUtc` — when this message becomes eligible for retry (null = immediately eligible)
- `LockedByInstanceId` — which processor instance claimed this message
- `LockedUntilUtc` — lock expiry; stale locks auto-release when this time passes

### 2.2 OutboxMessage — matching properties

`OutboxMessage` gains the same three properties with public getters/setters to match `IOutboxMessage`.

### 2.3 IOutboxStore — revised interface

```csharp
public interface IOutboxStore
{
    // Unchanged
    Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
    Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);

    // Changed signature — now takes nextRetryAtUtc
    Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, CancellationToken cancellationToken = default);

    // New — atomic claim replaces GetPendingAsync
    Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, CancellationToken cancellationToken = default);

    // New — dead letter management
    Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset = 0, CancellationToken cancellationToken = default);
    Task ReplayDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default);
}
```

**Removed:** `GetPendingAsync` — replaced entirely by `ClaimAsync`.

### 2.4 IBackoffStrategy — new abstraction

```csharp
public interface IBackoffStrategy
{
    TimeSpan ComputeDelay(int retryCount);
}
```

Default implementation:

```csharp
public class ExponentialBackoffStrategy : IBackoffStrategy
{
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _multiplier;

    public ExponentialBackoffStrategy(TimeSpan baseDelay, TimeSpan maxDelay, double multiplier = 2.0)
    {
        _baseDelay = baseDelay;
        _maxDelay = maxDelay;
        _multiplier = multiplier;
    }

    public TimeSpan ComputeDelay(int retryCount)
        => TimeSpan.FromSeconds(
            Math.Min(
                _baseDelay.TotalSeconds * Math.Pow(_multiplier, retryCount),
                _maxDelay.TotalSeconds));
}
```

### 2.5 OutboxOptions — new properties

```csharp
public class OutboxOptions
{
    // Existing (unchanged)
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 5;
    public TimeSpan CleanupAge { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    public string TableName { get; set; } = "__OutboxMessages";

    // V2 additions
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan BackoffBaseDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan BackoffMaxDelay { get; set; } = TimeSpan.FromMinutes(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public string InboxTableName { get; set; } = "__InboxMessages";
}
```

---

## 3. Distributed Locking

### 3.1 Provider Detection

Each ORM handles provider detection differently:

- **EF Core:** Auto-detects from `DbContext.Database.ProviderName` (`Microsoft.EntityFrameworkCore.SqlServer` or `Npgsql.EntityFrameworkCore.PostgreSQL`). No injected provider needed.
- **Dapper / Linq2Db:** Uses injected `ILockStatementProvider` to select SQL dialect.

```csharp
public interface ILockStatementProvider
{
    string ProviderName { get; }  // "SqlServer", "PostgreSql"
}

public class SqlServerLockStatementProvider : ILockStatementProvider
{
    public string ProviderName => "SqlServer";
}

public class PostgreSqlLockStatementProvider : ILockStatementProvider
{
    public string ProviderName => "PostgreSql";
}
```

Unsupported providers throw `NotSupportedException` with a clear message.

### 3.2 ClaimAsync SQL

**SQL Server:**

```sql
WITH batch AS (
    SELECT TOP(@batchSize) Id
    FROM __OutboxMessages WITH (UPDLOCK, ROWLOCK, READPAST)
    WHERE ProcessedAtUtc IS NULL
      AND DeadLetteredAtUtc IS NULL
      AND RetryCount < @maxRetries
      AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @now)
      AND (LockedUntilUtc IS NULL OR LockedUntilUtc <= @now)
    ORDER BY CreatedAtUtc
)
UPDATE o
SET o.LockedByInstanceId = @instanceId, o.LockedUntilUtc = @lockUntil
OUTPUT INSERTED.*
FROM __OutboxMessages o
INNER JOIN batch ON o.Id = batch.Id;
```

`UPDLOCK, ROWLOCK, READPAST` ensures concurrent instances skip rows already being claimed by another instance, preventing deadlocks and double-dispatch.

**PostgreSQL:**

```sql
UPDATE "__OutboxMessages" o
SET "LockedByInstanceId" = @instanceId, "LockedUntilUtc" = @lockUntil
FROM (
    SELECT "Id" FROM "__OutboxMessages"
    WHERE "ProcessedAtUtc" IS NULL
      AND "DeadLetteredAtUtc" IS NULL
      AND "RetryCount" < @maxRetries
      AND ("NextRetryAtUtc" IS NULL OR "NextRetryAtUtc" <= @now)
      AND ("LockedUntilUtc" IS NULL OR "LockedUntilUtc" <= @now)
    ORDER BY "CreatedAtUtc"
    LIMIT @batchSize
    FOR UPDATE SKIP LOCKED
) AS batch
WHERE o."Id" = batch."Id"
RETURNING o.*;
```

Both queries atomically:
1. Filter to eligible messages (not processed, not dead-lettered, under max retries, past retry delay, not locked or lock expired)
2. Claim by setting `LockedByInstanceId` and `LockedUntilUtc`
3. Return claimed messages in a single round-trip

### 3.3 Future Provider Extensibility

MySQL (`UPDATE ... ORDER BY ... LIMIT` with separate SELECT) and Oracle (`FOR UPDATE SKIP LOCKED`) follow the same pattern — add a new `ILockStatementProvider` implementation and SQL dialect. No interface changes required.

### 3.4 Index

Updated composite index for ClaimAsync performance:

```
IX_OutboxMessages_Pending: (ProcessedAtUtc, DeadLetteredAtUtc, NextRetryAtUtc, LockedUntilUtc, CreatedAtUtc)
```

Replaces the V1 index on `(ProcessedAtUtc, DeadLetteredAtUtc, CreatedAtUtc)`.

---

## 4. OutboxProcessingService Changes

### 4.1 Instance Identity

```csharp
public class OutboxProcessingService : BackgroundService
{
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private readonly IBackoffStrategy _backoffStrategy;
    // ... existing fields unchanged
}
```

### 4.2 ProcessBatchAsync — revised flow

> **Note:** The pseudocode below omits `.ConfigureAwait(false)` for readability. The real implementation must use `.ConfigureAwait(false)` on all `await` calls, consistent with V1.

```csharp
public async Task ProcessBatchAsync(CancellationToken cancellationToken)
{
    using var scope = _serviceProvider.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
    var serializer = scope.ServiceProvider.GetRequiredService<IOutboxSerializer>();
    var producers = scope.ServiceProvider.GetServices<IEventProducer>();
    var subscriptionManager = scope.ServiceProvider.GetRequiredService<EventSubscriptionManager>();
    var inboxStore = scope.ServiceProvider.GetService<IInboxStore>(); // Optional

    // Atomic claim replaces GetPendingAsync
    var claimed = await store.ClaimAsync(
        _instanceId, _options.BatchSize, _options.LockDuration, cancellationToken);

    foreach (var message in claimed)
    {
        try
        {
            // Auto-check inbox (if registered)
            if (inboxStore != null)
            {
                if (await inboxStore.ExistsAsync(message.Id, "OutboxProcessingService", cancellationToken))
                {
                    await store.MarkProcessedAsync(message.Id, cancellationToken);
                    continue;
                }
            }

            var @event = serializer.Deserialize(message.EventType, message.EventPayload);
            var filteredProducers = subscriptionManager.HasSubscriptions
                ? subscriptionManager.GetProducersForEvent(producers, @event.GetType())
                : producers;

            foreach (var producer in filteredProducers)
            {
                await producer.ProduceEventAsync((dynamic)@event, cancellationToken);
            }

            // Record in inbox before marking processed (if registered)
            if (inboxStore != null)
            {
                await inboxStore.RecordAsync(new InboxMessage
                {
                    MessageId = message.Id,
                    ConsumerType = "OutboxProcessingService",
                    EventType = message.EventType,
                    ReceivedAtUtc = DateTimeOffset.UtcNow
                }, cancellationToken);
            }

            await store.MarkProcessedAsync(message.Id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to dispatch outbox message {Id} (retry {Retry})",
                message.Id, message.RetryCount);

            if (message.RetryCount + 1 >= _options.MaxRetries)
            {
                await store.MarkDeadLetteredAsync(message.Id, cancellationToken);
            }
            else
            {
                var delay = _backoffStrategy.ComputeDelay(message.RetryCount + 1);
                var nextRetryAt = DateTimeOffset.UtcNow + delay;
                await store.MarkFailedAsync(message.Id, ex.Message, nextRetryAt, cancellationToken);
            }
        }
    }

    // Periodic cleanup (throttled by CleanupInterval)
    if (DateTimeOffset.UtcNow - _lastCleanupUtc >= _options.CleanupInterval)
    {
        await store.DeleteProcessedAsync(_options.CleanupAge, cancellationToken);
        await store.DeleteDeadLetteredAsync(_options.CleanupAge, cancellationToken);
        if (inboxStore != null)
        {
            await inboxStore.CleanupAsync(_options.CleanupAge, cancellationToken);
        }
        _lastCleanupUtc = DateTimeOffset.UtcNow;
    }
}
```

### 4.3 DI Registration Changes

In `AddOutbox<TOutboxStore>()`:

```csharp
// Backoff strategy (singleton, replaceable)
builder.Services.TryAddSingleton<IBackoffStrategy>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<OutboxOptions>>().Value;
    return new ExponentialBackoffStrategy(opts.BackoffBaseDelay, opts.BackoffMaxDelay, opts.BackoffMultiplier);
});
```

Users can register a custom `IBackoffStrategy` before calling `AddOutbox` to override.

### 4.4 OutboxEventRouter Changes

`OutboxEventRouter.RouteEventsAsync()` currently calls `GetPendingAsync` and `MarkFailedAsync` with V1 signatures. V2 changes its behavior:

**Before (V1):** `RouteEventsAsync()` reads all pending messages from the store, dispatches, marks processed/failed.

**After (V2):** `PersistBufferedEventsAsync` retains the persisted message IDs and deserialized events in a private list. `RouteEventsAsync()` dispatches only those just-persisted events (no store read). On success → `MarkProcessedAsync`. On failure → log warning and skip (the background processor picks it up on the next `ClaimAsync` poll with backoff).

**Implementation sketch:**

```csharp
// New private field
private readonly List<(Guid MessageId, ISerializableEvent Event)> _persistedEvents = new();

// In PersistBufferedEventsAsync, after SaveAsync:
_persistedEvents.Add((message.Id, @event));

// RouteEventsAsync() revised:
public async Task RouteEventsAsync(CancellationToken cancellationToken = default)
{
    if (_persistedEvents.Count == 0) return;

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
}
```

This means `RouteEventsAsync()`:
- **No longer calls** `GetPendingAsync` (removed from interface)
- **No longer calls** `MarkFailedAsync` (failures left for background processor)
- Only calls `MarkProcessedAsync` on success
- Becomes a best-effort immediate dispatch of the current scope's events only

The `RouteEventsAsync(IEnumerable<ISerializableEvent>, CancellationToken)` overload (direct dispatch without store) is unchanged.

---

## 5. Inbox / Idempotency

### 5.1 IInboxMessage

```csharp
public interface IInboxMessage
{
    Guid MessageId { get; }
    string EventType { get; }
    string? ConsumerType { get; }
    DateTimeOffset ReceivedAtUtc { get; }
}

public class InboxMessage : IInboxMessage
{
    public Guid MessageId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? ConsumerType { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}
```

### 5.2 IInboxStore

```csharp
public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid messageId, string? consumerType = null, CancellationToken cancellationToken = default);
    Task RecordAsync(IInboxMessage message, CancellationToken cancellationToken = default);
    Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
```

- `ExistsAsync` — returns true if the message was already processed by this consumer
- `RecordAsync` — records processing; throws on duplicate (unique constraint)
- `CleanupAsync` — deletes entries older than the specified age

### 5.3 Table Schema

```
__InboxMessages
├── MessageId       (Guid)
├── EventType       (string)
├── ConsumerType    (string, NOT NULL at DB level — C# property is `string?`, stored as "" when null)
├── ReceivedAtUtc   (DateTimeOffset)
├── PK: (MessageId, ConsumerType)
└── IX_InboxMessages_Cleanup: (ReceivedAtUtc)
```

Composite PK on `(MessageId, ConsumerType)` allows the same message to be processed by multiple different consumers while preventing duplicate processing by the same consumer.

### 5.4 Mode 1: Standalone Opt-In

Consumers check the inbox explicitly. The `MessageId` is a domain-specific deduplication key chosen by the consumer — typically a domain event ID, correlation ID, or any stable identifier the consumer can derive from the event. `ISerializableEvent` does not define an `Id` property; the concrete event type is responsible for carrying an appropriate identifier.

```csharp
// Assumes OrderCreatedEvent has an OrderId property suitable for deduplication
public class OrderCreatedHandler : IAppEventHandler<OrderCreatedEvent>
{
    private readonly IInboxStore _inbox;

    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken ct)
    {
        if (await _inbox.ExistsAsync(@event.OrderId, GetType().FullName, ct))
            return;

        // ... handle event ...

        await _inbox.RecordAsync(new InboxMessage
        {
            MessageId = @event.OrderId,
            ConsumerType = GetType().FullName,
            EventType = @event.GetType().FullName!,
            ReceivedAtUtc = DateTimeOffset.UtcNow
        }, ct);
    }
}
```

> **Mode 2 (integrated auto-check)** uses `OutboxMessage.Id` as the `MessageId`, which is always available. Mode 1 requires the consumer to choose an appropriate deduplication key from the concrete event type.

### 5.5 Mode 2: Integrated Auto-Check

When `IInboxStore` is registered, `OutboxProcessingService` automatically wraps each dispatch with an idempotency check (see Section 4.2). No consumer code changes needed. Resolved via `GetService<IInboxStore>()` — silently skipped if not registered.

### 5.6 DI Registration

```csharp
public static IPersistenceBuilder AddInbox<TInboxStore>(
    this IPersistenceBuilder builder)
    where TInboxStore : class, IInboxStore
{
    builder.Services.AddScoped<IInboxStore, TInboxStore>();
    return builder;
}
```

Separate from `AddOutbox` — can be used independently or together. Inbox cleanup piggybacks on `OutboxProcessingService`'s existing cleanup cycle when `IInboxStore` is registered.

---

## 6. Dead Letter Replay

### 6.1 GetDeadLettersAsync

Query: `WHERE DeadLetteredAtUtc IS NOT NULL`, ordered by `DeadLetteredAtUtc DESC` (most recent first), with `batchSize` and `offset` for paging. Returns full message details including `ErrorMessage` for diagnostics.

### 6.2 ReplayDeadLetterAsync

Resets a dead-lettered message to pending state:

```
DeadLetteredAtUtc = null
ProcessedAtUtc = null
ErrorMessage = null
RetryCount = 0
NextRetryAtUtc = null
LockedByInstanceId = null
LockedUntilUtc = null
```

After replay, the message re-enters the normal `ClaimAsync` pipeline with a full retry budget.

Throws `InvalidOperationException` if the message doesn't exist or isn't currently dead-lettered.

### 6.3 No Bulk Replay

Single-message replay only. Bulk replay is a future enhancement. Callers loop if they need bulk behavior.

### 6.4 Index

```
IX_OutboxMessages_DeadLettered: (DeadLetteredAtUtc DESC) WHERE DeadLetteredAtUtc IS NOT NULL
```

Filtered index for efficient dead letter queries.

---

## 7. Store Implementations

Each ORM project implements both `IOutboxStore` (updated) and `IInboxStore` (new):

| Project | Outbox Store | Inbox Store |
|---------|-------------|-------------|
| RCommon.EfCore | `EFCoreOutboxStore` (updated) | `EFCoreInboxStore` (new) |
| RCommon.Dapper | `DapperOutboxStore` (updated) | `DapperInboxStore` (new) |
| RCommon.Linq2Db | `Linq2DbOutboxStore` (updated) | `Linq2DbInboxStore` (new) |

### 7.1 EF Core

- `ClaimAsync`: Raw SQL via `Database.SqlQueryRaw<OutboxMessage>()`, dialect selected by `Database.ProviderName`
- `EFCoreInboxStore`: Standard EF Core CRUD on `DbSet<InboxMessage>`
- `InboxMessageConfiguration`: Entity configuration with composite PK and cleanup index
- `ModelBuilderExtensions`: Updated to include inbox entity configuration

### 7.2 Dapper

- `ClaimAsync`: Raw SQL selected by `ILockStatementProvider.ProviderName`
- `DapperInboxStore`: Standard Dapper queries (`INSERT`, `SELECT EXISTS`, `DELETE`)

### 7.3 Linq2Db

- `ClaimAsync`: Raw SQL selected by `ILockStatementProvider.ProviderName`
- `Linq2DbInboxStore`: Linq2Db LINQ API for CRUD, raw SQL for claim

---

## 8. Files Changed

### New files (RCommon.Persistence)
- `Outbox/IBackoffStrategy.cs`
- `Outbox/ExponentialBackoffStrategy.cs`
- `Outbox/ILockStatementProvider.cs`
- `Outbox/SqlServerLockStatementProvider.cs`
- `Outbox/PostgreSqlLockStatementProvider.cs`
- `Inbox/IInboxMessage.cs`
- `Inbox/InboxMessage.cs`
- `Inbox/IInboxStore.cs`
- `Inbox/InboxPersistenceBuilderExtensions.cs`

### Modified files (RCommon.Persistence)
- `Outbox/IOutboxMessage.cs` — 3 new properties
- `Outbox/OutboxMessage.cs` — 3 new properties
- `Outbox/IOutboxStore.cs` — remove `GetPendingAsync`, change `MarkFailedAsync`, add `ClaimAsync`, `GetDeadLettersAsync`, `ReplayDeadLetterAsync`
- `Outbox/OutboxOptions.cs` — 5 new properties
- `Outbox/OutboxProcessingService.cs` — instance ID, claim-based polling, backoff, inbox auto-check
- `Outbox/OutboxPersistenceBuilderExtensions.cs` — register `IBackoffStrategy`
- `Outbox/OutboxEventRouter.cs` — remove `GetPendingAsync`/`MarkFailedAsync` calls, retain persisted events for immediate dispatch

### Modified files (ORM projects)
- `RCommon.EfCore/Outbox/EFCoreOutboxStore.cs` — implement `ClaimAsync`, `GetDeadLettersAsync`, `ReplayDeadLetterAsync`, update `MarkFailedAsync`
- `RCommon.EfCore/Outbox/OutboxMessageConfiguration.cs` — new columns, updated index
- `RCommon.EfCore/Outbox/ModelBuilderExtensions.cs` — add inbox configuration
- `RCommon.Dapper/Outbox/DapperOutboxStore.cs` — same updates
- `RCommon.Dapper/DapperPersistenceBuilder.cs` — `ILockStatementProvider` registration support
- `RCommon.Linq2Db/Outbox/Linq2DbOutboxStore.cs` — same updates
- `RCommon.Linq2Db/Linq2DbPersistenceBuilder.cs` — `ILockStatementProvider` registration support

### New files (ORM projects)
- `RCommon.EfCore/Inbox/EFCoreInboxStore.cs`
- `RCommon.EfCore/Inbox/InboxMessageConfiguration.cs`
- `RCommon.Dapper/Inbox/DapperInboxStore.cs`
- `RCommon.Linq2Db/Inbox/Linq2DbInboxStore.cs`

### Test files (new and modified)
- Updated: `Tests/RCommon.EfCore.Tests/EFCoreOutboxStoreTests.cs`
- Updated: `Tests/RCommon.Dapper.Tests/DapperOutboxStoreTests.cs`
- Updated: `Tests/RCommon.Linq2Db.Tests/Linq2DbOutboxStoreTests.cs`
- New: `Tests/RCommon.EfCore.Tests/EFCoreInboxStoreTests.cs`
- New: `Tests/RCommon.Dapper.Tests/DapperInboxStoreTests.cs`
- New: `Tests/RCommon.Linq2Db.Tests/Linq2DbInboxStoreTests.cs`
- New: `Tests/RCommon.Persistence.Tests/ExponentialBackoffStrategyTests.cs`
- Updated: `Tests/RCommon.Persistence.Tests/OutboxProcessingServiceTests.cs`
- Updated: `Tests/RCommon.Persistence.Tests/OutboxEventRouterTests.cs` — remove `GetPendingAsync`/`MarkFailedAsync` mocks, test retained-event dispatch
- Updated: `Tests/RCommon.Persistence.Tests/OutboxEntityEventTrackerTests.cs` — update mocks for changed `RouteEventsAsync` behavior
- Updated: `Tests/RCommon.Persistence.Tests/OutboxConcurrencyTests.cs` — update mocks for removed `GetPendingAsync`

---

## 9. Design Decisions

| Decision | Rationale |
|----------|-----------|
| Break interfaces directly | User decision — no backward compat shims |
| Remove `GetPendingAsync` entirely | `ClaimAsync` is a strict superset; keeping both creates confusion about which to use |
| EF Core auto-detects provider | `Database.ProviderName` is always available; no extra DI registration needed |
| Dapper/Linq2Db use `ILockStatementProvider` | No DbContext to introspect; explicit provider selection is clearer |
| `IBackoffStrategy` as singleton | Stateless computation — one instance serves all scoped stores |
| Inbox `RecordAsync` throws on duplicate | Relies on DB unique constraint; simpler than `TryRecord` + boolean return |
| Inbox composite PK `(MessageId, ConsumerType)` | Same message, different consumers = OK. Same message, same consumer = blocked |
| Inbox cleanup in outbox service | Piggybacks on existing cleanup cycle; avoids a second background service |
| Single-message dead letter replay | Prevents accidental bulk replay; callers can loop if needed |
| `GetService` for `IInboxStore` in processing service | Fully opt-in — inbox silently disabled if not registered |
