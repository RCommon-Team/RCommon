# RCommon.MultiTenancy

Multitenancy builder abstraction for the RCommon framework. Provides the `WithMultiTenancy<T>()` fluent API for registering tenant resolution providers (such as Finbuckle) that supply the current `ITenantIdAccessor` implementation.

## Features

- **Fluent builder API** -- `WithMultiTenancy<TBuilder>()` extension method for registering multitenancy providers
- **Provider-agnostic** -- `IMultiTenantBuilder` interface that concrete providers (e.g., Finbuckle) implement
- Works with `IMultiTenant` entities and `ITenantIdAccessor` for automatic repository-level tenant filtering
- Targets .NET 8, .NET 9, and .NET 10

## Installation

```shell
dotnet add package RCommon.MultiTenancy
```

## Usage

This package is typically used together with a concrete provider like `RCommon.Finbuckle`:

```csharp
using RCommon;
using RCommon.Finbuckle;
using Finbuckle.MultiTenant;

builder.Services.AddRCommon(config =>
{
    config.WithPersistence<EFCorePerisistenceBuilder>(ef =>
    {
        ef.AddDbContext<ApplicationDbContext>("ApplicationDb", options =>
            options.UseSqlServer(connectionString));
    })
    .WithMultiTenancy<FinbuckleMultiTenantBuilder<TenantInfo>>(mt =>
    {
        // Finbuckle registers FinbuckleTenantIdAccessor as ITenantIdAccessor
        // Configure Finbuckle tenant strategies separately via services.AddMultiTenant<T>()
    });
});
```

Entities that implement `IMultiTenant` are automatically scoped to the current tenant:

```csharp
using RCommon.Entities;

public class Product : BusinessEntity<int>, IMultiTenant
{
    public string Name { get; set; }
    public string? TenantId { get; set; }
}

// Reads are filtered to the current tenant automatically
var products = await repo.FindAsync(p => p.IsActive);

// TenantId is stamped automatically on add
await repo.AddAsync(new Product { Name = "Widget" });
```

## Key Types

| Type | Description |
|------|-------------|
| `IMultiTenantBuilder` | Builder interface for configuring multitenancy providers |
| `MultiTenancyBuilderExtensions` | Provides `WithMultiTenancy<T>()` extension method on `IRCommonBuilder` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Finbuckle](https://www.nuget.org/packages/RCommon.Finbuckle) - Finbuckle-based multitenancy provider
- [RCommon.Persistence](https://www.nuget.org/packages/RCommon.Persistence) - Core persistence abstractions with multitenancy support
- [RCommon.Security](https://www.nuget.org/packages/RCommon.Security) - `ITenantIdAccessor` and claims-based tenant resolution

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
