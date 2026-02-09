# RCommon.Persistence.Caching.MemoryCache

Wires memory-based caching into RCommon's persistence caching decorators, providing `AddInMemoryPersistenceCaching()` and `AddDistributedMemoryPersistenceCaching()` extension methods that register the appropriate `ICacheService` implementation and all caching repository decorators.

## Features

- `AddInMemoryPersistenceCaching()` -- registers `InMemoryCacheService` (backed by `IMemoryCache`) as the cache provider for persistence caching decorators
- `AddDistributedMemoryPersistenceCaching()` -- registers `DistributedMemoryCacheService` (backed by `IDistributedCache` in-memory store) as the cache provider
- Automatically registers all open-generic caching repository decorators (`CachingGraphRepository<>`, `CachingLinqRepository<>`, `CachingSqlMapperRepository<>`)
- Configures `CachingOptions` with caching enabled by default
- Strategy-based factory resolves the correct `ICacheService` via `PersistenceCachingStrategy`

## Installation

```shell
dotnet add package RCommon.Persistence.Caching.MemoryCache
```

## Usage

```csharp
using RCommon;
using RCommon.Persistence.Caching.MemoryCache;

services.AddRCommon(builder =>
{
    builder.WithPersistence<EfCorePeristenceBuilder>(persistence =>
    {
        // Option 1: In-process IMemoryCache for persistence caching
        persistence.AddInMemoryPersistenceCaching();

        // Option 2: Distributed memory cache for persistence caching
        persistence.AddDistributedMemoryPersistenceCaching();
    });
});
```

## Key Types

| Type | Description |
|------|-------------|
| `IPersistenceBuilderExtensions` | Provides `AddInMemoryPersistenceCaching()` and `AddDistributedMemoryPersistenceCaching()` on `IPersistenceBuilder` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Persistence.Caching](https://www.nuget.org/packages/RCommon.Persistence.Caching) - Core persistence caching decorators and abstractions
- [RCommon.MemoryCache](https://www.nuget.org/packages/RCommon.MemoryCache) - Underlying `InMemoryCacheService` and `DistributedMemoryCacheService` implementations
- [RCommon.Persistence.Caching.RedisCache](https://www.nuget.org/packages/RCommon.Persistence.Caching.RedisCache) - Redis-backed alternative for persistence caching

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
