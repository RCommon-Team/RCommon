# RCommon.Authorization.Web

Swagger/OpenAPI operation filters for ASP.NET Core that automatically surface authorization metadata in your API documentation, including required Authorization headers and OAuth2 security requirements.

## Features

- Automatically adds an `Authorization` header parameter to Swagger operations protected by `AuthorizeFilter`
- Detects `[Authorize]` attribute on controllers and actions and adds 401/403 response codes to the OpenAPI spec
- Attaches OAuth2 security requirements to authorized operations
- Respects `[AllowAnonymous]` to skip authorization header injection
- Compatible with Swashbuckle.AspNetCore across .NET 8, .NET 9, and .NET 10

## Installation

```shell
dotnet add package RCommon.Authorization.Web
```

## Usage

Register the operation filters when configuring Swagger in your ASP.NET Core application:

```csharp
using RCommon.Authorization.Web.Filters;

builder.Services.AddSwaggerGen(options =>
{
    // Adds a required Authorization header to operations with AuthorizeFilter
    options.OperationFilter<AuthorizationHeaderParameterOperationFilter>();

    // Adds 401/403 responses and OAuth2 security to operations with [Authorize]
    options.OperationFilter<AuthorizeCheckOperationFilter>();
});
```

## Key Types

| Type | Description |
|------|-------------|
| `AuthorizationHeaderParameterOperationFilter` | Adds a required `Authorization` header parameter to operations protected by `AuthorizeFilter` |
| `AuthorizeCheckOperationFilter` | Adds 401/403 responses and an OAuth2 security requirement to operations decorated with `[Authorize]` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Security](https://www.nuget.org/packages/RCommon.Security) - Core security abstractions
- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions and builder infrastructure

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
