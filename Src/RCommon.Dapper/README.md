# RCommon.Dapper

Dapper implementation of the RCommon persistence abstractions. Provides a lightweight SQL mapper repository using Dapper and Dommel for CRUD operations with expression-based querying, while integrating with the RCommon data store factory and domain event tracking.

## Features

- `DapperRepository<TEntity>` implementing `ISqlMapperRepository<T>`, `IReadOnlyRepository<T>`, and `IWriteOnlyRepository<T>`
- Expression-based querying via Dommel's `SelectAsync`, `CountAsync`, and `AnyAsync` extension methods
- CRUD operations mapped to SQL using Dommel's `InsertAsync`, `UpdateAsync`, and `DeleteAsync`
- Bulk delete support via `DeleteMultipleAsync` with expression predicates
- Find by primary key using Dommel's `GetAsync`
- Automatic connection lifecycle management (open/close per operation)
- Named data store support for multi-database scenarios through `IDataStoreFactory` and `RDbConnection`
- Fluent DI configuration to register database connections as named data stores
- Domain event tracking integrated into add, update, and delete operations
- Targets .NET 8, .NET 9, and .NET 10

## Installation

```shell
dotnet add package RCommon.Dapper
```

## Usage

```csharp
// Configure in Program.cs or Startup
builder.Services.AddRCommon()
    .WithPersistence<DapperPersistenceBuilder>(dapper =>
    {
        dapper.AddDbConnection<ApplicationDbConnection>("ApplicationDb", options =>
        {
            options.DbFactory = SqlClientFactory.Instance;
            options.ConnectionString = builder.Configuration.GetConnectionString("ApplicationDb");
        });

        dapper.SetDefaultDataStore(defaults =>
            defaults.DefaultDataStoreName = "ApplicationDb");
    });
```

Your database connection must inherit from `RDbConnection`:

```csharp
public class ApplicationDbConnection : RDbConnection
{
    public ApplicationDbConnection(IOptions<RDbConnectionOptions> options)
        : base(options) { }
}
```

Then inject and use the repository abstractions:

```csharp
public class ProductService
{
    private readonly ISqlMapperRepository<Product> _productRepo;

    public ProductService(ISqlMapperRepository<Product> productRepo)
    {
        _productRepo = productRepo;
    }

    public async Task<ICollection<Product>> GetActiveProductsAsync()
    {
        return await _productRepo.FindAsync(p => p.IsActive);
    }

    public async Task<Product> GetByIdAsync(int id)
    {
        return await _productRepo.FindAsync(id);
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `DapperRepository<TEntity>` | Concrete repository using Dapper/Dommel with expression-based CRUD operations |
| `DapperPersistenceBuilder` | Fluent builder for registering Dapper database connections and repository services in DI |
| `IDapperBuilder` | Builder interface exposing `AddDbConnection<T>()` for registering named database connections |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Persistence](https://www.nuget.org/packages/RCommon.Persistence) - Core persistence abstractions (required dependency)
- [RCommon.EFCore](https://www.nuget.org/packages/RCommon.EFCore) - Entity Framework Core implementation
- [RCommon.Linq2Db](https://www.nuget.org/packages/RCommon.Linq2Db) - Linq2Db implementation

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
