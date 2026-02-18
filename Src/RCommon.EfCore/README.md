# RCommon.EFCore

Entity Framework Core implementation of the RCommon persistence abstractions. Provides a fully-featured repository with LINQ queries, eager loading, change tracking, and automatic domain event integration -- all backed by EF Core's `DbContext`.

## Features

- `EFCoreRepository<TEntity>` implementing `IGraphRepository<T>`, `ILinqRepository<T>`, `IReadOnlyRepository<T>`, and `IWriteOnlyRepository<T>`
- Full `IQueryable<T>` support for composable LINQ queries at the domain layer
- Eager loading via `Include` / `ThenInclude` mapped to EF Core's `IIncludableQueryable`
- Configurable change tracking (enable/disable per repository via the `Tracking` property)
- Paginated query results with ordering support
- Bulk delete via `ExecuteDeleteAsync` for expression-based batch operations
- Named data store support for multi-database scenarios through `IDataStoreFactory`
- `RCommonDbContext` base class implementing `IDataStore` for seamless factory resolution
- Fluent DI configuration to register DbContexts as named data stores
- Automatic entity event tracking for domain event dispatching on add, update, and delete
- **Soft delete** -- entities implementing `ISoftDelete` are automatically filtered on reads and logically deleted on writes
- **Multitenancy** -- entities implementing `IMultiTenant` are automatically filtered by tenant on reads and stamped with `TenantId` on writes
- Targets .NET 8, .NET 9, and .NET 10

## Installation

```shell
dotnet add package RCommon.EFCore
```

## Usage

```csharp
// Configure in Program.cs or Startup
builder.Services.AddRCommon()
    .WithPersistence<EFCorePerisistenceBuilder>(ef =>
    {
        ef.AddDbContext<ApplicationDbContext>("ApplicationDb", options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDb")));

        ef.SetDefaultDataStore(defaults =>
            defaults.DefaultDataStoreName = "ApplicationDb");
    });
```

Your `DbContext` must inherit from `RCommonDbContext`:

```csharp
public class ApplicationDbContext : RCommonDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();
}
```

Then inject and use the repository abstractions:

```csharp
public class OrderService
{
    private readonly IGraphRepository<Order> _orderRepo;

    public OrderService(IGraphRepository<Order> orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public async Task<ICollection<Order>> GetCustomerOrdersAsync(int customerId)
    {
        _orderRepo.Include(o => o.LineItems);
        return await _orderRepo.FindAsync(o => o.CustomerId == customerId);
    }
}
```

### Soft Delete and Multitenancy

`EFCoreRepository<TEntity>` automatically supports soft delete and multitenancy when your entities implement the opt-in interfaces:

```csharp
using RCommon.Entities;

public class Customer : BusinessEntity<int>, ISoftDelete, IMultiTenant
{
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public string? TenantId { get; set; }
}
```

Reads automatically exclude soft-deleted records and scope to the current tenant:

```csharp
// Both filters applied transparently — only active customers for the current tenant
var customers = await _customerRepo.FindAsync(c => c.Name.StartsWith("A"));
```

Writes automatically stamp the tenant and support logical deletion:

```csharp
// TenantId stamped automatically from ITenantIdAccessor
await _customerRepo.AddAsync(new Customer { Name = "Acme" });

// Soft delete — sets IsDeleted = true, performs UPDATE via EF Core
await _customerRepo.DeleteAsync(customer, isSoftDelete: true);
```

## Key Types

| Type | Description |
|------|-------------|
| `EFCoreRepository<TEntity>` | Concrete repository backed by EF Core with full CRUD, LINQ, eager loading, and change tracking |
| `RCommonDbContext` | Abstract `DbContext` base class implementing `IDataStore` for named data store resolution |
| `EFCorePerisistenceBuilder` | Fluent builder for registering EF Core DbContexts and repository services in DI |
| `IEFCorePersistenceBuilder` | Builder interface exposing `AddDbContext<T>()` for registering named DbContexts |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Persistence](https://www.nuget.org/packages/RCommon.Persistence) - Core persistence abstractions (required dependency)
- [RCommon.Dapper](https://www.nuget.org/packages/RCommon.Dapper) - Dapper implementation
- [RCommon.Linq2Db](https://www.nuget.org/packages/RCommon.Linq2Db) - Linq2Db implementation

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
