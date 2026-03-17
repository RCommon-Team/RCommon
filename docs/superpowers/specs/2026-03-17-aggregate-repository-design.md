# DDD Infrastructure — Design Specification

**Date:** 2026-03-17
**Branch:** feature/ddd
**Status:** Approved

## Problem

The existing repository interfaces (`ILinqRepository<T>`, `IGraphRepository<T>`, `ISqlMapperRepository<T>`) accept any `IBusinessEntity` type parameter. There is no compile-time enforcement that prevents persisting child entities (`DomainEntity<TKey>`) directly, bypassing the aggregate root boundary. Additionally, domain events raised by aggregates are not automatically dispatched after persistence, there is no dedicated read-model query path, and there is no saga/process manager infrastructure for coordinating multi-step workflows.

## Goal

Extend RCommon's DDD support with four interconnected capabilities:

1. **Aggregate Repository** — `IAggregateRepository<TAggregate, TKey>` with compile-time enforcement, DDD-constrained API, open-generic registration, and non-breaking coexistence with existing repositories.
2. **Automatic Domain Event Dispatch** — UnitOfWork post-commit hook that dispatches accumulated domain events through the existing `IEntityEventTracker` → `IEventRouter` → `IEventProducer` pipeline.
3. **Read-Model Repositories** — `IReadModelRepository<TReadModel>` for CQRS query-side access with paging, counting, and compile-time separation from write-model types.
4. **Saga & Process Manager Patterns** — `ISaga<TState, TKey>` orchestration with `IStateMachine<TState, TTrigger>` abstraction over state machine libraries (Stateless, MassTransit), `ISagaStore<TState, TKey>` for persistence, plus choreography via existing event infrastructure.

## Non-Goals

- Event sourcing integration (prepared for via `AggregateRoot.Version`, but not implemented here)
- Transactional outbox pattern (future enhancement for reliable event delivery)
- Concrete state machine adapters beyond interface definitions (Stateless/MassTransit adapters are separate packages)

---

## Part 1: Aggregate Repository

### Interface Hierarchy

The new interface sits alongside (not above) the existing repository interfaces:

```
Existing (unchanged):
  IReadOnlyRepository<TEntity> where TEntity : IBusinessEntity
  IWriteOnlyRepository<TEntity> where TEntity : IBusinessEntity
  ILinqRepository<TEntity> : IReadOnlyRepository, IWriteOnlyRepository, IEagerLoadableQueryable
  IGraphRepository<TEntity> : ILinqRepository
  ISqlMapperRepository<TEntity> : IReadOnlyRepository, IWriteOnlyRepository

New:
  IAggregateRepository<TAggregate, TKey>
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey>
```

`IAggregateRepository` does NOT inherit from `ILinqRepository`, `IGraphRepository`, or any existing repository interface. It does inherit `INamedDataSource` to support multi-database scenarios (consistent with all existing repository interfaces). This prevents consumers from casting up to the full query surface while preserving data store targeting.

### Interface Definition

**Location:** `Src/RCommon.Persistence/Crud/IAggregateRepository.cs`

```csharp
public interface IAggregateRepository<TAggregate, TKey> : INamedDataSource
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    // Read
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TAggregate?> FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    // Write
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    // Eager loading (fluent builder for aggregate graph)
    IAggregateRepository<TAggregate, TKey> Include<TProperty>(
        Expression<Func<TAggregate, TProperty>> path);
    IAggregateRepository<TAggregate, TKey> ThenInclude<TPreviousProperty, TProperty>(
        Expression<Func<TPreviousProperty, TProperty>> path);
}
```

**API decisions:**

- **No `FindAllAsync`** — Aggregates should be loaded individually. Collection queries belong in read models or query handlers.
- **`ExistsAsync(TKey id)`** — Lightweight existence check without loading the full aggregate. Useful for validation before operations.
- **No `GetCountAsync`/`AnyAsync`** — Query/reporting concerns, not aggregate operations.
- **Include/ThenInclude** — Fluent chaining for eager loading child entities within the aggregate boundary. Returns `IAggregateRepository` for chaining. Note: this uses a generic `TProperty` parameter (not `object` like the existing `ILinqRepository.Include`), which provides stronger typing. Concrete implementations use **explicit interface implementation** to satisfy both the `IAggregateRepository.Include<TProperty>` (returning `IAggregateRepository`) and the inherited base class `Include` (returning `IEagerLoadableQueryable<TEntity>`) separately.
- **`INamedDataSource` inheritance** — Exposes `DataStoreName` property for multi-database targeting, consistent with all existing repository interfaces.
- **All methods have `CancellationToken`** — Consistent with the async hardening work done in prior commits.
- **Immediate save semantics** — `AddAsync`/`UpdateAsync`/`DeleteAsync` call `SaveChangesAsync` immediately, matching the existing repository behavior. Future UnitOfWork integration may defer persistence, but that is out of scope for this spec.

### Known Trade-offs

- **Base class API surface leak:** The concrete implementations inherit from ORM base classes (e.g., `GraphRepositoryBase<TAggregate>`), which means the concrete type also implements `IGraphRepository<TAggregate>` and its full hierarchy (~25+ methods from `LinqRepositoryBase`). These base class abstract methods are inherited/delegated automatically — the aggregate repository only exposes the narrow `IAggregateRepository` surface via DI. Runtime casting from `IAggregateRepository` to `IGraphRepository` would succeed but is the consumer's responsibility to avoid. This is an acceptable trade-off for infrastructure reuse (event tracking, data store resolution, soft-delete/tenant filtering, logging).

### Concrete Implementations

Each ORM gets one concrete implementation that inherits from its existing repository base class for infrastructure reuse (event tracking, data store resolution, soft-delete/tenant filtering, logging).

#### EFCore

**Location:** `Src/RCommon.EfCore/Crud/EFCoreAggregateRepository.cs`

`EFCoreAggregateRepository<TAggregate, TKey> : GraphRepositoryBase<TAggregate>, IAggregateRepository<TAggregate, TKey>`

- `GetByIdAsync` → `FilteredRepositoryQuery.FirstOrDefaultAsync(e => e.Id.Equals(id))` (uses queryable path, not `DbSet.FindAsync`, because `FindAsync` ignores `Include` chains)
- `FindAsync` → `FilteredRepositoryQuery.Where(spec.Predicate).FirstOrDefaultAsync()`
- `ExistsAsync` → `FilteredRepositoryQuery.AnyAsync(e => e.Id.Equals(id))`
- `AddAsync` → `DbSet.AddAsync(aggregate)` + `EventTracker.AddEntity(aggregate)` + `SaveChangesAsync()` (matches existing `EFCoreRepository` immediate-save behavior)
- `UpdateAsync` → `DbSet.Update(aggregate)` + `EventTracker.AddEntity(aggregate)` + `SaveChangesAsync()`
- `DeleteAsync` → soft-delete via `ISoftDelete` or `DbSet.Remove(aggregate)` + `EventTracker.AddEntity(aggregate)` + `SaveChangesAsync()`. Supports the same dual-mode delete behavior as existing `EFCoreRepository` (physical delete by default, soft-delete when aggregate implements `ISoftDelete`).
- `Include/ThenInclude` → builds `IQueryable<TAggregate>` using EF Core's `EntityFrameworkQueryableExtensions.Include/ThenInclude`. The `Include` method on `IAggregateRepository` is an explicit interface implementation returning `IAggregateRepository`; the inherited base class `Include` (returning `IEagerLoadableQueryable`) is also implemented for internal use. Both methods can coexist because explicit interface implementation disambiguates them.

#### Dapper

**Location:** `Src/RCommon.Dapper/Crud/DapperAggregateRepository.cs`

`DapperAggregateRepository<TAggregate, TKey> : SqlRepositoryBase<TAggregate>, IAggregateRepository<TAggregate, TKey>`

- `GetByIdAsync` → `connection.GetAsync<TAggregate>(id)` via Dommel
- `FindAsync` → `connection.SelectAsync<TAggregate>(spec.Predicate).FirstOrDefault()`
- `ExistsAsync` → `connection.GetAsync<TAggregate>(id) != null`
- `AddAsync/UpdateAsync/DeleteAsync` → Dommel CRUD operations + `EventTracker.AddEntity(aggregate)`
- `Include/ThenInclude` → no-op (returns `this`). Dapper does not support eager loading natively; aggregate child loading must be handled manually or via multi-queries in domain-specific repository subclasses.

#### Linq2Db

**Location:** `Src/RCommon.Linq2Db/Crud/Linq2DbAggregateRepository.cs`

`Linq2DbAggregateRepository<TAggregate, TKey> : LinqRepositoryBase<TAggregate>, IAggregateRepository<TAggregate, TKey>`

- `GetByIdAsync` → `Table.FirstOrDefaultAsync(e => e.Id.Equals(id))`
- `FindAsync` → `Table.Where(spec.Predicate).FirstOrDefaultAsync()`
- `ExistsAsync` → `Table.AnyAsync(e => e.Id.Equals(id))`
- `AddAsync/UpdateAsync/DeleteAsync` → Linq2Db CRUD operations + `EventTracker.AddEntity(aggregate)`
- `Include/ThenInclude` → uses Linq2Db's `LoadWith` where applicable

### DI Registration

Each ORM builder adds the open-generic registration in its constructor, alongside the existing repository registrations.

**EFCorePerisistenceBuilder** (note: existing filename has `Perisistence` typo):
```csharp
// Existing
services.AddTransient(typeof(IGraphRepository<>), typeof(EFCoreRepository<>));
// New
services.AddTransient(typeof(IAggregateRepository<,>), typeof(EFCoreAggregateRepository<,>));
```

**DapperPersistenceBuilder:**
```csharp
// Existing
services.AddTransient(typeof(ISqlMapperRepository<>), typeof(DapperRepository<>));
// New
services.AddTransient(typeof(IAggregateRepository<,>), typeof(DapperAggregateRepository<,>));
```

**Linq2DbPersistenceBuilder:**
```csharp
// Existing
services.AddTransient(typeof(ILinqRepository<>), typeof(Linq2DbRepository<>));
// New
services.AddTransient(typeof(IAggregateRepository<,>), typeof(Linq2DbAggregateRepository<,>));
```

### Consumer Usage

```csharp
public class PlaceOrderHandler
{
    private readonly IAggregateRepository<Order, Guid> _orders;

    public PlaceOrderHandler(IAggregateRepository<Order, Guid> orders)
    {
        _orders = orders;
    }

    public async Task HandleAsync(PlaceOrderCommand cmd, CancellationToken ct)
    {
        var order = new Order(cmd.CustomerId);
        order.AddLineItem(cmd.ProductId, cmd.Quantity, cmd.Price);

        await _orders.AddAsync(order, ct);
    }
}
```

---

## Part 2: Automatic Domain Event Dispatch

### Mechanism

The `UnitOfWork` gains an optional dependency on `IEntityEventTracker`. After the transaction is fully committed (i.e., after `TransactionScope.Dispose()` following a `Complete()` call), it dispatches accumulated domain events through the existing pipeline: `IEntityEventTracker` → `IEventRouter` → `IEventProducer` → `IEventBus` → `ISubscriber<TEvent>`.

**Critical timing detail:** `TransactionScope.Complete()` only *marks* the scope as ready to commit. The actual database commit occurs when `TransactionScope.Dispose()` is called. Therefore, event dispatch must happen *after* scope disposal, not between `Complete()` and `Dispose()`.

### Updated IUnitOfWork Interface

**Location:** `Src/RCommon.Persistence/Transactions/IUnitOfWork.cs`

```csharp
public interface IUnitOfWork : IDisposable
{
    Guid TransactionId { get; }
    TransactionMode TransactionMode { get; set; }
    IsolationLevel IsolationLevel { get; set; }
    UnitOfWorkState State { get; }
    bool AutoComplete { get; }

    [Obsolete("Use CommitAsync instead for automatic domain event dispatch.")]
    void Commit();
    Task CommitAsync(CancellationToken cancellationToken = default);
}
```

**Note:** `IUnitOfWork` remains `IDisposable` only (not `IAsyncDisposable`). Adding `IAsyncDisposable` would be a breaking change for any external `IUnitOfWork` implementations. The concrete `UnitOfWork` class already inherits `IAsyncDisposable` from `DisposableResource` for callers that need `await using`.

### Modified UnitOfWork Implementation

**Location:** `Src/RCommon.Persistence/Transactions/UnitOfWork.cs`

The existing constructor signatures are preserved. `IEntityEventTracker?` is added as an optional parameter to both overloads for backward compatibility:

```csharp
public class UnitOfWork : DisposableResource, IUnitOfWork
{
    private readonly ILogger<UnitOfWork> _logger;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IEntityEventTracker? _eventTracker;
    private UnitOfWorkState _state;
    private TransactionScope _transactionScope;
    private bool _transactionScopeDisposed;

    // Overload 1: settings-based (used by UnitOfWorkFactory)
    public UnitOfWork(
        ILogger<UnitOfWork> logger,
        IGuidGenerator guidGenerator,
        IOptions<UnitOfWorkSettings> unitOfWorkSettings,
        IEntityEventTracker? eventTracker = null)
    {
        _logger = logger;
        _guidGenerator = guidGenerator;
        _eventTracker = eventTracker;
        TransactionId = _guidGenerator.Create();
        TransactionMode = TransactionMode.Default;
        IsolationLevel = unitOfWorkSettings.Value.DefaultIsolation;
        AutoComplete = unitOfWorkSettings.Value.AutoCompleteScope;
        _state = UnitOfWorkState.Created;
        _transactionScope = TransactionScopeHelper.CreateScope(_logger, this);
    }

    // Overload 2: explicit settings
    public UnitOfWork(
        ILogger<UnitOfWork> logger,
        IGuidGenerator guidGenerator,
        TransactionMode transactionMode,
        IsolationLevel isolationLevel,
        IEntityEventTracker? eventTracker = null)
    {
        _logger = logger;
        _guidGenerator = guidGenerator;
        _eventTracker = eventTracker;
        TransactionId = _guidGenerator.Create();
        TransactionMode = transactionMode;
        IsolationLevel = isolationLevel;
        AutoComplete = false;
        _state = UnitOfWorkState.Created;
        _transactionScope = TransactionScopeHelper.CreateScope(_logger, this);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Guard.Against<ObjectDisposedException>(_state == UnitOfWorkState.Disposed,
            "Cannot commit a disposed UnitOfWorkScope instance.");
        Guard.Against<UnitOfWorkException>(_state == UnitOfWorkState.Completed,
            "This unit of work scope has been marked completed.");

        _state = UnitOfWorkState.CommitAttempted;

        // 1. Mark scope for commit
        _transactionScope.Complete();

        // 2. Dispose scope — this is where the actual DB commit occurs
        _transactionScope.Dispose();
        _transactionScopeDisposed = true;
        _state = UnitOfWorkState.Completed;

        // 3. Post-commit: dispatch domain events (transaction is fully committed)
        if (_eventTracker != null)
        {
            var dispatched = await _eventTracker
                .EmitTransactionalEventsAsync()
                .ConfigureAwait(false);

            if (!dispatched)
            {
                _logger.LogWarning(
                    "UnitOfWork {TransactionId}: domain event dispatch returned false.",
                    TransactionId);
            }
        }
    }

    [Obsolete("Use CommitAsync instead for automatic domain event dispatch.")]
    public void Commit()
    {
        // Preserved for backward compatibility — no event dispatch
        Guard.Against<ObjectDisposedException>(_state == UnitOfWorkState.Disposed, ...);
        Guard.Against<UnitOfWorkException>(_state == UnitOfWorkState.Completed, ...);
        _state = UnitOfWorkState.CommitAttempted;
        _transactionScope.Complete();
        _state = UnitOfWorkState.Completed;
    }

    protected override void Dispose(bool disposing)
    {
        // ... existing logic, with guard for already-disposed scope:
        // In the finally block:
        if (!_transactionScopeDisposed)
        {
            _transactionScope.Dispose();
        }
        _state = UnitOfWorkState.Disposed;
        base.Dispose(disposing);
    }
}
```

### Design Decisions

- **`CommitAsync` as primary API** — New async method handles transaction commit + event dispatch. The synchronous `Commit()` is marked `[Obsolete]` but preserved for backward compatibility and does NOT dispatch events (avoids sync-over-async deadlocks).
- **Optional `IEntityEventTracker`** — Constructor parameter defaults to `null`. When no tracker is injected (non-DDD usage), the commit path is unchanged. No breaking change.
- **Post-commit dispatch timing** — `CommitAsync` calls `TransactionScope.Complete()` then `TransactionScope.Dispose()` before dispatching events. This ensures the database transaction is fully committed before handlers execute. The `_transactionScopeDisposed` flag prevents double-disposal in `Dispose(bool)`.
- **`EmitTransactionalEventsAsync` return value** — The existing method returns `Task<bool>`. A `false` result is logged as a warning but does not throw, because the committed data should not be rolled back due to event dispatch issues.
- **No outbox** — If event dispatch fails after commit, events are lost. A future transactional outbox pattern can address this by storing events in the same transaction and dispatching via a background worker. This is explicitly out of scope.

### UnitOfWorkBehavior Migration

The existing `UnitOfWorkRequestBehavior` (MediatR pipeline behavior) calls the synchronous `Commit()`. Since it runs in an async context (`Handle` returns `Task<TResponse>`), it should be updated to call `await CommitAsync(cancellationToken)` to enable automatic domain event dispatch and avoid sync-over-async deadlock risks.

### Event Dispatch Clarification: DomainEvents vs LocalEvents

`AggregateRoot.AddDomainEvent()` adds to both the `_domainEvents` collection and the `_localEvents` collection (via `AddLocalEvent()`). The `DomainEvents` property is a read-only view for the aggregate itself (inspection, testing). The `LocalEvents` collection is what drives the event dispatch pipeline through `IEntityEventTracker`.

### Event Flow (End-to-End)

```
1. AggregateRoot.AddDomainEvent(new OrderCreatedEvent(...))
   → adds to DomainEvents (read-only view) + LocalEvents (dispatch pipeline)
2. Repository.AddAsync(aggregate)
   → EventTracker.AddEntity(aggregate) registers for tracking
   → SaveChangesAsync() persists to database
3. UnitOfWork.CommitAsync()
   → TransactionScope.Complete() marks scope for commit
   → TransactionScope.Dispose() — actual DB commit happens here
   → EventTracker.EmitTransactionalEventsAsync():
     - Traverses object graph for nested IBusinessEntity instances
     - Collects all LocalEvents from root + children
     - Routes via IEventRouter → IEventProducer → IEventBus
   → ISubscriber<OrderCreatedEvent>.HandleAsync() executes
```

---

## Part 3: Read-Model Repositories

### IReadModel Marker Interface

**Location:** `Src/RCommon.Persistence/IReadModel.cs`

```csharp
/// <summary>
/// Marker interface for read-model/projection types used in CQRS query-side repositories.
/// Read models are optimized for querying and do not participate in domain event tracking.
/// </summary>
public interface IReadModel { }
```

### IReadModelRepository Interface

**Location:** `Src/RCommon.Persistence/Crud/IReadModelRepository.cs`

```csharp
public interface IReadModelRepository<TReadModel> : INamedDataSource
    where TReadModel : class, IReadModel
{
    // Single result
    Task<TReadModel?> FindAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    // Collection results
    Task<IReadOnlyList<TReadModel>> FindAllAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    // Paged results
    Task<IPagedResult<TReadModel>> GetPagedAsync(
        IPagedSpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    // Counting
    Task<long> GetCountAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    // Existence
    Task<bool> AnyAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default);

    // Eager loading
    IReadModelRepository<TReadModel> Include<TProperty>(
        Expression<Func<TReadModel, TProperty>> path);
}
```

### IPagedResult Interface

**Location:** `Src/RCommon.Models/IPagedResult.cs`

```csharp
public interface IPagedResult<T>
{
    IReadOnlyList<T> Items { get; }
    long TotalCount { get; }
    int PageNumber { get; }
    int PageSize { get; }
    int TotalPages { get; }
    bool HasNextPage { get; }
    bool HasPreviousPage { get; }
}
```

**Relationship to `IPaginatedList<T>`:** The existing `IPaginatedList<T>` (in `RCommon.Core/Collections/`) extends `IList<T>` and is a mutable, self-contained collection. `IPagedResult<T>` is a read-only result envelope that wraps items with pagination metadata. They serve different purposes: `IPaginatedList<T>` for in-memory collections, `IPagedResult<T>` for query results returned from repositories.

### PagedResult Implementation

**Location:** `Src/RCommon.Models/PagedResult.cs`

```csharp
public class PagedResult<T> : IPagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public long TotalCount { get; }
    public int PageNumber { get; }
    public int PageSize { get; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;

    public PagedResult(IReadOnlyList<T> items, long totalCount, int pageNumber, int pageSize)
    {
        Guard.Against<ArgumentOutOfRangeException>(pageSize <= 0, "PageSize must be greater than zero.");
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
```

### Design Decisions

- **No write operations** — Read models are populated by event handlers or projections, not through this repository.
- **`IReadModel` constraint** — Prevents accidentally querying aggregate types through the read path.
- **`IPagedResult<T>`** — Structured paging result with total count for UI pagination. Distinct from `IPaginatedList<T>` (see above).
- **`PageSize` guard** — Constructor throws `ArgumentOutOfRangeException` if `pageSize <= 0` to prevent division-by-zero in `TotalPages`.
- **No `ThenInclude`** — Read models are typically flat/denormalized; single-level Include is sufficient.
- **No event tracking** — Read model repositories do NOT inject `IEntityEventTracker`; read operations don't produce domain events.
- **`INamedDataSource`** — Supports targeting a read-optimized database (common CQRS pattern).

### Concrete Implementations (Composition Pattern)

Read-model concrete implementations use **composition** rather than inheriting from `LinqRepositoryBase<T>` / `SqlRepositoryBase<T>`. This is necessary because those base classes constrain `TEntity` to `IBusinessEntity`, but read models implement `IReadModel` (not `IBusinessEntity`). Composition allows clean read models that are simple POCOs with only the `IReadModel` marker.

Each implementation wraps the underlying ORM data access directly:

| ORM | Class | Approach | Location |
|-----|-------|----------|----------|
| EF Core | `EFCoreReadModelRepository<T>` | Wraps `DbContext` + `DbSet<T>` directly | `Src/RCommon.EfCore/Crud/` |
| Dapper | `DapperReadModelRepository<T>` | Wraps `IDbConnection` via Dommel | `Src/RCommon.Dapper/Crud/` |
| Linq2Db | `Linq2DbReadModelRepository<T>` | Wraps `IDataContext.GetTable<T>()` | `Src/RCommon.Linq2Db/Crud/` |

Each implementation resolves its data store via `IDataStoreFactory` (injected) and `DataStoreName` (from `INamedDataSource`) to support multi-database targeting, consistent with existing repositories.

### DI Registration

Added to each ORM builder alongside existing registrations:

```csharp
// EFCore
services.AddTransient(typeof(IReadModelRepository<>), typeof(EFCoreReadModelRepository<>));

// Dapper
services.AddTransient(typeof(IReadModelRepository<>), typeof(DapperReadModelRepository<>));

// Linq2Db
services.AddTransient(typeof(IReadModelRepository<>), typeof(Linq2DbReadModelRepository<>));
```

### Consumer Usage

```csharp
// Read model (clean POCO with IReadModel marker)
public class OrderSummary : IReadModel
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = default!;
    public decimal Total { get; set; }
    public string Status { get; set; } = default!;
    public DateTimeOffset PlacedAt { get; set; }
}

// Query handler
public class GetOrderSummariesHandler
{
    private readonly IReadModelRepository<OrderSummary> _orders;

    public GetOrderSummariesHandler(IReadModelRepository<OrderSummary> orders)
    {
        _orders = orders;
    }

    public async Task<IPagedResult<OrderSummary>> HandleAsync(
        GetOrderSummaries query, CancellationToken ct)
    {
        var spec = new PagedSpecification<OrderSummary>(
            o => o.Status == query.StatusFilter,
            query.Page, query.PageSize,
            o => o.PlacedAt, SortDirection.Descending);

        return await _orders.GetPagedAsync(spec, ct);
    }
}
```

---

## Part 4: Saga & Process Manager Patterns

### 4A. State Machine Abstraction

**Location:** `Src/RCommon.Core/StateMachines/`
**Namespace:** `RCommon.StateMachines`

The state machine abstraction decouples saga logic from any specific library (Stateless, MassTransit Automatonymous, etc.). Concrete adapters are separate NuGet packages. These interfaces live in `RCommon.Core` because a state machine is a general-purpose abstraction that can exist without persistence (e.g., coordinating steps within a single request). The saga types that depend on persistence live separately in `RCommon.Persistence/Sagas/`.

```csharp
// Core abstraction
public interface IStateMachine<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    TState CurrentState { get; }
    Task FireAsync(TTrigger trigger, CancellationToken cancellationToken = default);
    Task FireAsync<TData>(TTrigger trigger, TData data, CancellationToken cancellationToken = default);
    bool CanFire(TTrigger trigger);
    IEnumerable<TTrigger> PermittedTriggers { get; }
}

// Configuration builder (fluent API for defining transitions)
public interface IStateMachineConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    IStateConfigurator<TState, TTrigger> ForState(TState state);
    IStateMachine<TState, TTrigger> Build(TState initialState);
}

public interface IStateConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    IStateConfigurator<TState, TTrigger> Permit(TTrigger trigger, TState destinationState);
    IStateConfigurator<TState, TTrigger> OnEntry(Func<CancellationToken, Task> action);
    IStateConfigurator<TState, TTrigger> OnExit(Func<CancellationToken, Task> action);
    IStateConfigurator<TState, TTrigger> PermitIf(
        TTrigger trigger, TState destinationState, Func<bool> guard);
}
```

**Concrete adapters (separate packages, out of scope for this spec):**
- `RCommon.Stateless` → `StatelessStateMachine<TState, TTrigger>` wrapping `Stateless.StateMachine<TState, TTrigger>`
- `RCommon.MassTransit` → adapter wrapping MassTransit's Automatonymous state machine

### 4B. Saga State

**Location:** `Src/RCommon.Persistence/Sagas/SagaState.cs`
**Namespace:** `RCommon.Persistence.Sagas`

```csharp
/// <summary>
/// Base class for saga state that is persisted across steps.
/// Tracks lifecycle, correlation, and fault information.
/// </summary>
public abstract class SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    public TKey Id { get; set; } = default!;
    public string CorrelationId { get; set; } = default!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string CurrentStep { get; set; } = default!;
    public bool IsCompleted { get; set; }
    public bool IsFaulted { get; set; }
    public string? FaultReason { get; set; }
    public int Version { get; set; }  // optimistic concurrency
}
```

**Note:** `CurrentStep` is stored as `string` for database serialization compatibility. The `SagaOrchestrator` handles the `string` ↔ `enum` conversion internally via `Enum.Parse<TSagaState>` / `ToString()`.

### 4C. Saga Orchestrator

**Location:** `Src/RCommon.Persistence/Sagas/ISaga.cs`

```csharp
/// <summary>
/// Defines a saga orchestrator that coordinates multi-step workflows.
/// Subscribes to domain events and advances state through a state machine.
/// </summary>
public interface ISaga<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    Task HandleAsync<TEvent>(TEvent @event, TState state, CancellationToken ct = default)
        where TEvent : ISerializableEvent;
    Task CompensateAsync(TState state, CancellationToken ct = default);
}
```

**Location:** `Src/RCommon.Persistence/Sagas/SagaOrchestrator.cs`

```csharp
/// <summary>
/// Abstract base class for saga orchestrators that use a state machine
/// to coordinate transitions between steps.
/// </summary>
public abstract class SagaOrchestrator<TState, TKey, TSagaState, TSagaTrigger>
    : ISaga<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
    where TSagaState : struct, Enum
    where TSagaTrigger : struct, Enum
{
    private readonly IStateMachineConfigurator<TSagaState, TSagaTrigger> _configurator;
    private IStateMachine<TSagaState, TSagaTrigger>? _stateMachineTemplate;

    protected ISagaStore<TState, TKey> Store { get; }

    protected SagaOrchestrator(
        ISagaStore<TState, TKey> store,
        IStateMachineConfigurator<TSagaState, TSagaTrigger> configurator)
    {
        Store = store;
        _configurator = configurator;
    }

    /// <summary>
    /// Define state machine transitions (called once during lazy initialization).
    /// </summary>
    protected abstract void ConfigureStateMachine(
        IStateMachineConfigurator<TSagaState, TSagaTrigger> configurator);

    /// <summary>
    /// Map an incoming domain event to a state machine trigger.
    /// </summary>
    protected abstract TSagaTrigger MapEventToTrigger<TEvent>(TEvent @event)
        where TEvent : ISerializableEvent;

    /// <summary>
    /// The initial state for new saga instances.
    /// </summary>
    protected abstract TSagaState InitialState { get; }

    /// <summary>
    /// Ensures the state machine configuration is applied exactly once.
    /// </summary>
    private void EnsureConfigured()
    {
        if (_stateMachineTemplate == null)
        {
            ConfigureStateMachine(_configurator);
            _stateMachineTemplate = _configurator.Build(InitialState);
        }
    }

    public async Task HandleAsync<TEvent>(TEvent @event, TState state, CancellationToken ct)
        where TEvent : ISerializableEvent
    {
        EnsureConfigured();

        // Determine the current state — use InitialState if CurrentStep is not yet set
        var currentState = string.IsNullOrEmpty(state.CurrentStep)
            ? InitialState
            : Enum.Parse<TSagaState>(state.CurrentStep);

        var machine = _configurator.Build(currentState);
        var trigger = MapEventToTrigger(@event);

        if (!machine.CanFire(trigger))
            return; // Invalid transition — ignore

        await machine.FireAsync(trigger, ct).ConfigureAwait(false);
        state.CurrentStep = machine.CurrentState.ToString()!;
        await Store.SaveAsync(state, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Execute compensation logic to reverse completed steps.
    /// </summary>
    public abstract Task CompensateAsync(TState state, CancellationToken ct);
}
```

**Key design decisions:**
- **`Store` is a `protected` property** — subclasses need access to look up saga state by correlation ID in their `ISubscriber<TEvent>.HandleAsync` methods.
- **Lazy initialization** — `ConfigureStateMachine` is called once (via `EnsureConfigured()`) and the configurator is reused. Each `HandleAsync` call builds a fresh state machine instance with the correct initial state for that saga instance.
- **Null `CurrentStep` handling** — New saga instances that haven't transitioned yet use `InitialState` instead of attempting `Enum.Parse` on a null string.
- **`struct, Enum` constraints** — `TSagaState` and `TSagaTrigger` are constrained to `struct, Enum` (C# 7.3+), making `Enum.Parse<T>` safe at compile time.

### 4D. Saga Persistence

**Location:** `Src/RCommon.Persistence/Sagas/ISagaStore.cs`

```csharp
/// <summary>
/// Persistence interface for saga state. Supports lookup by correlation ID
/// (for event-driven saga resolution) and by primary key.
/// </summary>
public interface ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
    Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task SaveAsync(TState state, CancellationToken ct = default);
    Task DeleteAsync(TState state, CancellationToken ct = default);
}
```

**Concrete implementations:**

| ORM | Class | Location | Lifetime |
|-----|-------|----------|----------|
| EF Core | `EFCoreSagaStore<TState, TKey>` | `Src/RCommon.EfCore/Sagas/` | Scoped |
| Dapper | `DapperSagaStore<TState, TKey>` | `Src/RCommon.Dapper/Sagas/` | Scoped |
| Linq2Db | `Linq2DbSagaStore<TState, TKey>` | `Src/RCommon.Linq2Db/Sagas/` | Scoped |
| In-Memory | `InMemorySagaStore<TState, TKey>` | `Src/RCommon.Persistence/Sagas/` | Scoped |

**Lifetime rationale:** `ISagaStore` is registered as **Scoped** (not Transient like `IAggregateRepository`) because saga state stores may hold DbContext or connection references that should be scoped per request. `IAggregateRepository` is Transient because it follows the existing repository pattern and creates its own DbContext via `IDataStoreFactory`.

### 4E. Choreography Pattern

Choreography requires **no new infrastructure**. It uses the existing event system:

1. **Event handlers** (`ISubscriber<TEvent>`) react to domain events
2. **Each handler** performs its step and raises new domain events via `IEventProducer`
3. **No central coordinator** — the workflow emerges from the chain of event → handler → event

The pattern is supported via:
- Existing `ISubscriber<TEvent>` for step handlers
- Existing `IEventProducer` for publishing follow-up events
- Existing `ISyncEvent`/`IAsyncEvent` markers for dispatch strategy

### 4F. DI Registration

```csharp
// Core (default in-memory store for development/testing)
services.AddScoped(typeof(ISagaStore<,>), typeof(InMemorySagaStore<,>));

// EFCore builder (overrides in-memory)
services.AddScoped(typeof(ISagaStore<,>), typeof(EFCoreSagaStore<,>));

// State machine adapter (separate package, e.g. RCommon.Stateless)
services.AddTransient(typeof(IStateMachineConfigurator<,>), typeof(StatelessConfigurator<,>));
```

### Consumer Usage (Orchestration)

```csharp
public enum OrderSagaStep { Pending, PaymentProcessed, Shipped, Completed, Compensating }
public enum OrderSagaTrigger { PaymentReceived, ShipmentConfirmed, DeliveryConfirmed, Failure }

public class OrderSagaData : SagaState<Guid>
{
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
}

public class OrderSaga
    : SagaOrchestrator<OrderSagaData, Guid, OrderSagaStep, OrderSagaTrigger>,
      ISubscriber<PaymentProcessedEvent>,
      ISubscriber<ShipmentConfirmedEvent>
{
    protected override OrderSagaStep InitialState => OrderSagaStep.Pending;

    public OrderSaga(
        ISagaStore<OrderSagaData, Guid> store,
        IStateMachineConfigurator<OrderSagaStep, OrderSagaTrigger> configurator)
        : base(store, configurator) { }

    protected override void ConfigureStateMachine(
        IStateMachineConfigurator<OrderSagaStep, OrderSagaTrigger> config)
    {
        config.ForState(OrderSagaStep.Pending)
              .Permit(OrderSagaTrigger.PaymentReceived, OrderSagaStep.PaymentProcessed);
        config.ForState(OrderSagaStep.PaymentProcessed)
              .Permit(OrderSagaTrigger.ShipmentConfirmed, OrderSagaStep.Shipped)
              .Permit(OrderSagaTrigger.Failure, OrderSagaStep.Compensating);
        config.ForState(OrderSagaStep.Shipped)
              .Permit(OrderSagaTrigger.DeliveryConfirmed, OrderSagaStep.Completed);
    }

    protected override OrderSagaTrigger MapEventToTrigger<TEvent>(TEvent @event) => @event switch
    {
        PaymentProcessedEvent => OrderSagaTrigger.PaymentReceived,
        ShipmentConfirmedEvent => OrderSagaTrigger.ShipmentConfirmed,
        _ => throw new InvalidOperationException($"Unmapped event: {typeof(TEvent).Name}")
    };

    public override async Task CompensateAsync(OrderSagaData state, CancellationToken ct)
    {
        // Reverse payment, cancel shipment, etc.
    }

    // ISubscriber implementations delegate to HandleAsync
    public async Task HandleAsync(PaymentProcessedEvent @event, CancellationToken ct)
    {
        var state = await Store.FindByCorrelationIdAsync(@event.OrderId.ToString(), ct);
        if (state != null)
            await HandleAsync(@event, state, ct);
    }

    public async Task HandleAsync(ShipmentConfirmedEvent @event, CancellationToken ct)
    {
        var state = await Store.FindByCorrelationIdAsync(@event.OrderId.ToString(), ct);
        if (state != null)
            await HandleAsync(@event, state, ct);
    }
}
```

### Known Trade-offs

- **State machine abstraction overhead:** Adds indirection between saga logic and state machine library. Justified because RCommon targets multiple hosting scenarios (in-process monolith, microservices with MassTransit, etc.).
- **ISagaStore vs IAggregateRepository:** Sagas use a dedicated store rather than the aggregate repository because saga state has different lifecycle semantics (correlation ID lookup, no domain events, compensation tracking).
- **Choreography is "just events":** Intentionally minimal — the existing event infrastructure is sufficient. Patterns are documented rather than building framework code.
- **Concrete state machine adapters are separate packages:** The core library defines only interfaces. Adapters for Stateless, MassTransit, etc. are separate NuGet packages to avoid forcing a dependency.
- **`CurrentStep` as string:** Stored as `string` in `SagaState` for database serialization. The `SagaOrchestrator` handles `Enum.Parse`/`ToString` conversion. A future enhancement could provide a generic helper for type-safe access.

---

## Testing Strategy

### Part 1: Aggregate Repository Tests

**Location:** One test class per ORM test project, plus interface constraint tests in `RCommon.Persistence.Tests`.

1. **Interface constraint tests** (RCommon.Persistence.Tests)
   - Verify `IAggregateRepository<TAggregate, TKey>` constrains `TAggregate` to `IAggregateRoot<TKey>` via reflection
   - Verify `DomainEntity<TKey>` cannot satisfy the constraint

2. **EFCore implementation tests** (RCommon.EfCore.Tests)
   - `GetByIdAsync` returns entity from DbSet
   - `FindAsync` applies specification predicate
   - `ExistsAsync` returns true/false correctly
   - `AddAsync/UpdateAsync/DeleteAsync` modify DbSet and call EventTracker
   - `Include/ThenInclude` chain builds correct IQueryable

3. **Dapper implementation tests** (RCommon.Dapper.Tests)
   - Same CRUD operation tests via mocked IDbConnection
   - Include/ThenInclude are no-ops (return same instance)

4. **Linq2Db implementation tests** (RCommon.Linq2Db.Tests)
   - Same CRUD operation tests via mocked DataConnection

5. **Builder registration tests** (per ORM test project)
   - Verify `IAggregateRepository<,>` is registered as transient in service collection

### Part 2: Domain Event Dispatch Tests

1. **UnitOfWork integration tests**
   - `CommitAsync` dispatches events via `IEntityEventTracker.EmitTransactionalEventsAsync()`
   - `CommitAsync` does not dispatch when no tracker is injected
   - Events are not dispatched if `TransactionScope.Complete()` throws
   - Events dispatch AFTER `TransactionScope.Dispose()` (verified via mock ordering)
   - `EmitTransactionalEventsAsync` returning `false` logs warning but does not throw
   - Backward-compatible `Commit()` still works (no event dispatch)

2. **UnitOfWorkBehavior tests** (RCommon.Mediatr.Tests)
   - `UnitOfWorkRequestBehavior` calls `CommitAsync` instead of `Commit`
   - Events are dispatched when using MediatR pipeline

3. **End-to-end event flow tests**
   - Aggregate raises domain event → repository saves → UoW commits → subscriber receives event

### Part 3: Read-Model Repository Tests

1. **Interface constraint tests**
   - Verify `IReadModelRepository<T>` constrains `T` to `IReadModel`

2. **Implementation tests per ORM**
   - `FindAsync` applies specification
   - `FindAllAsync` returns collection
   - `GetPagedAsync` returns correct `IPagedResult` with pagination metadata
   - `GetCountAsync` returns correct count
   - `AnyAsync` returns true/false correctly

3. **PagedResult unit tests**
   - Verify `TotalPages`, `HasNextPage`, `HasPreviousPage` calculations
   - Verify `PageSize <= 0` throws `ArgumentOutOfRangeException`

### Part 4: Saga Tests

1. **SagaState tests**
   - Lifecycle state transitions (Pending → Active → Completed)
   - Fault tracking (IsFaulted, FaultReason)
   - Concurrency version increment

2. **SagaOrchestrator tests**
   - State machine configuration is applied exactly once (lazy init)
   - Event triggers correct state transition
   - Invalid triggers are ignored (no exception)
   - State is persisted after each transition
   - Null/empty `CurrentStep` uses `InitialState`
   - Compensation is callable

3. **ISagaStore tests per ORM**
   - `FindByCorrelationIdAsync` returns correct saga
   - `SaveAsync` persists state changes
   - Concurrent save with stale version fails (optimistic concurrency)

4. **State machine abstraction tests**
   - `IStateMachine.FireAsync` transitions state
   - `CanFire` returns correct permissions
   - `PermittedTriggers` reflects current state
   - Guard conditions prevent invalid transitions

---

## File Summary

| File | Action | Location |
|------|--------|----------|
| **Part 1: Aggregate Repository** | | |
| `IAggregateRepository.cs` | Create | `Src/RCommon.Persistence/Crud/` |
| `EFCoreAggregateRepository.cs` | Create | `Src/RCommon.EfCore/Crud/` |
| `DapperAggregateRepository.cs` | Create | `Src/RCommon.Dapper/Crud/` |
| `Linq2DbAggregateRepository.cs` | Create | `Src/RCommon.Linq2Db/Crud/` |
| `EFCorePerisistenceBuilder.cs` | Modify | `Src/RCommon.EfCore/` |
| `DapperPersistenceBuilder.cs` | Modify | `Src/RCommon.Dapper/` |
| `Linq2DbPersistenceBuilder.cs` | Modify | `Src/RCommon.Linq2Db/` |
| **Part 2: Domain Event Dispatch** | | |
| `IUnitOfWork.cs` | Modify | `Src/RCommon.Persistence/Transactions/` |
| `UnitOfWork.cs` | Modify | `Src/RCommon.Persistence/Transactions/` |
| `UnitOfWorkBehavior.cs` | Modify | `Src/RCommon.Mediatr/Behaviors/` |
| **Part 3: Read-Model Repositories** | | |
| `IReadModel.cs` | Create | `Src/RCommon.Persistence/` |
| `IReadModelRepository.cs` | Create | `Src/RCommon.Persistence/Crud/` |
| `IPagedResult.cs` | Create | `Src/RCommon.Models/` |
| `PagedResult.cs` | Create | `Src/RCommon.Models/` |
| `EFCoreReadModelRepository.cs` | Create | `Src/RCommon.EfCore/Crud/` |
| `DapperReadModelRepository.cs` | Create | `Src/RCommon.Dapper/Crud/` |
| `Linq2DbReadModelRepository.cs` | Create | `Src/RCommon.Linq2Db/Crud/` |
| **Part 4: State Machines (RCommon.Core)** | | |
| `IStateMachine.cs` | Create | `Src/RCommon.Core/StateMachines/` |
| `IStateMachineConfigurator.cs` | Create | `Src/RCommon.Core/StateMachines/` |
| `IStateConfigurator.cs` | Create | `Src/RCommon.Core/StateMachines/` |
| **Part 4: Sagas (RCommon.Persistence)** | | |
| `SagaState.cs` | Create | `Src/RCommon.Persistence/Sagas/` |
| `ISaga.cs` | Create | `Src/RCommon.Persistence/Sagas/` |
| `SagaOrchestrator.cs` | Create | `Src/RCommon.Persistence/Sagas/` |
| `ISagaStore.cs` | Create | `Src/RCommon.Persistence/Sagas/` |
| `InMemorySagaStore.cs` | Create | `Src/RCommon.Persistence/Sagas/` |
| `EFCoreSagaStore.cs` | Create | `Src/RCommon.EfCore/Sagas/` |
| `DapperSagaStore.cs` | Create | `Src/RCommon.Dapper/Sagas/` |
| `Linq2DbSagaStore.cs` | Create | `Src/RCommon.Linq2Db/Sagas/` |
| Test files | Create | Per project |
