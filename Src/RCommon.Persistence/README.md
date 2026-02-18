# RCommon.Persistence

Persistence abstraction layer for RCommon providing the repository pattern, unit of work, specification pattern, and named data store management. This package defines the core interfaces and base classes that ORM-specific implementations (EF Core, Dapper, Linq2Db) build upon.

## Features

- Repository pattern with separate read-only and write-only interfaces for CQRS-friendly designs
- LINQ-enabled repositories exposing `IQueryable<T>` for composable queries
- Graph repositories with change tracking support for ORMs like Entity Framework Core
- SQL mapper repositories for micro-ORMs like Dapper
- Specification pattern support for encapsulating query logic
- Paginated query results via `IPaginatedList<T>` with built-in ordering
- Eager loading abstraction with `Include` / `ThenInclude` chaining
- Unit of work pattern with configurable transaction modes and isolation levels
- Named data store factory for managing multiple database connections
- Domain event tracking integrated into repository operations
- **Soft delete** -- automatic `!IsDeleted` filtering on reads and logical deletion on writes for entities implementing `ISoftDelete`
- **Multitenancy** -- automatic tenant filtering on reads and `TenantId` stamping on writes for entities implementing `IMultiTenant`
- Fluent builder API for DI registration via `AddRCommon()`

## Installation

```shell
dotnet add package RCommon.Persistence
```

## Usage

This package is typically used indirectly through a provider-specific package. However, you program against these abstractions in your application and domain layers:

```csharp
// Inject repository abstractions into your services
public class OrderService
{
    private readonly IGraphRepository<Order> _orderRepo;
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public OrderService(IGraphRepository<Order> orderRepo, IUnitOfWorkFactory unitOfWorkFactory)
    {
        _orderRepo = orderRepo;
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<Order> GetOrderAsync(int id)
    {
        return await _orderRepo.FindAsync(id);
    }

    public async Task<ICollection<Order>> GetPendingOrdersAsync()
    {
        return await _orderRepo.FindAsync(o => o.Status == OrderStatus.Pending);
    }

    public async Task<IPaginatedList<Order>> GetOrdersPagedAsync(int page, int pageSize)
    {
        return await _orderRepo.FindAsync(
            o => o.IsActive,
            o => o.CreatedDate,
            orderByAscending: false,
            pageNumber: page,
            pageSize: pageSize);
    }

    public async Task PlaceOrderAsync(Order order)
    {
        using var unitOfWork = _unitOfWorkFactory.Create(TransactionMode.Default);
        await _orderRepo.AddAsync(order);
        unitOfWork.Commit();
    }
}
```

### Soft Delete

Entities implementing `ISoftDelete` get automatic repository behavior:
- **Reads**: A `!IsDeleted` filter is combined with your query expression automatically
- **Writes**: `DeleteAsync(entity, isSoftDelete: true)` sets `IsDeleted = true` and performs an UPDATE instead of a DELETE

```csharp
using RCommon.Entities;

public class Customer : BusinessEntity<int>, ISoftDelete
{
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
}

// Queries automatically exclude soft-deleted records
var activeCustomers = await repo.FindAsync(c => c.Name.StartsWith("A"));

// Soft delete
await repo.DeleteAsync(customer, isSoftDelete: true);

// Physical delete
await repo.DeleteAsync(customer);
```

### Multitenancy

Entities implementing `IMultiTenant` get automatic repository behavior:
- **Reads**: A `TenantId == currentTenantId` filter is combined with your query expression automatically
- **Writes**: `TenantId` is stamped on the entity during `AddAsync` using the current `ITenantIdAccessor`

```csharp
using RCommon.Entities;

public class Product : BusinessEntity<int>, IMultiTenant
{
    public string Name { get; set; }
    public string? TenantId { get; set; }
}

// Queries automatically scoped to the current tenant
var products = await repo.FindAsync(p => p.Name.Contains("Widget"));

// TenantId is stamped automatically on add
await repo.AddAsync(new Product { Name = "Widget" });
```

When no `ITenantIdAccessor` is configured (or it returns `null`), tenant filtering is bypassed entirely.

## Key Types

| Type | Description |
|------|-------------|
| `IReadOnlyRepository<TEntity>` | Async read operations: find by key, expression, specification, count, and any |
| `IWriteOnlyRepository<TEntity>` | Async write operations: add, add range, update, delete, and delete many |
| `ILinqRepository<TEntity>` | Combines read/write with `IQueryable<T>` support, pagination, and eager loading |
| `IGraphRepository<TEntity>` | Extends `ILinqRepository<T>` with change tracking control for full ORMs |
| `ISqlMapperRepository<TEntity>` | Read/write repository for micro-ORMs with explicit table name mapping |
| `IUnitOfWork` | Transaction scope that commits or rolls back on dispose |
| `IUnitOfWorkFactory` | Creates `IUnitOfWork` instances with configurable transaction mode and isolation level |
| `IDataStoreFactory` | Resolves named data store instances (DbContext, DbConnection, etc.) |
| `IPersistenceBuilder` | Fluent builder interface for registering persistence providers in DI |
| `LinqRepositoryBase<TEntity>` | Abstract base class for LINQ-enabled repository implementations |
| `SqlRepositoryBase<TEntity>` | Abstract base class for SQL mapper repository implementations |
| `SoftDeleteHelper` | Utility for validating `ISoftDelete` support, marking entities deleted, and building `!IsDeleted` filter expressions |
| `MultiTenantHelper` | Utility for validating `IMultiTenant` support, stamping `TenantId`, and building tenant filter expressions |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.EFCore](https://www.nuget.org/packages/RCommon.EFCore) - Entity Framework Core implementation
- [RCommon.Dapper](https://www.nuget.org/packages/RCommon.Dapper) - Dapper implementation
- [RCommon.Linq2Db](https://www.nuget.org/packages/RCommon.Linq2Db) - Linq2Db implementation

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
