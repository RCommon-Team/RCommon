# RCommon.MediatR

Integrates [MediatR](https://github.com/jbogard/MediatR) with RCommon's event handling and mediator systems, providing in-process event production, notification/request handling, and pipeline behaviors while programming against RCommon's abstractions.

## Features

- Publish events to all notification handlers using MediatR's fan-out (publish) semantics
- Send events to a single request handler using MediatR's point-to-point (send) semantics
- Bridge MediatR notifications and requests to RCommon's `ISubscriber<T>` and `IAppRequestHandler<T>` abstractions
- Mediator adapter (`MediatRAdapter`) implementing `IMediatorAdapter` for request/response and notification patterns
- Built-in pipeline behaviors for logging, validation, and unit of work
- Support for both fire-and-forget requests and request/response patterns
- Event subscription routing ensures events are delivered only to their configured producers

## Installation

```shell
dotnet add package RCommon.MediatR
```

## Usage

Configure MediatR for event handling:

```csharp
using RCommon;
using RCommon.MediatR;

builder.Services.AddRCommon()
    .WithEventHandling<MediatREventHandlingBuilder>(eventHandling =>
    {
        // Register event subscribers
        eventHandling.AddSubscriber<OrderCreatedEvent, OrderCreatedEventHandler>();
    });
```

Configure MediatR as the mediator with requests, notifications, and pipeline behaviors:

```csharp
using RCommon;
using RCommon.Mediator.MediatR;

builder.Services.AddRCommon()
    .WithMediator<MediatRBuilder>(mediator =>
    {
        // Register notifications (fan-out to all handlers)
        mediator.AddNotification<OrderShippedNotification, OrderShippedHandler>();

        // Register requests (single handler, fire-and-forget)
        mediator.AddRequest<CreateOrderCommand, CreateOrderCommandHandler>();

        // Register requests with responses
        mediator.AddRequest<GetOrderQuery, OrderDto, GetOrderQueryHandler>();

        // Add pipeline behaviors
        mediator.AddLoggingToRequestPipeline();
        mediator.AddValidationToRequestPipeline();
        mediator.AddUnitOfWorkToRequestPipeline();

        // Custom MediatR configuration
        mediator.Configure(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<Program>();
        });
    });
```

## Key Types

| Type | Description |
|------|-------------|
| `PublishWithMediatREventProducer` | `IEventProducer` that publishes events to all handlers via MediatR publish (fan-out) |
| `SendWithMediatREventProducer` | `IEventProducer` that sends events to a single handler via MediatR send (point-to-point) |
| `MediatRAdapter` | `IMediatorAdapter` implementation that wraps MediatR's `IMediator` for send/publish operations |
| `MediatREventHandler<TEvent, TNotification>` | Bridges MediatR `INotificationHandler` to RCommon's `ISubscriber<T>` for event handling |
| `MediatRNotificationHandler<T, TNotification>` | Bridges MediatR notifications to RCommon's `ISubscriber<T>` for app notifications |
| `MediatRRequestHandler<T, TRequest>` | Bridges MediatR requests to RCommon's `IAppRequestHandler<T>` |
| `MediatRRequestHandler<T, TRequest, TResponse>` | Bridges MediatR requests to RCommon's `IAppRequestHandler<T, TResponse>` for request/response |
| `LoggingRequestBehavior<TRequest, TResponse>` | Pipeline behavior that logs command handling |
| `ValidatorBehavior<TRequest, TResponse>` | Pipeline behavior that validates requests before handling |
| `UnitOfWorkRequestBehavior<TRequest, TResponse>` | Pipeline behavior that wraps handlers in a transactional unit of work |
| `MediatREventHandlingBuilder` | Builder for configuring MediatR-based event handling |
| `MediatRBuilder` | Builder for configuring MediatR as the mediator implementation |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions including `IEventProducer` and `ISubscriber<T>`
- [RCommon.Mediator](https://www.nuget.org/packages/RCommon.Mediator) - Mediator abstractions (`IMediatorService`, `IMediatorAdapter`)
- [RCommon.MassTransit](https://www.nuget.org/packages/RCommon.MassTransit) - MassTransit integration for distributed messaging
- [RCommon.Wolverine](https://www.nuget.org/packages/RCommon.Wolverine) - Wolverine integration for distributed messaging

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
