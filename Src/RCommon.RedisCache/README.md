# RCommon.RedisCache

Provides a Redis-backed distributed cache implementation of `ICacheService` using `IDistributedCache` from StackExchange.Redis, with fluent builder extensions for DI configuration.

## Features

- `RedisCacheService` -- implements `ICacheService` using `IDistributedCache` backed by StackExchange.Redis
- Automatic JSON serialization/deserialization of cached values via `IJsonSerializer`
- `RedisCachingBuilder` for plugging into the `AddRCommon()` builder pipeline via `WithDistributedCaching<T>`
- `Configure()` extension to customize `RedisCacheOptions` (connection string, instance name, etc.)
- `CacheDynamicallyCompiledExpressions()` extension to enable expression caching for improved runtime performance

## Installation

```shell
dotnet add package RCommon.RedisCache
```

## Usage

```csharp
using RCommon;
using RCommon.RedisCache;

services.AddRCommon(builder =>
{
    builder.WithDistributedCaching<RedisCachingBuilder>(cache =>
    {
        cache.Configure(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = "MyApp:";
        });
        cache.CacheDynamicallyCompiledExpressions();
    });
});
```

## Key Types

| Type | Description |
|------|-------------|
| `RedisCacheService` | `ICacheService` implementation backed by Redis via `IDistributedCache` with JSON serialization |
| `RedisCachingBuilder` | Concrete builder for configuring Redis distributed caching |
| `IRedisCachingBuilder` | Builder interface extending `IDistributedCachingBuilder` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Caching](https://www.nuget.org/packages/RCommon.Caching) - Core caching abstractions (`ICacheService`, `CacheKey`, builder contracts)
- [RCommon.MemoryCache](https://www.nuget.org/packages/RCommon.MemoryCache) - In-process and distributed memory cache implementations
- [RCommon.Persistence.Caching.RedisCache](https://www.nuget.org/packages/RCommon.Persistence.Caching.RedisCache) - Wires Redis caching into the persistence caching repository decorators

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
