# RCommon.Mediator

Provides a mediator abstraction layer for RCommon that decouples application code from specific mediator libraries, enabling request/response dispatch and notification publishing through a uniform API.

## Features

- **Library-agnostic mediator API** -- depend on `IMediatorService` instead of MediatR, Wolverine, or any other implementation
- **Request/response dispatch** -- send requests to a single handler with or without a return value
- **Notification publishing** -- broadcast notifications to all registered subscribers
- **Adapter pattern** -- swap mediator implementations by changing only the `IMediatorAdapter` registration
- **Subscriber contracts** -- `IAppRequest`, `IAppRequest<TResponse>`, and `IAppNotification` marker interfaces for message classification
- **Handler contracts** -- `IAppRequestHandler<TRequest>` and `IAppRequestHandler<TRequest, TResponse>` for implementing handlers
- **Fluent builder API** -- integrates with the `AddRCommon()` builder pattern for clean DI configuration

## Installation

```shell
dotnet add package RCommon.Mediator
```

## Usage

```csharp
using RCommon;
using RCommon.Mediator;
using RCommon.Mediator.Subscribers;

// Configure the mediator in your DI setup using a concrete adapter (e.g., MediatR)
services.AddRCommon(config =>
{
    config.WithMediator<MediatRBuilder>(mediator =>
    {
        // Register handlers from your application assembly
        mediator.AddHandlersFromAssemblyContainingType<CreateOrderHandler>();
    });
});

// Define a request and handler
public class CreateOrderRequest : IAppRequest<OrderDto>
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderHandler : IAppRequestHandler<CreateOrderRequest, OrderDto>
{
    public async Task<OrderDto> HandleAsync(CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // Handle the request and return a result
        return new OrderDto { Id = Guid.NewGuid(), ProductName = request.ProductName };
    }
}

// Consume the mediator from your application layer
public class OrderController
{
    private readonly IMediatorService _mediator;

    public OrderController(IMediatorService mediator)
    {
        _mediator = mediator;
    }

    public async Task<OrderDto> CreateOrder(CreateOrderRequest request)
    {
        return await _mediator.Send<CreateOrderRequest, OrderDto>(request);
    }

    public async Task NotifyOrderCreated(OrderCreatedNotification notification)
    {
        await _mediator.Publish(notification);
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `IMediatorService` | Primary application-facing interface for sending requests and publishing notifications |
| `MediatorService` | Default implementation that delegates to an `IMediatorAdapter` |
| `IMediatorAdapter` | Adapter interface that bridges to a specific mediator library (e.g., MediatR) |
| `IMediatorBuilder` | Builder contract for configuring a mediator implementation within `AddRCommon()` |
| `IAppRequest` | Marker interface for requests dispatched to a single handler with no return value |
| `IAppRequest<TResponse>` | Marker interface for requests that return a response of type `TResponse` |
| `IAppNotification` | Marker interface for notifications broadcast to all registered subscribers |
| `IAppRequestHandler<TRequest>` | Handler contract for requests with no return value |
| `IAppRequestHandler<TRequest, TResponse>` | Handler contract for requests that produce a typed response |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions and builder infrastructure
- [RCommon.Mediatr](https://www.nuget.org/packages/RCommon.Mediatr) - MediatR adapter implementation for `IMediatorAdapter`

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
