# RCommon.Core

The foundation package for the RCommon framework, providing the fluent builder for dependency injection configuration, an in-memory event bus with transactional event routing, guard clauses, GUID generation, system time abstraction, and a rich set of extension methods.

## Features

- Fluent `AddRCommon()` builder pattern for configuring framework services via Microsoft DI
- In-memory event bus (`IEventBus`) with publish/subscribe support and polymorphic event dispatch
- Transactional event routing (`IEventRouter`) that stores events and dispatches them to the correct `IEventProducer` instances based on subscription configuration
- `EventSubscriptionManager` for isolating event subscriptions so each producer only receives its registered events
- `Guard` class with validation methods for nulls, empty strings, ranges, types, collections, email, and more
- Sequential and simple GUID generators (`IGuidGenerator`) optimized for database-friendly ordering
- `ISystemTime` abstraction for testable, time zone-aware date/time handling
- `ICommonFactory<T>` for DI-aware factory pattern with customization support
- Extension methods for collections, strings, expressions, streams, dictionaries, reflection, and IQueryable
- Reflection utilities including `ObjectGraphWalker` for traversing object graphs and `ReflectionHelper` for generic type inspection and compiled method invocation

## Installation

```shell
dotnet add package RCommon.Core
```

## Usage

```csharp
using RCommon;
using RCommon.EventHandling;

// Bootstrap RCommon in your DI configuration
services.AddRCommon(builder =>
{
    builder
        .WithSequentialGuidGenerator(options =>
            options.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString)
        .WithDateTimeSystem(options =>
            options.Kind = DateTimeKind.Utc)
        .WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
        {
            eventHandling.AddSubscriber<OrderCreatedEvent, OrderCreatedEventHandler>();
        });
});
```

## Key Types

| Type | Description |
|------|-------------|
| `IRCommonBuilder` | Fluent builder interface for configuring RCommon framework services |
| `IEventBus` | In-process event bus for publishing events and subscribing handlers |
| `ISubscriber<TEvent>` | Strongly-typed event subscriber that handles a specific event type |
| `IEventRouter` | Routes stored transactional events to the appropriate `IEventProducer` instances |
| `IEventProducer` | Dispatches serializable events to a destination (bus, broker, etc.) |
| `EventSubscriptionManager` | Tracks event-to-producer subscriptions for isolated event routing |
| `Guard` | Utility class with guard clause methods for parameter validation |
| `IGuidGenerator` | Abstraction for GUID generation (sequential or simple) |
| `ISystemTime` | Abstracts the system clock for testable time-dependent code |
| `ICommonFactory<T>` | DI-aware factory pattern for creating service instances |
| `ObjectGraphWalker` | Recursively traverses an object graph searching for instances of a specified type |
| `ReflectionHelper` | Utilities for generic type inspection, attribute retrieval, and compiled method invocation |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Models](https://www.nuget.org/packages/RCommon.Models) - Shared models for CQRS commands, queries, events, pagination, and execution results
- [RCommon.Entities](https://www.nuget.org/packages/RCommon.Entities) - Domain entity base classes with auditing, soft delete, and transactional event tracking

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
