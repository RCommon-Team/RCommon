# DDD Entity Abstractions for RCommon.Entities

**Date:** 2026-03-16
**Branch:** feature/ddd
**Status:** Design

## Summary

Add Domain-Driven Design tactical building blocks to the existing `RCommon.Entities` project: `AggregateRoot`, `DomainEntity`, `ValueObject`, and `IDomainEvent`. These types extend the existing `BusinessEntity` hierarchy and reuse the `IEntityEventTracker` pipeline for domain event dispatch.

## Goals

- Provide first-class DDD abstractions for aggregate roots, domain entities, and value objects
- Reuse existing infrastructure (`BusinessEntity`, `IEntityEventTracker`, `IEventRouter`) with zero breaking changes
- Maintain the generic key pattern (`TKey : IEquatable<TKey>`) consistent with the rest of RCommon
- Keep scope focused: entity types + domain events only (no domain services, sagas, or event sourcing in this iteration)

## Non-Goals

- Domain service abstractions
- Guard/invariant helper classes
- Event sourcing infrastructure
- Aggregate-specific repository interfaces (persistence layer changes)
- Saga/process manager abstractions

## Design Decisions

### 1. Location: In-place in RCommon.Entities

DDD types are added directly to `RCommon.Entities` in the `RCommon.Entities` namespace. Rationale: `AggregateRoot` inherits from `BusinessEntity`, and `IDomainEvent` extends `ISerializableEvent` — these are natural extensions of the existing hierarchy, not a separate concern. The project is small (12 files) and adding 6 more keeps it focused.

### 2. AggregateRoot extends BusinessEntity

`AggregateRoot<TKey>` inherits from `BusinessEntity<TKey>`. This reuses existing event tracking, key support, and entity equality. The `AddDomainEvent` method delegates to `AddLocalEvent`, making the entire event pipeline (`IEntityEventTracker` → `InMemoryEntityEventTracker` → `IEventRouter` → `IEventProducer`) work without modification.

### 3. Value Objects use C# records

`ValueObject` is an abstract record, leveraging C# record semantics for automatic structural equality, immutability, and `with`-expression support. This is the modern, idiomatic C# approach.

### 4. IDomainEvent extends ISerializableEvent

`IDomainEvent` extends the existing `ISerializableEvent` marker interface, adding `EventId` and `OccurredOn` metadata. This means domain events flow through the existing event routing pipeline unchanged.

### 5. Versioning on AggregateRoot

`AggregateRoot` includes a `Version` (int) property for optimistic concurrency control. This is essential for eventual event sourcing support and is standard DDD practice for aggregate consistency.

### 6. DomainEntity is lightweight

`DomainEntity<TKey>` is a standalone class (does not extend `BusinessEntity`) with identity-based equality but no event tracking. Entities within an aggregate raise events through their aggregate root, not directly.

## Type Hierarchy

```
Existing (unchanged):
  ITrackedEntity
    IBusinessEntity
      BusinessEntity              (abstract, composite keys, event tracking)
        BusinessEntity<TKey>      (abstract, single key, event tracking)

New DDD types:
  IAggregateRoot<TKey> : IBusinessEntity<TKey>
    AggregateRoot<TKey> : BusinessEntity<TKey>, IAggregateRoot<TKey>

  DomainEntity<TKey>              (standalone, identity only, no event tracking)

  ValueObject                     (abstract record, structural equality)

  IDomainEvent : ISerializableEvent
    DomainEvent                   (abstract record, base implementation)
```

## New Files

All files are added to `Src/RCommon.Entities/` in the `RCommon.Entities` namespace.

### IDomainEvent.cs

```csharp
using RCommon.Models.Events;

namespace RCommon.Entities;

/// <summary>
/// Represents a domain event raised by an aggregate root.
/// Extends ISerializableEvent for compatibility with the existing event routing pipeline.
/// </summary>
public interface IDomainEvent : ISerializableEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// The date and time when this event occurred.
    /// </summary>
    DateTimeOffset OccurredOn { get; }
}
```

### DomainEvent.cs

```csharp
namespace RCommon.Entities;

/// <summary>
/// Abstract base record for domain events. Provides default values for EventId and OccurredOn.
/// Use as a base for all concrete domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
```

### IAggregateRoot.cs

```csharp
namespace RCommon.Entities;

/// <summary>
/// Interface for aggregate roots in the domain model.
/// Extends IBusinessEntity to maintain compatibility with existing repository and event tracking infrastructure.
/// </summary>
public interface IAggregateRoot<TKey> : IBusinessEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// The version number used for optimistic concurrency control.
    /// Incremented on each state change to the aggregate.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// The collection of domain events raised by this aggregate that have not yet been dispatched.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
```

### AggregateRoot.cs

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace RCommon.Entities;

/// <summary>
/// Abstract base class for aggregate roots. Extends BusinessEntity to reuse event tracking,
/// key support, and entity equality. Adds versioning for optimistic concurrency and typed
/// domain event methods.
/// </summary>
/// <typeparam name="TKey">The type of the aggregate's identity.</typeparam>
public abstract class AggregateRoot<TKey> : BusinessEntity<TKey>, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Version number for optimistic concurrency control. Incremented via <see cref="IncrementVersion"/>.
    /// </summary>
    public virtual int Version { get; protected set; }

    /// <summary>
    /// Returns the domain events that have been raised by this aggregate but not yet dispatched.
    /// This is a typed view over the underlying LocalEvents collection.
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents
        => LocalEvents.OfType<IDomainEvent>().ToList().AsReadOnly();

    /// <summary>
    /// Raises a domain event on this aggregate. The event will be dispatched when
    /// the entity event tracker emits transactional events.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
        => AddLocalEvent(domainEvent);

    /// <summary>
    /// Removes a previously raised domain event before it has been dispatched.
    /// </summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
        => RemoveLocalEvent(domainEvent);

    /// <summary>
    /// Clears all pending domain events from this aggregate.
    /// </summary>
    public void ClearDomainEvents()
        => ClearLocalEvents();

    /// <summary>
    /// Increments the version number for optimistic concurrency control.
    /// Call this when the aggregate's state changes.
    /// </summary>
    protected void IncrementVersion()
        => Version++;
}
```

### DomainEntity.cs

```csharp
namespace RCommon.Entities;

/// <summary>
/// Abstract base class for domain entities within an aggregate. Provides identity-based equality
/// but no event tracking — entities within an aggregate raise events through their aggregate root.
/// </summary>
/// <typeparam name="TKey">The type of the entity's identity.</typeparam>
public abstract class DomainEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// The unique identity of this entity.
    /// </summary>
    public virtual TKey Id { get; protected set; }

    public override bool Equals(object obj)
    {
        if (obj is not DomainEntity<TKey> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (IsTransient() || other.IsTransient())
            return false;

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        if (IsTransient())
            return base.GetHashCode();

        return Id.GetHashCode();
    }

    /// <summary>
    /// Returns true if this entity has not yet been assigned a persistent identity.
    /// </summary>
    public bool IsTransient()
        => Id is null || Id.Equals(default);

    public static bool operator ==(DomainEntity<TKey> left, DomainEntity<TKey> right)
    {
        if (left is null && right is null)
            return true;
        if (left is null || right is null)
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(DomainEntity<TKey> left, DomainEntity<TKey> right)
        => !(left == right);
}
```

### ValueObject.cs

```csharp
namespace RCommon.Entities;

/// <summary>
/// Abstract base record for value objects. Leverages C# record semantics for automatic
/// structural equality, immutability, and with-expression support.
///
/// Derive concrete value objects from this type:
/// <code>
/// public record Money(decimal Amount, string Currency) : ValueObject;
/// public record Address(string Street, string City, string ZipCode) : ValueObject;
/// </code>
/// </summary>
public abstract record ValueObject;
```

## Domain Event Flow

The domain event dispatch flow reuses the existing infrastructure with zero modifications:

```
1. AggregateRoot.AddDomainEvent(IDomainEvent)
   → calls BusinessEntity.AddLocalEvent(ISerializableEvent)
   → IDomainEvent IS-A ISerializableEvent, so this just works
   → event stored in BusinessEntity._localEvents
   → C# event TransactionalEventAdded fires

2. Repository.AddAsync/UpdateAsync/DeleteAsync(aggregate)
   → EventTracker.AddEntity(aggregate)
   → (existing behavior, unchanged)

3. EmitTransactionalEventsAsync()
   → InMemoryEntityEventTracker traverses object graph
   → Collects LocalEvents from aggregate root and nested entities
   → IEventRouter.AddTransactionalEvents() + RouteEventsAsync()
   → IEventProducer dispatches via MediatR, EventBus, MassTransit, etc.
```

**No changes required to:**
- `IEntityEventTracker` interface
- `InMemoryEntityEventTracker` implementation
- `IEventRouter` / `InMemoryTransactionalEventRouter`
- Repository base classes (`LinqRepositoryBase`, `GraphRepositoryBase`, `EFCoreRepository`)
- Event producer implementations

## Existing Files: No Modifications

This design requires zero changes to existing files. All new types are additive.

## Testing Strategy

Unit tests should cover:
- `AggregateRoot`: domain event add/remove/clear, version increment, DomainEvents projection
- `DomainEntity`: identity-based equality, transient detection, type-mismatch inequality
- `ValueObject`: structural equality via record semantics, inequality for different values
- `DomainEvent`: default `EventId` and `OccurredOn` generation, `init` property overrides
- Integration: verify domain events raised on `AggregateRoot` flow through `InMemoryEntityEventTracker` and `IEventRouter` correctly

## Future Considerations

These are explicitly out of scope but inform the design:
- **Event sourcing**: `Version` on `AggregateRoot` is already positioned for event store append operations
- **Aggregate repository**: A future `IAggregateRepository<TAggregateRoot, TKey>` could enforce loading/saving complete aggregates
- **Domain services**: `IDomainService` marker interface could be added later
- **Saga/process managers**: Could consume `IDomainEvent` types for orchestration
