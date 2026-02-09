# RCommon.Caching

Provides the core caching abstractions for RCommon, including the `ICacheService` interface, strongly-typed cache keys, and builder contracts for plugging in memory or distributed caching providers.

## Features

- `ICacheService` interface with generic `GetOrCreate` and `GetOrCreateAsync` methods (read-through / get-or-create pattern)
- `CacheKey` value type with validation, max-length enforcement (256 chars), and factory methods for composite and type-scoped keys
- `IMemoryCachingBuilder` and `IDistributedCachingBuilder` contracts for provider-agnostic DI configuration
- `WithMemoryCaching<T>` and `WithDistributedCaching<T>` extension methods on `IRCommonBuilder` for fluent setup
- `ExpressionCachingStrategy` enum for strategy-based resolution of cache services used to cache dynamically compiled expressions

## Installation

```shell
dotnet add package RCommon.Caching
```

## Usage

This package is typically consumed indirectly through a concrete provider such as `RCommon.MemoryCache` or `RCommon.RedisCache`. You can also program against the abstraction directly:

```csharp
// Inject ICacheService and use the get-or-create pattern
public class ProductService
{
    private readonly ICacheService _cache;

    public ProductService(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<Product> GetProductAsync(int id)
    {
        return await _cache.GetOrCreateAsync(
            CacheKey.With("product", id.ToString()),
            () => _productRepository.FindAsync(id));
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `ICacheService` | Core abstraction providing `GetOrCreate` and `GetOrCreateAsync` for read-through caching |
| `CacheKey` | Strongly-typed cache key with validation, max-length enforcement, and static factory methods |
| `IMemoryCachingBuilder` | Builder contract for configuring in-memory caching providers |
| `IDistributedCachingBuilder` | Builder contract for configuring distributed caching providers |
| `ExpressionCachingStrategy` | Strategy enum used to resolve the appropriate `ICacheService` for expression caching |
| `CachingBuilderExtensions` | `WithMemoryCaching<T>` and `WithDistributedCaching<T>` extensions on `IRCommonBuilder` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.MemoryCache](https://www.nuget.org/packages/RCommon.MemoryCache) - In-process and distributed memory cache implementations of `ICacheService`
- [RCommon.RedisCache](https://www.nuget.org/packages/RCommon.RedisCache) - Redis-backed distributed cache implementation of `ICacheService`
- [RCommon.Persistence.Caching](https://www.nuget.org/packages/RCommon.Persistence.Caching) - Caching decorator repositories that layer caching over any persistence provider

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
