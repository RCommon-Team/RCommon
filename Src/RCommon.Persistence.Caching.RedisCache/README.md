# RCommon.Persistence.Caching.RedisCache

Wires Redis-based caching into RCommon's persistence caching decorators, providing the `AddRedisPersistenceCaching()` extension method that registers `RedisCacheService` and all caching repository decorators.

## Features

- `AddRedisPersistenceCaching()` -- registers `RedisCacheService` (backed by StackExchange.Redis via `IDistributedCache`) as the cache provider for persistence caching decorators
- Automatically registers all open-generic caching repository decorators (`CachingGraphRepository<>`, `CachingLinqRepository<>`, `CachingSqlMapperRepository<>`)
- Configures `CachingOptions` with caching enabled by default
- Strategy-based factory resolves the correct `ICacheService` via `PersistenceCachingStrategy`

## Installation

```shell
dotnet add package RCommon.Persistence.Caching.RedisCache
```

## Usage

```csharp
using RCommon;
using RCommon.RedisCache;
using RCommon.Persistence.Caching.RedisCache;

services.AddRCommon(builder =>
{
    // Configure Redis connection
    builder.WithDistributedCaching<RedisCachingBuilder>(cache =>
    {
        cache.Configure(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "MyApp:";
        });
    });

    builder.WithPersistence<EfCorePeristenceBuilder>(persistence =>
    {
        // Use Redis as the cache provider for persistence caching
        persistence.AddRedisPersistenceCaching();
    });
});
```

## Key Types

| Type | Description |
|------|-------------|
| `IPersistenceBuilderExtensions` | Provides `AddRedisPersistenceCaching()` on `IPersistenceBuilder` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Persistence.Caching](https://www.nuget.org/packages/RCommon.Persistence.Caching) - Core persistence caching decorators and abstractions
- [RCommon.RedisCache](https://www.nuget.org/packages/RCommon.RedisCache) - Underlying `RedisCacheService` implementation
- [RCommon.Persistence.Caching.MemoryCache](https://www.nuget.org/packages/RCommon.Persistence.Caching.MemoryCache) - Memory-based alternative for persistence caching

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
