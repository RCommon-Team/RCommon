# RCommon.Security

Provides claims-based security abstractions for RCommon, including current user and current client identity access built on top of `ClaimsPrincipal`, with configurable claim type mappings and multi-tenancy support.

## Features

- **Current user abstraction** -- `ICurrentUser` exposes the authenticated user's ID, tenant ID, roles, and claims without depending on a specific auth framework
- **Current client abstraction** -- `ICurrentClient` identifies the calling OAuth/API client from the `client_id` claim
- **ClaimsPrincipal accessor** -- `ICurrentPrincipalAccessor` provides the current principal with support for temporary principal replacement via scoped `IDisposable`
- **AsyncLocal principal override** -- `CurrentPrincipalAccessorBase` uses `AsyncLocal<T>` so overridden principals flow across async contexts
- **Configurable claim types** -- `ClaimTypesConst` allows customizing which claim URIs map to user ID, tenant ID, client ID, roles, etc.
- **ClaimsIdentity extensions** -- helper methods for finding user/tenant/client IDs and for safely adding or replacing claims
- **Authorization exception** -- `AuthorizationException` with configurable severity, error codes, and fluent data attachment
- **Fluent builder API** -- integrates with the `AddRCommon()` builder pattern for one-line DI registration

## Installation

```shell
dotnet add package RCommon.Security
```

## Usage

```csharp
using RCommon;
using RCommon.Security.Users;
using RCommon.Security.Clients;

// Register security services in your DI setup
services.AddRCommon(config =>
{
    config.WithClaimsAndPrincipalAccessor();
});

// Inject ICurrentUser or ICurrentClient in your services
public class TenantService
{
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentClient _currentClient;

    public TenantService(ICurrentUser currentUser, ICurrentClient currentClient)
    {
        _currentUser = currentUser;
        _currentClient = currentClient;
    }

    public Guid GetTenantId()
    {
        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return _currentUser.TenantId
            ?? throw new InvalidOperationException("No tenant claim found.");
    }

    public string GetClientId()
    {
        return _currentClient.Id
            ?? throw new InvalidOperationException("No client identity found.");
    }

    public bool IsInRole(string role)
    {
        return _currentUser.Roles.Contains(role);
    }
}
```

### Customizing Claim Types

```csharp
// Override the default claim type URIs at startup if your identity provider uses custom claims
ClaimTypesConst.UserId = "sub";
ClaimTypesConst.TenantId = "tenant";
ClaimTypesConst.ClientId = "azp";
```

## Key Types

| Type | Description |
|------|-------------|
| `ICurrentUser` | Provides the authenticated user's ID, tenant ID, roles, and claim lookups |
| `CurrentUser` | Default implementation that reads from the current `ClaimsPrincipal` |
| `ICurrentClient` | Provides the authenticated client application's ID and authentication status |
| `CurrentClient` | Default implementation that reads the `client_id` claim from the principal |
| `ICurrentPrincipalAccessor` | Accesses the current `ClaimsPrincipal` and supports scoped replacement |
| `ThreadCurrentPrincipalAccessor` | Default accessor that reads from `Thread.CurrentPrincipal` |
| `CurrentPrincipalAccessorBase` | Abstract base using `AsyncLocal<T>` for async-safe principal overrides |
| `ClaimTypesConst` | Configurable constants for standard claim type URIs (user ID, role, tenant, etc.) |
| `AuthorizationException` | Exception for unauthorized requests with log level, error code, and fluent data API |
| `ClaimsIdentityExtensions` | Extension methods for extracting user/tenant/client IDs and managing claims |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions and builder infrastructure

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
