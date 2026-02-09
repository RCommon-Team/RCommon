# RCommon.FluentValidation

FluentValidation integration for RCommon's `IValidationProvider` abstraction. This package bridges the FluentValidation library into RCommon's validation pipeline, resolving registered `IValidator<T>` instances from the DI container and executing them with support for automatic CQRS command/query validation.

## Features

- Implements `IValidationProvider` using the FluentValidation library
- Resolves and executes all registered `IValidator<T>` instances for a given type from DI
- Runs multiple validators concurrently via `Task.WhenAll`
- Maps FluentValidation failures to RCommon's `ValidationOutcome` and `ValidationFault` types
- Supports optional automatic validation of CQRS commands and queries via `CqrsValidationOptions`
- Assembly scanning to auto-register all validators in one or more assemblies
- Configurable `throwOnFaults` behavior to throw `ValidationException` on failure
- Registered as a scoped service in the DI container

## Installation

```shell
dotnet add package RCommon.FluentValidation
```

## Usage

Register FluentValidation through the RCommon builder and add your validators:

```csharp
using RCommon;
using RCommon.FluentValidation;

services.AddRCommon(builder =>
{
    builder.WithValidation<FluentValidationBuilder>(validation =>
    {
        validation.AddValidator<CreateOrderDto, CreateOrderDtoValidator>();

        // Or scan an assembly for all validators
        validation.AddValidatorsFromAssembly(typeof(CreateOrderDtoValidator).Assembly);
    });
});
```

To enable automatic validation in the CQRS pipeline:

```csharp
services.AddRCommon(builder =>
{
    builder.WithValidation<FluentValidationBuilder>(options =>
    {
        options.ValidateCommands = true;
        options.ValidateQueries = true;
    });
});
```

Inject and use `IValidationProvider` directly when needed:

```csharp
public class OrderService
{
    private readonly IValidationProvider _validator;

    public OrderService(IValidationProvider validator)
    {
        _validator = validator;
    }

    public async Task CreateOrder(CreateOrderDto dto)
    {
        var outcome = await _validator.ValidateAsync(dto, throwOnFaults: true);

        // If throwOnFaults is false, inspect the outcome manually
        if (!outcome.IsValid)
        {
            foreach (var fault in outcome.Errors)
            {
                Console.WriteLine($"{fault.PropertyName}: {fault.ErrorMessage}");
            }
        }
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `FluentValidationProvider` | `IValidationProvider` implementation that resolves and runs FluentValidation validators |
| `FluentValidationBuilder` | Registers `FluentValidationProvider` into the DI container |
| `IFluentValidationBuilder` | Builder interface exposing `IServiceCollection` for validator registration |
| `FluentValidationBuilderExtensions` | Provides `AddValidator<T, TValidator>()`, `AddValidatorsFromAssembly()`, and assembly scanning methods |
| `ValidationBuilderExtensions` | Provides `WithValidation<T>()` on `IRCommonBuilder` for pipeline registration |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.ApplicationServices](https://www.nuget.org/packages/RCommon.ApplicationServices) - Core validation abstractions (IValidationProvider, ValidationOutcome, ValidationFault)
- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - RCommon framework core and DI builder

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
