# RCommon.Persistence.Caching

Provides caching decorator repositories that wrap any RCommon persistence provider with a cache layer, enabling cache-aware query overloads on `IGraphRepository`, `ILinqRepository`, and `ISqlMapperRepository`.

## Features

- `CachingGraphRepository<TEntity>` -- decorator around `IGraphRepository<TEntity>` that adds cache-aware `FindAsync` overloads accepting a cache key
- `CachingLinqRepository<TEntity>` -- decorator around `ILinqRepository<TEntity>` (via `IGraphRepository`) with the same cached query pattern
- `CachingSqlMapperRepository<TEntity>` -- decorator around `ISqlMapperRepository<TEntity>` with cached `FindAsync` overloads
- Non-cached operations (Add, Update, Delete, etc.) are delegated directly to the inner repository
- `PersistenceCachingStrategy` enum for strategy-based resolution of the `ICacheService` used by repository decorators
- `AddPersistenceCaching()` extension on `IPersistenceBuilder` to register all caching repository decorators with a custom cache factory

## Installation

```shell
dotnet add package RCommon.Persistence.Caching
```

## Usage

```csharp
using RCommon;
using RCommon.Persistence.Caching;
using RCommon.Persistence.Caching.Crud;

// Register persistence caching with a custom cache factory
services.AddRCommon(builder =>
{
    builder.WithPersistence<EfCorePeristenceBuilder>(persistence =>
    {
        persistence.AddPersistenceCaching(serviceProvider => strategy =>
        {
            return serviceProvider.GetRequiredService<ICacheService>();
        });
    });
});

// Use the caching repository in your service
public class ProductService
{
    private readonly ICachingGraphRepository<Product> _repo;

    public ProductService(ICachingGraphRepository<Product> repo)
    {
        _repo = repo;
    }

    public async Task<ICollection<Product>> GetActiveProductsAsync()
    {
        // Cached query -- checks cache first, falls through to the database on a miss
        return await _repo.FindAsync(
            CacheKey.With("active-products"),
            x => x.IsActive);
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `CachingGraphRepository<TEntity>` | Decorator adding cached `FindAsync` overloads to `IGraphRepository<TEntity>` |
| `CachingLinqRepository<TEntity>` | Decorator adding cached `FindAsync` overloads to `ILinqRepository<TEntity>` |
| `CachingSqlMapperRepository<TEntity>` | Decorator adding cached `FindAsync` overloads to `ISqlMapperRepository<TEntity>` |
| `ICachingGraphRepository<TEntity>` | Interface extending `IGraphRepository<TEntity>` with cache-key-based query methods |
| `ICachingLinqRepository<TEntity>` | Interface extending `ILinqRepository<TEntity>` with cache-key-based query methods |
| `ICachingSqlMapperRepository<TEntity>` | Interface extending `ISqlMapperRepository<TEntity>` with cache-key-based query methods |
| `PersistenceCachingStrategy` | Strategy enum for resolving the `ICacheService` used by caching repositories |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Caching](https://www.nuget.org/packages/RCommon.Caching) - Core caching abstractions (`ICacheService`, `CacheKey`)
- [RCommon.Persistence.Caching.MemoryCache](https://www.nuget.org/packages/RCommon.Persistence.Caching.MemoryCache) - Wires in-memory caching into persistence caching decorators
- [RCommon.Persistence.Caching.RedisCache](https://www.nuget.org/packages/RCommon.Persistence.Caching.RedisCache) - Wires Redis caching into persistence caching decorators

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
