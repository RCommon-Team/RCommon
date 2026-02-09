# RCommon.Linq2Db

Linq2Db implementation of the RCommon persistence abstractions. Provides a LINQ-enabled repository backed by Linq2Db's `DataConnection`, supporting composable queries, eager loading, pagination, and integration with the RCommon data store factory and domain event tracking.

## Features

- `Linq2DbRepository<TEntity>` implementing `ILinqRepository<T>`, `IReadOnlyRepository<T>`, and `IWriteOnlyRepository<T>`
- Full `IQueryable<T>` support built on Linq2Db's `ITable<T>` for composable LINQ queries
- Eager loading via `Include` / `ThenInclude` mapped to Linq2Db's `LoadWith` / `ThenLoad` API
- Paginated query results with ordering support via `IPaginatedList<T>`
- Expression-based and specification-based querying with `FindQuery` returning `IQueryable<T>`
- Bulk delete via Linq2Db's `DeleteAsync` on queryable expressions
- Named data store support for multi-database scenarios through `IDataStoreFactory`
- `RCommonDataConnection` base class implementing `IDataStore` for seamless factory resolution
- Fluent DI configuration using `AddLinqToDBContext` under the hood
- Domain event tracking integrated into add, update, and delete operations
- Targets .NET 8, .NET 9, and .NET 10

## Installation

```shell
dotnet add package RCommon.Linq2Db
```

## Usage

```csharp
// Configure in Program.cs or Startup
builder.Services.AddRCommon()
    .WithPersistence<Linq2DbPersistenceBuilder>(linq2Db =>
    {
        linq2Db.AddDataConnection<ApplicationDataConnection>("ApplicationDb",
            (serviceProvider, options) =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("ApplicationDb")));

        linq2Db.SetDefaultDataStore(defaults =>
            defaults.DefaultDataStoreName = "ApplicationDb");
    });
```

Your data connection must inherit from `RCommonDataConnection`:

```csharp
public class ApplicationDataConnection : RCommonDataConnection
{
    public ApplicationDataConnection(DataOptions options)
        : base(options) { }
}
```

Then inject and use the repository abstractions:

```csharp
public class CustomerService
{
    private readonly ILinqRepository<Customer> _customerRepo;

    public CustomerService(ILinqRepository<Customer> customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<ICollection<Customer>> GetActiveCustomersAsync()
    {
        return await _customerRepo.FindAsync(c => c.IsActive);
    }

    public async Task<IPaginatedList<Customer>> GetCustomersPagedAsync(int page, int pageSize)
    {
        return await _customerRepo.FindAsync(
            c => c.IsActive,
            c => c.LastName,
            orderByAscending: true,
            pageNumber: page,
            pageSize: pageSize);
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `Linq2DbRepository<TEntity>` | Concrete repository using Linq2Db with full LINQ, eager loading, and CRUD support |
| `RCommonDataConnection` | Base `DataConnection` class implementing `IDataStore` for named data store resolution |
| `Linq2DbPersistenceBuilder` | Fluent builder for registering Linq2Db data connections and repository services in DI |
| `ILinq2DbPersistenceBuilder` | Builder interface exposing `AddDataConnection<T>()` for registering named data connections |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Persistence](https://www.nuget.org/packages/RCommon.Persistence) - Core persistence abstractions (required dependency)
- [RCommon.EFCore](https://www.nuget.org/packages/RCommon.EFCore) - Entity Framework Core implementation
- [RCommon.Dapper](https://www.nuget.org/packages/RCommon.Dapper) - Dapper implementation

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
