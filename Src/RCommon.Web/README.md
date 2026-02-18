# RCommon.Web

ASP.NET Core integration for the RCommon security abstractions. Provides `HttpContextCurrentPrincipalAccessor` which resolves the current `ClaimsPrincipal` from `HttpContext.User` instead of `Thread.CurrentPrincipal`, making `ICurrentUser`, `ICurrentClient`, `ITenantIdAccessor`, and all claims-based security abstractions work correctly in web applications.

## Features

- **HTTP context principal accessor** -- `HttpContextCurrentPrincipalAccessor` reads the authenticated user from `IHttpContextAccessor.HttpContext.User`
- **One-line DI registration** -- `WithClaimsAndPrincipalAccessorForWeb()` registers all security services wired to the HTTP context
- **Drop-in replacement** -- use instead of `WithClaimsAndPrincipalAccessor()` in ASP.NET Core applications
- Targets .NET 8, .NET 9, and .NET 10

## Installation

```shell
dotnet add package RCommon.Web
```

## Usage

### Why This Package?

The default `ThreadCurrentPrincipalAccessor` (from `RCommon.Security`) reads from `Thread.CurrentPrincipal`, which is `null` in ASP.NET Core. This means `ICurrentUser`, `ClaimsTenantIdAccessor`, and all claims-based services silently return `null` in web apps.

`HttpContextCurrentPrincipalAccessor` bridges this gap by reading from `HttpContext.User`.

### Registration

Replace `WithClaimsAndPrincipalAccessor()` with `WithClaimsAndPrincipalAccessorForWeb()` in your ASP.NET Core application:

```csharp
using RCommon;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRCommon(config =>
{
    // Use this instead of config.WithClaimsAndPrincipalAccessor()
    config.WithClaimsAndPrincipalAccessorForWeb();
});
```

This registers:
- `HttpContextCurrentPrincipalAccessor` as `ICurrentPrincipalAccessor`
- `IHttpContextAccessor` (via `AddHttpContextAccessor()`)
- `ICurrentUser`, `ICurrentClient`, `ITenantIdAccessor` (same as the non-web variant)

### Using Security Services

Once registered, inject `ICurrentUser`, `ICurrentClient`, or `ITenantIdAccessor` in your controllers or services:

```csharp
using RCommon.Security.Users;
using RCommon.Security.Claims;

public class OrderController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly ITenantIdAccessor _tenantIdAccessor;

    public OrderController(ICurrentUser currentUser, ITenantIdAccessor tenantIdAccessor)
    {
        _currentUser = currentUser;
        _tenantIdAccessor = tenantIdAccessor;
    }

    [HttpGet]
    public IActionResult GetUserInfo()
    {
        return Ok(new
        {
            UserId = _currentUser.UserId,
            TenantId = _tenantIdAccessor.GetTenantId(),
            Roles = _currentUser.Roles,
            IsAuthenticated = _currentUser.IsAuthenticated
        });
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `HttpContextCurrentPrincipalAccessor` | `ICurrentPrincipalAccessor` implementation that reads from `HttpContext.User` |
| `WebConfigurationExtensions` | Provides `WithClaimsAndPrincipalAccessorForWeb()` extension method for DI registration |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Security](https://www.nuget.org/packages/RCommon.Security) - Core security abstractions (`ICurrentUser`, `ICurrentPrincipalAccessor`, `ITenantIdAccessor`)
- [RCommon.Authorization.Web](https://www.nuget.org/packages/RCommon.Authorization.Web) - Swagger/OpenAPI authorization filters for ASP.NET Core
- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions and builder infrastructure

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
