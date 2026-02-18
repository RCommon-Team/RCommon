# RCommon.Finbuckle

Finbuckle multitenancy provider for the RCommon framework. Adapts Finbuckle's `IMultiTenantContextAccessor<TTenantInfo>` to RCommon's `ITenantIdAccessor` interface, enabling automatic tenant-scoped repository operations using Finbuckle's tenant resolution strategies.

## Features

- **Finbuckle integration** -- `FinbuckleTenantIdAccessor<TTenantInfo>` bridges Finbuckle's tenant context to RCommon's `ITenantIdAccessor`
- **Automatic DI registration** -- `FinbuckleMultiTenantBuilder<TTenantInfo>` replaces the default `NullTenantIdAccessor` with the Finbuckle implementation
- Works with all Finbuckle tenant resolution strategies (header, route, host, claim, etc.)
- Targets .NET 8, .NET 9, and .NET 10

## Installation

```shell
dotnet add package RCommon.Finbuckle
```

## Usage

### Registration

Configure Finbuckle's multitenancy alongside RCommon:

```csharp
using RCommon;
using RCommon.Finbuckle;
using Finbuckle.MultiTenant;

// 1. Configure Finbuckle (standard Finbuckle setup)
builder.Services.AddMultiTenant<TenantInfo>()
    .WithHeaderStrategy("X-Tenant")
    .WithConfigurationStore();

// 2. Configure RCommon with Finbuckle multitenancy
builder.Services.AddRCommon(config =>
{
    config.WithClaimsAndPrincipalAccessorForWeb()
        .WithPersistence<EFCorePerisistenceBuilder>(ef =>
        {
            ef.AddDbContext<ApplicationDbContext>("ApplicationDb", options =>
                options.UseSqlServer(connectionString));
        })
        .WithMultiTenancy<FinbuckleMultiTenantBuilder<TenantInfo>>(mt =>
        {
            // FinbuckleTenantIdAccessor is registered automatically
        });
});
```

### Tenant-Scoped Entities

Entities implementing `IMultiTenant` are automatically filtered and stamped:

```csharp
using RCommon.Entities;

public class Document : BusinessEntity<Guid>, IMultiTenant
{
    public string Title { get; set; }
    public string? TenantId { get; set; }
}

// Finbuckle resolves the tenant from the request (e.g., X-Tenant header)
// Repository reads are scoped to that tenant automatically
var docs = await repo.FindAsync(d => d.Title.Contains("report"));

// TenantId is stamped from Finbuckle's resolved tenant on add
await repo.AddAsync(new Document { Title = "Q4 Report" });
```

## Key Types

| Type | Description |
|------|-------------|
| `FinbuckleTenantIdAccessor<TTenantInfo>` | Adapts Finbuckle's `IMultiTenantContextAccessor<TTenantInfo>` to `ITenantIdAccessor` |
| `FinbuckleMultiTenantBuilder<TTenantInfo>` | Builder that registers Finbuckle as the multitenancy provider for RCommon |
| `IFinbuckleMultiTenantBuilder<TTenantInfo>` | Builder interface for the Finbuckle multitenancy configuration |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.MultiTenancy](https://www.nuget.org/packages/RCommon.MultiTenancy) - Multitenancy builder abstraction (required dependency)
- [RCommon.Persistence](https://www.nuget.org/packages/RCommon.Persistence) - Core persistence abstractions with multitenancy support
- [RCommon.Security](https://www.nuget.org/packages/RCommon.Security) - `ITenantIdAccessor` interface definition

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
