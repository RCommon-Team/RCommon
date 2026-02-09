# RCommon.Wolverine

Integrates [Wolverine](https://wolverinefx.net/) messaging with RCommon's event handling system, allowing you to produce and consume events through Wolverine's `IMessageBus` while programming against RCommon's `IEventProducer` and `ISubscriber<T>` abstractions.

## Features

- Publish events to all subscribed handlers using Wolverine's fan-out (publish) semantics
- Send events to a single handler endpoint using Wolverine's point-to-point (send) semantics
- Bridge Wolverine message handlers to RCommon's `ISubscriber<T>` abstraction for handler portability
- Event subscription routing ensures events are delivered only to their configured producers
- Factory delegate support for subscriber registration

## Installation

```shell
dotnet add package RCommon.Wolverine
```

## Usage

```csharp
using RCommon;
using RCommon.Wolverine;

builder.Services.AddRCommon()
    .WithEventHandling<WolverineEventHandlingBuilder>(eventHandling =>
    {
        // Register subscribers that bridge Wolverine to RCommon
        eventHandling.AddSubscriber<OrderCreatedEvent, OrderCreatedEventHandler>();

        // Or register with a factory delegate
        eventHandling.AddSubscriber<OrderShippedEvent, OrderShippedEventHandler>(
            sp => new OrderShippedEventHandler(sp.GetRequiredService<ILogger<OrderShippedEventHandler>>()));
    });

// Configure Wolverine transports separately via the host builder
builder.Host.UseWolverine(opts =>
{
    opts.ListenToRabbitQueue("orders");
    opts.PublishMessage<OrderCreatedEvent>().ToRabbitExchange("orders");
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
        // This publishes via Wolverine's IMessageBus to all subscribed handlers
        await _eventProducer.ProduceEventAsync(new OrderCreatedEvent(order));
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `PublishWithWolverineEventProducer` | `IEventProducer` that publishes events to all handlers via `IMessageBus.PublishAsync` (fan-out) |
| `SendWithWolverineEventProducer` | `IEventProducer` that sends events to a single endpoint via `IMessageBus.SendAsync` (point-to-point) |
| `WolverineEventHandler<TEvent>` | Wolverine `IWolverineHandler` that delegates to an RCommon `ISubscriber<T>` |
| `IWolverineEventHandlingBuilder` | Builder interface for configuring Wolverine event handling within RCommon |
| `WolverineEventHandlingBuilder` | Default builder implementation for configuring Wolverine event handling |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Core abstractions including `IEventProducer` and `ISubscriber<T>`
- [RCommon.MassTransit](https://www.nuget.org/packages/RCommon.MassTransit) - MassTransit integration for distributed messaging
- [RCommon.MediatR](https://www.nuget.org/packages/RCommon.MediatR) - MediatR integration for in-process event handling and mediator pattern

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
