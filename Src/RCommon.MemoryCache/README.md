# RCommon.MemoryCache

Provides two in-process memory caching implementations of `ICacheService`: one backed by `IMemoryCache` and another backed by `IDistributedCache` (in-memory distributed cache), with fluent builder extensions for DI configuration.

## Features

- `InMemoryCacheService` -- delegates to Microsoft's `IMemoryCache` for fast in-process caching with `GetOrCreate`/`GetOrCreateAsync`
- `DistributedMemoryCacheService` -- delegates to `IDistributedCache` (in-memory distributed store) with automatic JSON serialization via `IJsonSerializer`
- `InMemoryCachingBuilder` and `DistributedMemoryCacheBuilder` for plugging into the `AddRCommon()` builder pipeline
- `Configure()` extension to customize `MemoryCacheOptions` or `MemoryDistributedCacheOptions`
- `CacheDynamicallyCompiledExpressions()` extension to enable expression caching, which improves performance in areas of RCommon that compile expressions and lambdas at runtime

## Installation

```shell
dotnet add package RCommon.MemoryCache
```

## Usage

```csharp
using RCommon;
using RCommon.MemoryCache;

services.AddRCommon(builder =>
{
    // Option 1: In-process IMemoryCache
    builder.WithMemoryCaching<InMemoryCachingBuilder>(cache =>
    {
        cache.Configure(options => options.SizeLimit = 1024);
        cache.CacheDynamicallyCompiledExpressions();
    });

    // Option 2: Distributed memory cache (IDistributedCache backed by memory)
    builder.WithDistributedCaching<DistributedMemoryCacheBuilder>(cache =>
    {
        cache.Configure(options => options.SizeLimit = 2048);
        cache.CacheDynamicallyCompiledExpressions();
    });
});
```

## Key Types

| Type | Description |
|------|-------------|
| `InMemoryCacheService` | `ICacheService` implementation backed by `IMemoryCache` |
| `DistributedMemoryCacheService` | `ICacheService` implementation backed by `IDistributedCache` with JSON serialization |
| `InMemoryCachingBuilder` | Concrete builder for configuring in-process memory caching |
| `DistributedMemoryCacheBuilder` | Concrete builder for configuring distributed memory caching |
| `IInMemoryCachingBuilder` | Builder interface extending `IMemoryCachingBuilder` |
| `IDistributedMemoryCachingBuilder` | Builder interface extending `IDistributedCachingBuilder` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Caching](https://www.nuget.org/packages/RCommon.Caching) - Core caching abstractions (`ICacheService`, `CacheKey`, builder contracts)
- [RCommon.RedisCache](https://www.nuget.org/packages/RCommon.RedisCache) - Redis-backed distributed cache implementation
- [RCommon.Persistence.Caching.MemoryCache](https://www.nuget.org/packages/RCommon.Persistence.Caching.MemoryCache) - Wires memory caching into the persistence caching repository decorators

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
