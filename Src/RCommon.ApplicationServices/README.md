# RCommon.ApplicationServices

Provides a CQRS (Command Query Responsibility Segregation) implementation with dedicated command and query buses, handler registration, and optional validation integration for the RCommon framework.

## Features

- **Command Bus** -- dispatches commands to a single registered `ICommandHandler<TResult, TCommand>` and returns an `IExecutionResult`
- **Query Bus** -- dispatches queries to a single registered `IQueryHandler<TQuery, TResult>` and returns a typed result
- **Validation pipeline** -- optionally validates commands and/or queries before handler execution via `IValidationService`
- **Handler registration** -- register handlers individually or scan assemblies with automatic decorator exclusion
- **Expression caching** -- dynamically compiled handler delegates can be cached for improved dispatch performance
- **Fluent builder API** -- integrates with the `AddRCommon()` builder pattern for clean DI configuration

## Installation

```shell
dotnet add package RCommon.ApplicationServices
```

## Usage

```csharp
using RCommon;
using RCommon.ApplicationServices;

// Configure CQRS in your DI setup
services.AddRCommon(config =>
{
    config.WithCQRS<CqrsBuilder>(cqrs =>
    {
        // Register handlers individually
        cqrs.AddCommandHandler<CreateOrderHandler, CreateOrderCommand, CommandResult>();
        cqrs.AddQueryHandler<GetOrderHandler, GetOrderQuery, OrderDto>();

        // Or scan an assembly for all handlers
        cqrs.AddCommandHandlers(typeof(CreateOrderHandler).Assembly);
        cqrs.AddQueryHandlers(typeof(GetOrderHandler).Assembly);
    });
});

// Dispatch a command from your application layer
public class OrderService
{
    private readonly ICommandBus _commandBus;
    private readonly IQueryBus _queryBus;

    public OrderService(ICommandBus commandBus, IQueryBus queryBus)
    {
        _commandBus = commandBus;
        _queryBus = queryBus;
    }

    public async Task<CommandResult> CreateOrderAsync(CreateOrderCommand command)
    {
        return await _commandBus.DispatchCommandAsync(command);
    }

    public async Task<OrderDto> GetOrderAsync(GetOrderQuery query)
    {
        return await _queryBus.DispatchQueryAsync(query);
    }
}
```

### Enabling Validation

```csharp
services.AddRCommon(config =>
{
    config.WithValidation<FluentValidationBuilder>(validation =>
    {
        validation.UseWithCqrs(options =>
        {
            options.ValidateCommands = true;
            options.ValidateQueries = true;
        });
    });
});
```

## Key Types

| Type | Description |
|------|-------------|
| `ICommandBus` | Dispatches commands to their registered handler and returns an `IExecutionResult` |
| `IQueryBus` | Dispatches queries to their registered handler and returns a typed result |
| `ICommandHandler<TResult, TCommand>` | Handles a specific command type and produces an execution result |
| `IQueryHandler<TQuery, TResult>` | Handles a specific query type and produces a result |
| `IValidationService` | Validates objects before dispatch; integrates with the CQRS pipeline |
| `ValidationOutcome` | Contains a list of `ValidationFault` errors produced by validation |
| `ValidationFault` | Describes a single validation failure with property name, message, and severity |
| `CqrsValidationOptions` | Controls whether commands and/or queries are validated before dispatch |
| `CqrsBuilder` | Default `ICqrsBuilder` implementation that registers `CommandBus` and `QueryBus` |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions and builder infrastructure
- [RCommon.FluentValidation](https://www.nuget.org/packages/RCommon.FluentValidation) - FluentValidation-based `IValidationProvider` for CQRS pipeline integration
- [RCommon.Models](https://www.nuget.org/packages/RCommon.Models) - `ICommand`, `IQuery`, and `IExecutionResult` model contracts

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
