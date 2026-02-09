# RCommon.MassTransit

Integrates [MassTransit](https://masstransit.io/) distributed messaging with RCommon's event handling system, allowing you to produce and consume events through MassTransit while programming against RCommon's `IEventProducer` and `ISubscriber<T>` abstractions.

## Features

- Publish events to all subscribed consumers using MassTransit's fan-out (publish) semantics
- Send events to a single consumer endpoint using MassTransit's point-to-point (send) semantics
- Bridge MassTransit consumers to RCommon's `ISubscriber<T>` abstraction for handler portability
- Event subscription routing ensures events are delivered only to their configured producers
- Full access to MassTransit's `IBusRegistrationConfigurator` for transport and consumer configuration
- Automatic hosted service, health check, and instrumentation registration

## Installation

```shell
dotnet add package RCommon.MassTransit
```

## Usage

```csharp
using RCommon;
using RCommon.MassTransit;

builder.Services.AddRCommon()
    .WithEventHandling<MassTransitEventHandlingBuilder>(eventHandling =>
    {
        // Register subscribers that bridge MassTransit to RCommon
        eventHandling.AddSubscriber<OrderCreatedEvent, OrderCreatedEventHandler>();

        // Configure MassTransit transports (full IBusRegistrationConfigurator access)
        eventHandling.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host("localhost", "/", h =>
            {
                h.Username("guest");
                h.Password("guest");
            });
            cfg.ConfigureEndpoints(context);
        });
    });
```

Produce events from application code:

```csharp
public class OrderService
{
    private readonly IEventProducer _eventProducer;

    public OrderService(IEventProducer eventProducer)
    {
        _eventProducer = eventProducer;
    }

    public async Task CreateOrderAsync(Order order)
    {
        // This publishes via MassTransit to all subscribed consumers
        await _eventProducer.ProduceEventAsync(new OrderCreatedEvent(order));
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `PublishWithMassTransitEventProducer` | `IEventProducer` that publishes events to all consumers via `IBus.Publish` (fan-out) |
| `SendWithMassTransitEventProducer` | `IEventProducer` that sends events to a single endpoint via `IBus.Send` (point-to-point) |
| `MassTransitEventHandler<TEvent>` | MassTransit `IConsumer<T>` that delegates to an RCommon `ISubscriber<T>` |
| `IMassTransitEventHandlingBuilder` | Builder combining `IEventHandlingBuilder` with MassTransit's `IBusRegistrationConfigurator` |
| `MassTransitEventHandlingBuilder` | Default builder implementation for configuring MassTransit event handling |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions including `IEventProducer` and `ISubscriber<T>`
- [RCommon.MediatR](https://www.nuget.org/packages/RCommon.MediatR) - MediatR integration for in-process event handling and mediator pattern
- [RCommon.Wolverine](https://www.nuget.org/packages/RCommon.Wolverine) - Wolverine integration for distributed messaging

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
