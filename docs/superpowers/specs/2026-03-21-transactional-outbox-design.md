# Transactional Outbox Pattern Design

## Context

The current event dispatch flow in RCommon has a reliability gap: domain events are dispatched **after** the database transaction commits. If the process crashes between commit and dispatch, or if a producer fails, events are lost silently.

The outbox pattern solves this by persisting events to a database table within the same transaction as the domain writes, guaranteeing at-least-once delivery.

## Architecture Overview

### Interception Point

The outbox replaces `InMemoryTransactionalEventRouter` (the `IEventRouter` implementation) with an `OutboxEventRouter` that writes events to an `IOutboxStore` within the active transaction. A background `IHostedService` polls for pending messages and dispatches them.

### Three Integration Tiers

1. **Generic outbox** — RCommon's own outbox with ORM-specific stores (EF Core, Dapper, Linq2Db)
2. **MassTransit native outbox** — Wraps `MassTransit.EntityFrameworkCore`'s built-in transactional outbox
3. **Wolverine native outbox** — Wraps `WolverineFx.EntityFrameworkCore`'s durable messaging

---

## Core Abstractions (`RCommon.Persistence`)

### IOutboxMessage

```csharp
public interface IOutboxMessage
{
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
}
```

### OutboxMessage (concrete entity)

```csharp
public class OutboxMessage : IOutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventPayload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public DateTimeOffset? DeadLetteredAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public string? CorrelationId { get; set; }
    public string? TenantId { get; set; }
}
```

### IOutboxSerializer

Pluggable serialization for converting `ISerializableEvent` to/from JSON. A default `JsonOutboxSerializer` uses `System.Text.Json` and stores the assembly-qualified type name in `EventType`.

```csharp
public interface IOutboxSerializer
{
    string Serialize(ISerializableEvent @event);
    string GetEventTypeName(ISerializableEvent @event);
    ISerializableEvent Deserialize(string eventType, string payload);
}
```

**Default implementation (`JsonOutboxSerializer`):**
- `Serialize` — `JsonSerializer.Serialize(@event, @event.GetType())`
- `GetEventTypeName` — stores `Type.AssemblyQualifiedName` (short form: `TypeName, AssemblyName`)
- `Deserialize` — resolves type via `Type.GetType(eventType)`, then `JsonSerializer.Deserialize(payload, resolvedType)`

**Security note:** Type-name-based deserialization is restricted to types implementing `ISerializableEvent`. The `JsonOutboxSerializer` validates that the resolved type implements `ISerializableEvent` before deserializing.

Users can replace the default serializer via DI registration to use `Newtonsoft.Json`, a type-name mapping strategy, or custom serialization.

### IOutboxStore

```csharp
public interface IOutboxStore
{
    Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
    Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
```

`GetPendingAsync` returns messages where `ProcessedAtUtc IS NULL AND DeadLetteredAtUtc IS NULL AND RetryCount < MaxRetries`, ordered by `CreatedAtUtc ASC`. This ensures dead-lettered messages are excluded from polling.

### OutboxOptions

```csharp
public class OutboxOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 5;
    public TimeSpan CleanupAge { get; set; } = TimeSpan.FromDays(7);
    public string TableName { get; set; } = "__OutboxMessages";
}
```

### OutboxEventRouter

Implements `IEventRouter`. Replaces `InMemoryTransactionalEventRouter` when outbox is enabled.

- `AddTransactionalEvent(ISerializableEvent)` — **buffers the event in an internal list** (this method is `void` per the `IEventRouter` contract, so no async I/O here). Serialization and persistence happen later in `RouteEventsAsync`.
- `AddTransactionalEvents(IEnumerable<ISerializableEvent>)` — batch version, also buffers in memory
- `PersistBufferedEventsAsync(CancellationToken)` — **new method (not on IEventRouter):** Serializes buffered events via `IOutboxSerializer`, creates `OutboxMessage` instances (with `Id` from `IGuidGenerator`, `CorrelationId` and `TenantId` from ambient context), and calls `IOutboxStore.SaveAsync()` for each. Clears the buffer after persistence. Called by `OutboxEntityEventTracker.PersistEventsAsync()`.
- `RouteEventsAsync(CancellationToken)` — Reads pending messages from `IOutboxStore.GetPendingAsync()`, deserializes via `IOutboxSerializer`, dispatches via `IEventProducer` instances. On success, calls `MarkProcessedAsync()`. Failures are logged but not thrown (background poller will retry). Called by `OutboxEntityEventTracker.EmitTransactionalEventsAsync()` (post-commit immediate dispatch).
- `RouteEventsAsync(IEnumerable<ISerializableEvent>, CancellationToken)` — dispatches specific events directly (not from outbox store)

**Note on sync/async:** The `IEventRouter.AddTransactionalEvent()` method is `void` (synchronous), so `OutboxEventRouter` buffers events in memory. The actual async persistence to `IOutboxStore` happens in `RouteEventsAsync()`, which is `Task`-returning. This avoids sync-over-async issues and is consistent with the existing `InMemoryTransactionalEventRouter` which also buffers in a `ConcurrentQueue`.

**Concurrency with background poller:** Both `RouteEventsAsync` (immediate) and `OutboxProcessingService` (poller) may attempt to process the same message. This is acceptable — at-least-once semantics means duplicate dispatch is expected. Consumers must be idempotent. The immediate dispatch calls `MarkProcessedAsync` on success; the poller skips already-processed messages via the `GetPendingAsync` filter.

### OutboxProcessingService

`IHostedService` that runs a background loop. Injects `IServiceScopeFactory` (not `IOutboxStore` directly) and creates a new `IServiceScope` per polling iteration to resolve scoped dependencies.

1. Creates a new `IServiceScope`
2. Resolves `IOutboxStore`, `IOutboxSerializer`, `IEventProducer` instances from the scope
3. Polls `IOutboxStore.GetPendingAsync(batchSize)` on the configured interval
4. Deserializes each `OutboxMessage` back to its `ISerializableEvent` type via `IOutboxSerializer`
5. Dispatches via the registered `IEventProducer` instances (using `EventSubscriptionManager` for filtering)
6. On success: calls `IOutboxStore.MarkProcessedAsync()`
7. On failure: calls `IOutboxStore.MarkFailedAsync()`, increments `RetryCount`
8. Messages exceeding `MaxRetries`: calls `IOutboxStore.MarkDeadLetteredAsync()` and logs a warning
9. Periodically calls `IOutboxStore.DeleteProcessedAsync(CleanupAge)` and `IOutboxStore.DeleteDeadLetteredAsync(CleanupAge)` to prune old entries
10. Disposes the scope

---

## Two-Phase UnitOfWork Flow

The existing `UnitOfWork.CommitAsync()` dispatches events **after** commit (Phase 3 only). The outbox requires events to be persisted **before** commit (within the same transaction). The changes below will be made during outbox implementation — the current codebase does not yet have `PersistEventsAsync` on `IEntityEventTracker`.

### Changes to IEntityEventTracker

Add `PersistEventsAsync` and add `CancellationToken` to `EmitTransactionalEventsAsync` (consistency fix):

```csharp
public interface IEntityEventTracker
{
    void AddEntity(IBusinessEntity entity);
    ICollection<IBusinessEntity> TrackedEntities { get; }
    Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default);  // MODIFIED: added CT
    Task PersistEventsAsync(CancellationToken cancellationToken = default);                  // NEW
}
```

**Breaking change:** `EmitTransactionalEventsAsync()` now accepts an optional `CancellationToken`. Existing callers without the parameter continue to compile (default value).

### InMemoryEntityEventTracker

```csharp
// PersistEventsAsync is a no-op for in-memory
public Task PersistEventsAsync(CancellationToken cancellationToken = default)
    => Task.CompletedTask;

// EmitTransactionalEventsAsync updated to accept CT (passed through to IEventRouter)
public async Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default)
{
    // existing logic, passes cancellationToken to _eventRouter.RouteEventsAsync(ct)
}
```

### OutboxEntityEventTracker (new, true decorator)

Holds an inner `InMemoryEntityEventTracker` reference via constructor injection (the DI container registers `InMemoryEntityEventTracker` as a concrete type, and `OutboxEntityEventTracker` as `IEntityEventTracker`). The decorator reuses the inner tracker's entity graph walking logic — no duplication. The data flow is:

```
PersistEventsAsync() (phase 1, within transaction):
  → delegates to inner InMemoryEntityEventTracker to walk entity graph
  → collects LocalEvents from each entity
  → calls IEventRouter.AddTransactionalEvent() for each event (buffers in OutboxEventRouter)
  → calls OutboxEventRouter.PersistBufferedEventsAsync() to flush buffer → IOutboxStore.SaveAsync()

EmitTransactionalEventsAsync() (phase 3, after commit):
  → calls IEventRouter.RouteEventsAsync()
  → OutboxEventRouter reads pending from IOutboxStore, dispatches via IEventProducer
```

The `OutboxEntityEventTracker` delegates to the `IEventRouter` — it does NOT directly call `IOutboxStore`. The `OutboxEventRouter` is the single component that writes to the store.

### Revised UnitOfWork.CommitAsync()

```csharp
public async Task CommitAsync(CancellationToken cancellationToken = default)
{
    // guards...
    _state = UnitOfWorkState.CommitAttempted;

    // Phase 1: persist events to outbox (within active transaction)
    if (_eventTracker != null)
    {
        await _eventTracker.PersistEventsAsync(cancellationToken).ConfigureAwait(false);
    }

    // Phase 2: commit transaction (domain writes + outbox writes atomically)
    _transactionScope.Complete();
    _transactionScope.Dispose();
    _transactionScopeDisposed = true;
    _state = UnitOfWorkState.Completed;

    // Phase 3: immediate dispatch attempt (best-effort, failures handled by poller)
    if (_eventTracker != null)
    {
        await _eventTracker
            .EmitTransactionalEventsAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
```

When outbox is NOT configured, `PersistEventsAsync()` is a no-op and `EmitTransactionalEventsAsync()` dispatches from memory as before.

---

## ORM-Specific Outbox Stores

All implementations share the same table schema (table name configurable via `OutboxOptions.TableName`, default `__OutboxMessages`):

```
__OutboxMessages
├── Id                (uniqueidentifier, PK)
├── EventType         (nvarchar(1024))
├── EventPayload      (nvarchar(max))
├── CreatedAtUtc      (datetimeoffset)
├── ProcessedAtUtc    (datetimeoffset, nullable)
├── DeadLetteredAtUtc (datetimeoffset, nullable)
├── ErrorMessage      (nvarchar(max), nullable)
├── RetryCount        (int, default 0)
├── CorrelationId     (nvarchar(256), nullable)
└── TenantId          (nvarchar(256), nullable)
```

`OutboxMessage.Id` is generated via `IGuidGenerator` (which produces v7 UUIDs when configured, providing time-ordered keys for index performance).

### EF Core (`Src/RCommon.EfCore/Outbox/`)

- `EFCoreOutboxStore` — uses `RCommonDbContext`. `SaveAsync()` adds the `OutboxMessage` entity to the change tracker and calls `DbContext.SaveChangesAsync()`. Because the `DbContext` connection is enlisted in the ambient `TransactionScope` from `UnitOfWork`, both domain entity changes (saved earlier by repository `SaveAsync` calls) and outbox messages commit atomically when the `TransactionScope` completes.
- `OutboxMessageConfiguration` — `IEntityTypeConfiguration<OutboxMessage>` mapping to `__OutboxMessages` (reads table name from `OutboxOptions`)
- Convenience extension: `modelBuilder.AddOutboxMessages()` to apply configuration

**Important:** For EF Core to enlist in `TransactionScope`, the database connection must support distributed transactions or use `Enlist=true` in the connection string (SQL Server). For PostgreSQL, `Npgsql` enlists automatically. This is an existing requirement of `UnitOfWork`'s `TransactionScope` usage, not new to the outbox.

### Dapper (`Src/RCommon.Dapper/Outbox/`)

- `DapperOutboxStore` — raw SQL via `IDbConnection`. Enlists in the ambient `TransactionScope` from `UnitOfWork`. SQL statements use the table name from `OutboxOptions.TableName`.

### Linq2Db (`Src/RCommon.Linq2Db/Outbox/`)

- `Linq2DbOutboxStore` — uses `DataConnection.InsertAsync<OutboxMessage>()`. Enlists in the ambient `TransactionScope` from `UnitOfWork`.

### Migration Strategy

- **EF Core users:** Add `modelBuilder.AddOutboxMessages()` to their `DbContext.OnModelCreating()` and run `dotnet ef migrations add AddOutboxMessages`. Standard EF Core migration workflow.
- **Dapper / Linq2Db users:** A SQL script is provided in the package documentation for each supported database (SQL Server, PostgreSQL). Users execute the script manually or integrate it into their migration tooling.

---

## MassTransit Native Outbox (`Src/RCommon.MassTransit.Outbox/` — NEW PROJECT)

Separate project to keep `RCommon.MassTransit` lean (no EF Core dependency).

### Project References & Dependencies

- Project: `RCommon.MassTransit`, `RCommon.Persistence`
- NuGet: `MassTransit.EntityFrameworkCore`

### Fluent API

```csharp
public interface IMassTransitOutboxBuilder
{
    IMassTransitOutboxBuilder UsePostgres();
    IMassTransitOutboxBuilder UseSqlServer();
    IMassTransitOutboxBuilder UseBusOutbox(Action<IBusOutboxConfigurator>? configure = null);
}

// Extension on IMassTransitEventHandlingBuilder
public static IMassTransitEventHandlingBuilder AddOutbox<TDbContext>(
    this IMassTransitEventHandlingBuilder builder,
    Action<IMassTransitOutboxBuilder>? configure = null)
    where TDbContext : DbContext
```

Delegates to MassTransit's `AddEntityFrameworkOutbox<TDbContext>()` and optionally `UseBusOutbox()`. Does NOT register `OutboxEventRouter` or `OutboxProcessingService` — MassTransit handles everything natively.

---

## Wolverine Native Outbox (`Src/RCommon.Wolverine.Outbox/` — NEW PROJECT)

Separate project to keep `RCommon.Wolverine` lean (no EF Core dependency).

### Project References & Dependencies

- Project: `RCommon.Wolverine`, `RCommon.Persistence`
- NuGet: `WolverineFx.EntityFrameworkCore`

### Fluent API

```csharp
public interface IWolverineOutboxBuilder
{
    IWolverineOutboxBuilder UseEntityFrameworkCoreTransactions();
}

// Extension on IWolverineEventHandlingBuilder
public static IWolverineEventHandlingBuilder AddOutbox(
    this IWolverineEventHandlingBuilder builder,
    Action<IWolverineOutboxBuilder>? configure = null)
```

Delegates to Wolverine's `UseEntityFrameworkCoreTransactions()` and configures durable messaging. Does NOT register `OutboxEventRouter` or `OutboxProcessingService` — Wolverine handles everything natively.

---

## Builder / Fluent API & DI Registration

### Generic Outbox (on persistence builders)

```csharp
// Extension method on IPersistenceBuilder
public static IPersistenceBuilder AddOutbox<TOutboxStore>(
    this IPersistenceBuilder builder,
    Action<OutboxOptions>? configure = null)
    where TOutboxStore : class, IOutboxStore
```

**Registers:**
1. `IOutboxStore` → ORM-specific implementation (scoped)
2. `IOutboxSerializer` → `JsonOutboxSerializer` (singleton, replaceable)
3. `IEventRouter` → `OutboxEventRouter` (scoped, replaces `InMemoryTransactionalEventRouter`)
4. `IEntityEventTracker` → `OutboxEntityEventTracker` (scoped, replaces `InMemoryEntityEventTracker`)
5. `OutboxProcessingService` as `IHostedService` (singleton, uses `IServiceScopeFactory` internally)
6. `OutboxOptions` via `IOptions<OutboxOptions>`

### Usage Examples

**EF Core:**
```csharp
builder.WithPersistence<EFCorePerisistenceBuilder>(ef =>
{
    ef.AddDbContext<MyDbContext>("default", options => ...);
    ef.AddOutbox<EFCoreOutboxStore>(outbox =>
    {
        outbox.PollingInterval = TimeSpan.FromSeconds(5);
        outbox.MaxRetries = 5;
        outbox.BatchSize = 100;
    });
});
```

**Dapper:**
```csharp
builder.WithPersistence<DapperPersistenceBuilder>(dapper =>
{
    dapper.AddDbConnection<SqlConnection>("default", options => ...);
    dapper.AddOutbox<DapperOutboxStore>(outbox => { ... });
});
```

**MassTransit native outbox:**
```csharp
builder.WithEventHandling<MassTransitEventHandlingBuilder>(mt =>
{
    mt.AddOutbox<MyDbContext>(outbox =>
    {
        outbox.UseSqlServer();
        outbox.UseBusOutbox();
    });
});
```

**Wolverine native outbox:**
```csharp
builder.WithEventHandling<WolverineEventHandlingBuilder>(w =>
{
    w.AddOutbox(outbox =>
    {
        outbox.UseEntityFrameworkCoreTransactions();
    });
});
```

---

## Projects & Dependencies Summary

### Existing Projects (modified)

| Project | Changes | New Dependencies |
|---------|---------|-----------------|
| `RCommon.Persistence` | Add `IOutboxMessage`, `IOutboxStore`, `IOutboxSerializer`, `OutboxMessage`, `OutboxOptions`, `JsonOutboxSerializer`, `OutboxEventRouter`, `OutboxProcessingService`, `OutboxEntityEventTracker`, builder extensions | `Microsoft.Extensions.Hosting.Abstractions` (explicit PackageReference, not relying on transitive from RCommon.Core) |
| `RCommon.EfCore` | Add `Outbox/EFCoreOutboxStore`, `Outbox/OutboxMessageConfiguration`, `ModelBuilderExtensions` | None |
| `RCommon.Dapper` | Add `Outbox/DapperOutboxStore` | None |
| `RCommon.Linq2Db` | Add `Outbox/Linq2DbOutboxStore` | None |
| `RCommon.Entities` | Modify `IEntityEventTracker` (add `PersistEventsAsync`, add CT to `EmitTransactionalEventsAsync`), `InMemoryEntityEventTracker` (no-op impl + CT) | None |
| `RCommon.Persistence` | Modify `UnitOfWork.CommitAsync()` (two-phase) | None |

### New Projects

| Project | References | NuGet Dependencies |
|---------|-----------|-------------------|
| `Src/RCommon.MassTransit.Outbox` | `RCommon.MassTransit`, `RCommon.Persistence` | `MassTransit.EntityFrameworkCore` |
| `Src/RCommon.Wolverine.Outbox` | `RCommon.Wolverine`, `RCommon.Persistence` | `WolverineFx.EntityFrameworkCore` |

### New Test Projects

| Project | References |
|---------|-----------|
| `Tests/RCommon.MassTransit.Outbox.Tests` | `RCommon.MassTransit.Outbox` |
| `Tests/RCommon.Wolverine.Outbox.Tests` | `RCommon.Wolverine.Outbox` |

---

## Changes to Existing Code

### IEntityEventTracker (breaking interface change)

```csharp
public interface IEntityEventTracker
{
    void AddEntity(IBusinessEntity entity);
    ICollection<IBusinessEntity> TrackedEntities { get; }
    Task<bool> EmitTransactionalEventsAsync(CancellationToken cancellationToken = default);  // MODIFIED: added CT
    Task PersistEventsAsync(CancellationToken cancellationToken = default);                  // NEW
}
```

### InMemoryEntityEventTracker

```csharp
public Task PersistEventsAsync(CancellationToken cancellationToken = default)
    => Task.CompletedTask;  // no-op for in-memory
```

### UnitOfWork.CommitAsync() (revised order)

```csharp
public async Task CommitAsync(CancellationToken cancellationToken = default)
{
    // guards...
    _state = UnitOfWorkState.CommitAttempted;

    // Phase 1: persist events to outbox (within active transaction)
    if (_eventTracker != null)
    {
        await _eventTracker.PersistEventsAsync(cancellationToken).ConfigureAwait(false);
    }

    // Phase 2: commit transaction (domain writes + outbox writes atomically)
    _transactionScope.Complete();
    _transactionScope.Dispose();
    _transactionScopeDisposed = true;
    _state = UnitOfWorkState.Completed;

    // Phase 3: immediate dispatch attempt (best-effort, failures handled by poller)
    if (_eventTracker != null)
    {
        await _eventTracker
            .EmitTransactionalEventsAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
```

---

## Testing Strategy

### Unit Tests (additions to existing test projects)

**`Tests/RCommon.Persistence.Tests/`:**
- `OutboxEventRouterTests` — serialization via IOutboxSerializer, store calls, immediate dispatch, failure handling
- `OutboxProcessingServiceTests` — polling, scope creation per iteration, batch dispatch, retry logic, max retries → dead letter, cleanup
- `OutboxMessageTests` — serialization/deserialization round-trip via JsonOutboxSerializer
- `OutboxEntityEventTrackerTests` — two-phase flow, entity graph walking, delegates to IEventRouter (not IOutboxStore directly)
- `UnitOfWorkOutboxIntegrationTests` — full two-phase commit flow with mocked store/producers

**`Tests/RCommon.EfCore.Tests/`:**
- `EFCoreOutboxStoreTests` — CRUD operations against in-memory SQLite DbContext

**`Tests/RCommon.Dapper.Tests/`:**
- `DapperOutboxStoreTests` — CRUD operations with raw SQL

**`Tests/RCommon.Linq2Db.Tests/`:**
- `Linq2DbOutboxStoreTests` — CRUD operations via DataConnection

**`Tests/RCommon.MassTransit.Outbox.Tests/` (new):**
- `MassTransitOutboxBuilderTests` — verifies native outbox service registration

**`Tests/RCommon.Wolverine.Outbox.Tests/` (new):**
- `WolverineOutboxBuilderTests` — verifies native outbox service registration

### Concurrency & Edge Case Tests

- **Concurrent dispatch test** — verifies that immediate dispatch + poller both processing the same message results in at-least-once delivery (not corruption)
- **Transaction rollback test** — verifies outbox messages are NOT persisted when the TransactionScope rolls back
- **Dead letter test** — verifies messages exceeding MaxRetries are marked dead-lettered and excluded from future GetPendingAsync calls

### Test Frameworks

- xUnit 2.9.3, FluentAssertions 8.2.0, Moq 4.20.72 (from `Directory.Build.props`)

---

## Non-Breaking Guarantee

When outbox is NOT configured:
- `IEntityEventTracker` → `InMemoryEntityEventTracker` (unchanged, `PersistEventsAsync` is no-op)
- `IEventRouter` → `InMemoryTransactionalEventRouter` (unchanged)
- `UnitOfWork.CommitAsync()` — `PersistEventsAsync` is no-op, `EmitTransactionalEventsAsync` dispatches from memory as before
- No `OutboxProcessingService` registered
- **Behavior is identical to today**

---

## Concurrency Model

The outbox guarantees **at-least-once delivery**. Duplicate dispatch is expected and consumers must be idempotent.

- **Immediate dispatch** (in `OutboxEventRouter.RouteEventsAsync`): runs synchronously after commit, calls `MarkProcessedAsync` on success
- **Background poller** (`OutboxProcessingService`): runs on a timer, picks up messages where `ProcessedAtUtc IS NULL AND DeadLetteredAtUtc IS NULL`
- **Race window:** If immediate dispatch succeeds but `MarkProcessedAsync` fails (crash), the poller will re-dispatch. This is the at-least-once guarantee.
- **No distributed locking:** Single-process deployment assumed for V1. Horizontal scaling with multiple poller instances would require a `LockedUntilUtc` claim mechanism (documented as future enhancement).

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| Replace `IEventRouter` (not producers or UoW) | Minimal code changes; router contract already fits the outbox write/dispatch pattern |
| Two-phase commit in UoW | Events must be persisted within the transaction for atomicity; dispatch happens after commit |
| Separate MassTransit.Outbox / Wolverine.Outbox projects | Keeps base messaging packages lean; EF Core outbox dependency is opt-in |
| Generic outbox for all three ORMs | Dapper/Linq2Db users need outbox without a messaging framework |
| Background poller + immediate attempt | Best latency (immediate) with guaranteed delivery (poller catches failures) |
| `PersistEventsAsync` no-op for in-memory | Zero behavior change for non-outbox users |
| Shared `__OutboxMessages` table schema | All ORMs read/write the same table; enables mixed ORM scenarios |
| `OutboxEntityEventTracker` as decorator | Adds outbox persistence without rewriting entity graph walking logic |
| `IOutboxSerializer` abstraction | Pluggable serialization; default `System.Text.Json` with type-safe deserialization |
| `IServiceScopeFactory` in hosted service | Singleton `OutboxProcessingService` cannot resolve scoped `IOutboxStore` directly |
| `DeadLetteredAtUtc` column | Dead-lettered messages are excluded from polling and can be cleaned up separately |
| `CorrelationId` / `TenantId` on outbox message | Multi-tenant and observability support; poller restores context when dispatching |
| Configurable table name | Avoids conflicts with user conventions or multi-schema deployments |
| `IGuidGenerator` for message IDs | Produces v7 UUIDs when configured, providing time-ordered keys for index performance |
| At-least-once / no distributed lock (V1) | Simple single-process model; distributed locking is a future enhancement |

---

## Future Enhancements (V2)

- **Exponential backoff:** Add `NextRetryAtUtc` column and configurable backoff strategy for failed messages
- **Distributed locking:** Add `LockedUntilUtc` / `ClaimAsync` for horizontal scaling with multiple poller instances
- **Dead letter replay:** Add `IOutboxStore.GetDeadLettersAsync()` and replay API for operational recovery
- **Inbox (idempotency):** Add `__InboxMessages` table to deduplicate incoming events at the consumer level
