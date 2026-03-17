# Aggregate Repository — Design Specification

**Date:** 2026-03-17
**Branch:** feature/ddd
**Status:** Approved

## Problem

The existing repository interfaces (`ILinqRepository<T>`, `IGraphRepository<T>`, `ISqlMapperRepository<T>`) accept any `IBusinessEntity` type parameter. There is no compile-time enforcement that prevents persisting child entities (`DomainEntity<TKey>`) directly, bypassing the aggregate root boundary. This undermines the DDD aggregate pattern where all mutations should flow through the aggregate root.

## Goal

Add an `IAggregateRepository<TAggregate, TKey>` interface with a DDD-constrained API that:

1. **Compile-time enforcement** — Generic constraint `where TAggregate : class, IAggregateRoot<TKey>` prevents non-aggregate types from being persisted through this interface.
2. **Minimal API surface** — Only aggregate-appropriate operations: load by ID, find by specification, existence check, add, update, delete, and eager loading. No `GetCountAsync`, `AnyAsync`, `IQueryable`, or paginated queries (those are read-model concerns).
3. **Open-generic registration** — Follows the existing pattern where one registration handles all aggregate types automatically (e.g., `services.AddTransient(typeof(IAggregateRepository<,>), typeof(EFCoreAggregateRepository<,>))`).
4. **Non-breaking** — Existing `IGraphRepository<T>`, `ILinqRepository<T>`, and `ISqlMapperRepository<T>` remain unchanged and fully functional for non-DDD usage.

## Non-Goals

- Event sourcing integration (prepared for via `AggregateRoot.Version`, but not implemented here)
- Automatic domain event dispatch on repository save (future work with UnitOfWork integration)
- Read-model/query-side repositories (separate concern)
- Saga or process manager patterns

## Design

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
- **Include/ThenInclude** — Fluent chaining for eager loading child entities within the aggregate boundary. Returns `IAggregateRepository` for chaining.
- **`INamedDataSource` inheritance** — Exposes `DataStoreName` property for multi-database targeting, consistent with all existing repository interfaces.
- **All methods have `CancellationToken`** — Consistent with the async hardening work done in prior commits.
- **Immediate save semantics** — `AddAsync`/`UpdateAsync`/`DeleteAsync` call `SaveChangesAsync` immediately, matching the existing repository behavior. Future UnitOfWork integration may defer persistence, but that is out of scope for this spec.

### Known Trade-offs

- **Base class API surface leak:** The concrete implementations inherit from ORM base classes (e.g., `GraphRepositoryBase<TAggregate>`), which means the concrete type also implements `IGraphRepository<TAggregate>`. However, the DI registration only maps `IAggregateRepository<,>`, so normal injection is safe. Runtime casting from `IAggregateRepository` to `IGraphRepository` would succeed but is the consumer's responsibility to avoid. This is an acceptable trade-off for infrastructure reuse.

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
- `Include/ThenInclude` → builds `IQueryable<TAggregate>` using EF Core's `EntityFrameworkQueryableExtensions.Include/ThenInclude`. The `Include` method on `IAggregateRepository` is an explicit interface implementation returning `IAggregateRepository`; the inherited base class `Include` (returning `IEagerLoadableQueryable`) is also implemented for internal use.

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

**EFCorePerisistenceBuilder:**
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

## Testing Strategy

### Unit Tests

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

## File Summary

| File | Action | Location |
|------|--------|----------|
| `IAggregateRepository.cs` | Create | `Src/RCommon.Persistence/Crud/` |
| `EFCoreAggregateRepository.cs` | Create | `Src/RCommon.EfCore/Crud/` |
| `DapperAggregateRepository.cs` | Create | `Src/RCommon.Dapper/Crud/` |
| `Linq2DbAggregateRepository.cs` | Create | `Src/RCommon.Linq2Db/Crud/` |
| `EFCorePerisistenceBuilder.cs` | Modify | `Src/RCommon.EfCore/` |
| `DapperPersistenceBuilder.cs` | Modify | `Src/RCommon.Dapper/` |
| `Linq2DbPersistenceBuilder.cs` | Modify | `Src/RCommon.Linq2Db/` |
| Test files | Create | Per ORM test project |
