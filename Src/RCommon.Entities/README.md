# RCommon.Entities

Domain entity base classes for the RCommon framework, providing a strongly-typed `BusinessEntity<TKey>` base with built-in transactional event tracking, auditing support via `AuditedEntity`, and an `IEntityEventTracker` that collects and emits entity events through the event routing infrastructure.

## Features

- `BusinessEntity` and `BusinessEntity<TKey>` abstract base classes with composite and single-key support
- Built-in transactional (local) event accumulation on entities via `AddLocalEvent`, `RemoveLocalEvent`, and `ClearLocalEvents`
- Entity-level event notifications (`TransactionalEventAdded`, `TransactionalEventRemoved`, `TransactionalEventsCleared`) for observing event changes
- `AuditedEntity` base classes that track `CreatedBy`, `DateCreated`, `LastModifiedBy`, and `DateLastModified` with flexible user types
- `ITrackedEntity` interface for opting entities into event tracking
- `IEntityEventTracker` and `InMemoryEntityEventTracker` for collecting entity events across object graphs and routing them through `IEventRouter`
- `EntityNotFoundException` for consistent "entity not found" error handling with type and ID context

## Installation

```shell
dotnet add package RCommon.Entities
```

## Usage

```csharp
using RCommon.Entities;
using RCommon.Models.Events;

// Define a domain entity with a GUID key
public class Order : BusinessEntity<Guid>
{
    public string ProductName { get; set; }
    public int Quantity { get; set; }

    public void Submit()
    {
        // Add a transactional event that will be emitted on persistence
        AddLocalEvent(new OrderSubmittedEvent { OrderId = Id });
    }
}

// Define an audited entity tracking who created/modified it
public class Invoice : AuditedEntity<Guid, string, string>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}

// Emit entity events through the event router
public class OrderService
{
    private readonly IEntityEventTracker _eventTracker;

    public OrderService(IEntityEventTracker eventTracker)
    {
        _eventTracker = eventTracker;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        order.Submit();
        _eventTracker.AddEntity(order);
        await _eventTracker.EmitTransactionalEventsAsync();
    }
}
```

## Key Types

| Type | Description |
|------|-------------|
| `IBusinessEntity` | Base entity interface with composite key support and local event collection |
| `IBusinessEntity<TKey>` | Entity interface with a single strongly-typed `Id` property |
| `BusinessEntity` | Abstract base class with transactional event tracking and entity equality |
| `BusinessEntity<TKey>` | Generic base class adding a typed primary key to `BusinessEntity` |
| `IAuditedEntity<TCreatedByUser, TLastModifiedByUser>` | Audit contract with created/modified user and timestamp properties |
| `AuditedEntity<TKey, TCreatedByUser, TLastModifiedByUser>` | Base class combining `BusinessEntity<TKey>` with full audit tracking |
| `ITrackedEntity` | Marks an entity as eligible for event tracking via `AllowEventTracking` |
| `IEntityEventTracker` | Collects tracked entities and emits their transactional events |
| `InMemoryEntityEventTracker` | In-memory implementation that traverses entity object graphs and routes events |
| `EntityNotFoundException` | Exception for when an expected entity does not exist, with type and ID context |

## Documentation

For full documentation, visit [rcommon.com](https://rcommon.com).

## Related Packages

- [RCommon.Core](https://www.nuget.org/packages/RCommon.Core) - Foundation package with event bus, builder pattern, guards, and extensions
- [RCommon.Models](https://www.nuget.org/packages/RCommon.Models) - Shared models for CQRS commands, queries, events, pagination, and execution results

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
