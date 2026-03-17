# DDD Entity Abstractions Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add AggregateRoot, DomainEntity, ValueObject, and IDomainEvent abstractions to the RCommon.Entities project with full test coverage.

**Architecture:** New DDD types extend the existing BusinessEntity hierarchy. AggregateRoot<TKey> inherits BusinessEntity<TKey> and reuses the IEntityEventTracker pipeline for domain event dispatch. DomainEntity<TKey> is a lightweight standalone base with identity equality. ValueObject is a C# abstract record. IDomainEvent extends ISerializableEvent for pipeline compatibility.

**Tech Stack:** C# / .NET (net8.0, net9.0, net10.0), xUnit, FluentAssertions, Moq, Bogus

**Spec:** `docs/superpowers/specs/2026-03-16-ddd-entity-abstractions-design.md`

---

## File Structure

### Source files (all in `Src/RCommon.Entities/`)

| File | Action | Responsibility |
|------|--------|---------------|
| `IDomainEvent.cs` | Create | Interface extending ISerializableEvent with EventId + OccurredOn |
| `DomainEvent.cs` | Create | Abstract record implementing IDomainEvent with defaults |
| `IAggregateRoot.cs` | Create | Non-generic + generic interfaces for aggregate roots |
| `AggregateRoot.cs` | Create | Abstract base class extending BusinessEntity<TKey> |
| `DomainEntity.cs` | Create | Lightweight entity base with identity equality, no event tracking |
| `ValueObject.cs` | Create | Abstract record for value objects |

### Test files (all in `Tests/RCommon.Entities.Tests/`)

| File | Action | Responsibility |
|------|--------|---------------|
| `DomainEventTests.cs` | Create | Tests for IDomainEvent/DomainEvent record behavior |
| `AggregateRootTests.cs` | Create | Tests for AggregateRoot domain events, versioning, dual-list sync |
| `DomainEntityTests.cs` | Create | Tests for identity equality, transient detection, operator overloads |
| `ValueObjectTests.cs` | Create | Tests for structural equality via records |

### No existing files are modified.

---

## Chunk 1: Domain Events (IDomainEvent + DomainEvent)

### Task 1: IDomainEvent and DomainEvent — Write failing tests

**Files:**
- Test: `Tests/RCommon.Entities.Tests/DomainEventTests.cs`

- [ ] **Step 1: Create the test file with test stubs**

```csharp
// Tests/RCommon.Entities.Tests/DomainEventTests.cs
using FluentAssertions;
using RCommon.Entities;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for IDomainEvent interface and DomainEvent abstract record.
/// </summary>
public class DomainEventTests
{
    #region Test Domain Events

    /// <summary>
    /// Concrete domain event for testing.
    /// </summary>
    private record TestOrderPlacedEvent(Guid OrderId, decimal Total) : DomainEvent;

    /// <summary>
    /// Another concrete domain event for equality testing.
    /// </summary>
    private record TestOrderCancelledEvent(Guid OrderId, string Reason) : DomainEvent;

    #endregion

    #region IDomainEvent Contract Tests

    [Fact]
    public void DomainEvent_Implements_IDomainEvent()
    {
        // Arrange & Act
        var domainEvent = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);

        // Assert
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    [Fact]
    public void DomainEvent_Implements_ISerializableEvent()
    {
        // Arrange & Act
        var domainEvent = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);

        // Assert
        domainEvent.Should().BeAssignableTo<ISerializableEvent>();
    }

    #endregion

    #region Default Property Tests

    [Fact]
    public void DomainEvent_EventId_IsAssignedByDefault()
    {
        // Arrange & Act
        var domainEvent = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);

        // Assert
        domainEvent.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void DomainEvent_OccurredOn_IsAssignedByDefault()
    {
        // Arrange & Act
        var before = DateTimeOffset.UtcNow;
        var domainEvent = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);
        var after = DateTimeOffset.UtcNow;

        // Assert
        domainEvent.OccurredOn.Should().BeOnOrAfter(before);
        domainEvent.OccurredOn.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void DomainEvent_TwoInstances_HaveDifferentEventIds()
    {
        // Arrange & Act
        var event1 = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);
        var event2 = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    #endregion

    #region Init Property Override Tests

    [Fact]
    public void DomainEvent_EventId_CanBeOverriddenViaInit()
    {
        // Arrange
        var customId = Guid.NewGuid();

        // Act
        var domainEvent = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m)
        {
            EventId = customId
        };

        // Assert
        domainEvent.EventId.Should().Be(customId);
    }

    [Fact]
    public void DomainEvent_OccurredOn_CanBeOverriddenViaInit()
    {
        // Arrange
        var customTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);

        // Act
        var domainEvent = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m)
        {
            OccurredOn = customTime
        };

        // Assert
        domainEvent.OccurredOn.Should().Be(customTime);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void DomainEvent_SameValues_AreEqual()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var occurredOn = DateTimeOffset.UtcNow;

        // Act
        var event1 = new TestOrderPlacedEvent(orderId, 99.99m) { EventId = eventId, OccurredOn = occurredOn };
        var event2 = new TestOrderPlacedEvent(orderId, 99.99m) { EventId = eventId, OccurredOn = occurredOn };

        // Assert
        event1.Should().Be(event2);
    }

    [Fact]
    public void DomainEvent_DifferentValues_AreNotEqual()
    {
        // Arrange & Act
        var event1 = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);
        var event2 = new TestOrderPlacedEvent(Guid.NewGuid(), 50.00m);

        // Assert
        event1.Should().NotBe(event2);
    }

    [Fact]
    public void DomainEvent_DifferentTypes_AreNotEqual()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var placedEvent = new TestOrderPlacedEvent(orderId, 99.99m);
        var cancelledEvent = new TestOrderCancelledEvent(orderId, "Changed mind");

        // Assert
        placedEvent.Should().NotBe(cancelledEvent);
    }

    #endregion

    #region With-Expression Tests

    [Fact]
    public void DomainEvent_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new TestOrderPlacedEvent(Guid.NewGuid(), 99.99m);

        // Act
        var modified = original with { Total = 149.99m };

        // Assert
        modified.Total.Should().Be(149.99m);
        modified.OrderId.Should().Be(original.OrderId);
        modified.EventId.Should().Be(original.EventId);
        modified.OccurredOn.Should().Be(original.OccurredOn);
    }

    #endregion
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~DomainEventTests" --no-restore -v quiet`
Expected: Build failure — `DomainEvent` and `IDomainEvent` types do not exist yet.

### Task 2: IDomainEvent and DomainEvent — Implement

**Files:**
- Create: `Src/RCommon.Entities/IDomainEvent.cs`
- Create: `Src/RCommon.Entities/DomainEvent.cs`

- [ ] **Step 3: Create IDomainEvent.cs**

```csharp
// Src/RCommon.Entities/IDomainEvent.cs
using RCommon.Models.Events;

namespace RCommon.Entities
{
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
}
```

- [ ] **Step 4: Create DomainEvent.cs**

```csharp
// Src/RCommon.Entities/DomainEvent.cs
namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base record for domain events. Provides default values for EventId and OccurredOn.
    /// Use as a base for all concrete domain events.
    /// </summary>
    public abstract record DomainEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~DomainEventTests" --no-restore -v quiet`
Expected: All 11 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add Src/RCommon.Entities/IDomainEvent.cs Src/RCommon.Entities/DomainEvent.cs Tests/RCommon.Entities.Tests/DomainEventTests.cs
git commit -m "feat: add IDomainEvent interface and DomainEvent base record

IDomainEvent extends ISerializableEvent with EventId and OccurredOn.
DomainEvent is an abstract record with sensible defaults."
```

---

## Chunk 2: ValueObject

### Task 3: ValueObject — Write failing tests

**Files:**
- Test: `Tests/RCommon.Entities.Tests/ValueObjectTests.cs`

- [ ] **Step 7: Create the test file**

```csharp
// Tests/RCommon.Entities.Tests/ValueObjectTests.cs
using FluentAssertions;
using RCommon.Entities;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for ValueObject abstract record.
/// </summary>
public class ValueObjectTests
{
    #region Test Value Objects

    private record Money(decimal Amount, string Currency) : ValueObject;

    private record Address(string Street, string City, string ZipCode) : ValueObject;

    #endregion

    #region Structural Equality Tests

    [Fact]
    public void ValueObject_SameValues_AreEqual()
    {
        // Arrange & Act
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");

        // Assert
        money1.Should().Be(money2);
    }

    [Fact]
    public void ValueObject_DifferentValues_AreNotEqual()
    {
        // Arrange & Act
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "USD");

        // Assert
        money1.Should().NotBe(money2);
    }

    [Fact]
    public void ValueObject_DifferentTypes_AreNotEqual()
    {
        // Arrange & Act — two different ValueObject subtypes
        var money = new Money(100.00m, "USD");
        var address = new Address("123 Main St", "Springfield", "62701");

        // Assert
        money.Should().NotBe(address);
    }

    [Fact]
    public void ValueObject_SameValues_HaveSameHashCode()
    {
        // Arrange & Act
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(100.00m, "USD");

        // Assert
        money1.GetHashCode().Should().Be(money2.GetHashCode());
    }

    [Fact]
    public void ValueObject_DifferentValues_HaveDifferentHashCode()
    {
        // Arrange & Act
        var money1 = new Money(100.00m, "USD");
        var money2 = new Money(200.00m, "EUR");

        // Assert
        money1.GetHashCode().Should().NotBe(money2.GetHashCode());
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void ValueObject_EqualityOperator_ReturnsTrueForSameValues()
    {
        // Arrange & Act
        var money1 = new Money(50.00m, "GBP");
        var money2 = new Money(50.00m, "GBP");

        // Assert
        (money1 == money2).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_InequalityOperator_ReturnsTrueForDifferentValues()
    {
        // Arrange & Act
        var money1 = new Money(50.00m, "GBP");
        var money2 = new Money(75.00m, "GBP");

        // Assert
        (money1 != money2).Should().BeTrue();
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void ValueObject_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new Money(100.00m, "USD");

        // Act
        var modified = original with { Amount = 200.00m };

        // Assert
        modified.Amount.Should().Be(200.00m);
        modified.Currency.Should().Be("USD");
        original.Amount.Should().Be(100.00m, "original should be unchanged");
    }

    #endregion

    #region Interface Conformance Tests

    [Fact]
    public void ValueObject_ConcreteType_IsAssignableToValueObject()
    {
        // Arrange & Act
        var money = new Money(100.00m, "USD");

        // Assert
        money.Should().BeAssignableTo<ValueObject>();
    }

    #endregion
}
```

- [ ] **Step 8: Run test to verify it fails**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~ValueObjectTests" --no-restore -v quiet`
Expected: Build failure — `ValueObject` type does not exist yet.

### Task 4: ValueObject — Implement

**Files:**
- Create: `Src/RCommon.Entities/ValueObject.cs`

- [ ] **Step 9: Create ValueObject.cs**

```csharp
// Src/RCommon.Entities/ValueObject.cs
namespace RCommon.Entities
{
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
}
```

- [ ] **Step 10: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~ValueObjectTests" --no-restore -v quiet`
Expected: All 9 tests PASS.

- [ ] **Step 11: Commit**

```bash
git add Src/RCommon.Entities/ValueObject.cs Tests/RCommon.Entities.Tests/ValueObjectTests.cs
git commit -m "feat: add ValueObject abstract record

C# record-based value object with automatic structural equality,
immutability, and with-expression support."
```

---

## Chunk 3: DomainEntity

### Task 5: DomainEntity — Write failing tests

**Files:**
- Test: `Tests/RCommon.Entities.Tests/DomainEntityTests.cs`

- [ ] **Step 12: Create the test file**

```csharp
// Tests/RCommon.Entities.Tests/DomainEntityTests.cs
using Bogus;
using FluentAssertions;
using RCommon.Entities;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for DomainEntity{TKey} abstract class.
/// </summary>
public class DomainEntityTests
{
    private readonly Faker _faker;

    public DomainEntityTests()
    {
        _faker = new Faker();
    }

    #region Test Entities

    private class TestDomainEntityInt : DomainEntity<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestDomainEntityInt() { }

        public TestDomainEntityInt(int id)
        {
            Id = id;
        }
    }

    private class TestDomainEntityGuid : DomainEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestDomainEntityGuid() { }

        public TestDomainEntityGuid(Guid id)
        {
            Id = id;
        }
    }

    private class TestDomainEntityString : DomainEntity<string>
    {
        public string Name { get; set; } = string.Empty;

        public TestDomainEntityString() { }

        public TestDomainEntityString(string id)
        {
            Id = id;
        }
    }

    /// <summary>
    /// A different entity type with the same key type, for cross-type equality tests.
    /// </summary>
    private class TestOtherDomainEntityInt : DomainEntity<int>
    {
        public TestOtherDomainEntityInt(int id)
        {
            Id = id;
        }
    }

    #endregion

    #region Identity Tests

    [Fact]
    public void DomainEntity_DefaultConstructor_IdIsDefault()
    {
        // Arrange & Act
        var entity = new TestDomainEntityInt();

        // Assert
        entity.Id.Should().Be(default(int));
    }

    [Fact]
    public void DomainEntity_ConstructorWithId_SetsId()
    {
        // Arrange
        var id = _faker.Random.Int(1, 1000);

        // Act
        var entity = new TestDomainEntityInt(id);

        // Assert
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void DomainEntity_GuidKey_SetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var entity = new TestDomainEntityGuid(id);

        // Assert
        entity.Id.Should().Be(id);
    }

    [Fact]
    public void DomainEntity_StringKey_SetsId()
    {
        // Arrange
        var id = _faker.Random.AlphaNumeric(10);

        // Act
        var entity = new TestDomainEntityString(id);

        // Assert
        entity.Id.Should().Be(id);
    }

    #endregion

    #region Transient Detection Tests

    [Fact]
    public void IsTransient_DefaultIntId_ReturnsTrue()
    {
        // Arrange & Act
        var entity = new TestDomainEntityInt();

        // Assert
        entity.IsTransient().Should().BeTrue();
    }

    [Fact]
    public void IsTransient_DefaultGuidId_ReturnsTrue()
    {
        // Arrange & Act
        var entity = new TestDomainEntityGuid();

        // Assert
        entity.IsTransient().Should().BeTrue();
    }

    [Fact]
    public void IsTransient_NullStringId_ReturnsTrue()
    {
        // Arrange & Act
        var entity = new TestDomainEntityString();

        // Assert
        entity.IsTransient().Should().BeTrue();
    }

    [Fact]
    public void IsTransient_NonDefaultId_ReturnsFalse()
    {
        // Arrange & Act
        var entity = new TestDomainEntityInt(42);

        // Assert
        entity.IsTransient().Should().BeFalse();
    }

    [Fact]
    public void IsTransient_NonEmptyGuidId_ReturnsFalse()
    {
        // Arrange & Act
        var entity = new TestDomainEntityGuid(Guid.NewGuid());

        // Assert
        entity.IsTransient().Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(99);

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_SameReference_ReturnsTrue()
    {
        // Arrange
        var entity = new TestDomainEntityInt(42);

        // Act & Assert
        entity.Equals(entity).Should().BeTrue();
    }

    [Fact]
    public void Equals_Null_ReturnsFalse()
    {
        // Arrange
        var entity = new TestDomainEntityInt(42);

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentType_SameId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestOtherDomainEntityInt(42);

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_BothTransient_ReturnsFalse()
    {
        // Arrange — two transient entities should not be considered equal
        var entity1 = new TestDomainEntityInt();
        var entity2 = new TestDomainEntityInt();

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_OneTransient_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt();

        // Act & Assert
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_ObjectOverload_WorksCorrectly()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        object entity2 = new TestDomainEntityInt(42);

        // Act & Assert
        entity1.Equals(entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_NonDomainEntityObject_ReturnsFalse()
    {
        // Arrange
        var entity = new TestDomainEntityInt(42);
        var nonEntity = "not an entity";

        // Act & Assert
        entity.Equals(nonEntity).Should().BeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_SameId_ReturnsSameHash()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_TransientEntity_ReturnsObjectHashCode()
    {
        // Arrange — transient entities use base.GetHashCode(), so two instances differ
        var entity1 = new TestDomainEntityInt();
        var entity2 = new TestDomainEntityInt();

        // Act & Assert — just verify they don't throw; values will differ
        entity1.GetHashCode().Should().NotBe(0);
        entity2.GetHashCode().Should().NotBe(0);
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void EqualityOperator_SameId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);

        // Act & Assert
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_DifferentId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(99);

        // Act & Assert
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        TestDomainEntityInt? entity1 = null;
        TestDomainEntityInt? entity2 = null;

        // Act & Assert
        (entity1 == entity2).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperator_OneNull_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        TestDomainEntityInt? entity2 = null;

        // Act & Assert
        (entity1 == entity2).Should().BeFalse();
    }

    [Fact]
    public void InequalityOperator_DifferentId_ReturnsTrue()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(99);

        // Act & Assert
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void InequalityOperator_SameId_ReturnsFalse()
    {
        // Arrange
        var entity1 = new TestDomainEntityInt(42);
        var entity2 = new TestDomainEntityInt(42);

        // Act & Assert
        (entity1 != entity2).Should().BeFalse();
    }

    #endregion

    #region IEquatable Tests

    [Fact]
    public void DomainEntity_Implements_IEquatable()
    {
        // Arrange & Act
        var entity = new TestDomainEntityInt(42);

        // Assert
        entity.Should().BeAssignableTo<IEquatable<DomainEntity<int>>>();
    }

    #endregion

    #region Does NOT implement IBusinessEntity

    [Fact]
    public void DomainEntity_DoesNotImplement_IBusinessEntity()
    {
        // Arrange & Act
        var entity = new TestDomainEntityInt(42);

        // Assert — DomainEntity is intentionally NOT an IBusinessEntity
        entity.Should().NotBeAssignableTo<IBusinessEntity>();
    }

    #endregion
}
```

- [ ] **Step 13: Run test to verify it fails**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~DomainEntityTests" --no-restore -v quiet`
Expected: Build failure — `DomainEntity<TKey>` type does not exist yet.

### Task 6: DomainEntity — Implement

**Files:**
- Create: `Src/RCommon.Entities/DomainEntity.cs`

- [ ] **Step 14: Create DomainEntity.cs**

```csharp
// Src/RCommon.Entities/DomainEntity.cs
namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base class for domain entities within an aggregate. Provides identity-based equality
    /// but no event tracking — entities within an aggregate raise events through their aggregate root.
    /// Because DomainEntity does not implement IBusinessEntity, the ObjectGraphWalker in
    /// InMemoryEntityEventTracker will not traverse it. All domain events must be raised on the
    /// aggregate root.
    /// </summary>
    /// <typeparam name="TKey">The type of the entity's identity.</typeparam>
    [Serializable]
    public abstract class DomainEntity<TKey> : IEquatable<DomainEntity<TKey>>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// The unique identity of this entity.
        /// </summary>
        public virtual TKey Id { get; protected set; } = default!;

        public bool Equals(DomainEntity<TKey>? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            if (GetType() != other.GetType())
                return false;

            if (IsTransient() || other.IsTransient())
                return false;

            return Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
            => Equals(obj as DomainEntity<TKey>);

        public override int GetHashCode()
        {
            var id = Id;
            if (id is null || id.Equals(default(TKey)))
                return base.GetHashCode();
            return id.GetHashCode();
        }

        /// <summary>
        /// Returns true if this entity has not yet been assigned a persistent identity.
        /// </summary>
        public bool IsTransient()
            => Id is null || Id.Equals(default);

        public static bool operator ==(DomainEntity<TKey>? left, DomainEntity<TKey>? right)
        {
            if (left is null && right is null)
                return true;
            if (left is null || right is null)
                return false;
            return left.Equals(right);
        }

        public static bool operator !=(DomainEntity<TKey>? left, DomainEntity<TKey>? right)
            => !(left == right);
    }
}
```

- [ ] **Step 15: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~DomainEntityTests" --no-restore -v quiet`
Expected: All 28 tests PASS.

- [ ] **Step 16: Commit**

```bash
git add Src/RCommon.Entities/DomainEntity.cs Tests/RCommon.Entities.Tests/DomainEntityTests.cs
git commit -m "feat: add DomainEntity<TKey> base class

Lightweight entity with identity-based equality and IEquatable support.
No event tracking — child entities raise events through aggregate root."
```

---

## Chunk 4: AggregateRoot

### Task 7: AggregateRoot — Write failing tests

**Files:**
- Test: `Tests/RCommon.Entities.Tests/AggregateRootTests.cs`

- [ ] **Step 17: Create the test file**

```csharp
// Tests/RCommon.Entities.Tests/AggregateRootTests.cs
using Bogus;
using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for AggregateRoot{TKey}, IAggregateRoot, and IAggregateRoot{TKey}.
/// </summary>
public class AggregateRootTests
{
    private readonly Faker _faker;

    public AggregateRootTests()
    {
        _faker = new Faker();
    }

    #region Test Types

    /// <summary>
    /// Concrete aggregate root for testing with int key.
    /// Exposes protected methods for test access.
    /// </summary>
    private class TestAggregateInt : AggregateRoot<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestAggregateInt() : base() { }

        public TestAggregateInt(int id) : base(id) { }

        /// <summary>
        /// Public wrapper for the protected AddDomainEvent method.
        /// </summary>
        public void RaiseDomainEvent(IDomainEvent domainEvent)
            => AddDomainEvent(domainEvent);

        /// <summary>
        /// Public wrapper for the protected RemoveDomainEvent method.
        /// </summary>
        public void UndoDomainEvent(IDomainEvent domainEvent)
            => RemoveDomainEvent(domainEvent);

        /// <summary>
        /// Public wrapper for the protected IncrementVersion method.
        /// </summary>
        public void BumpVersion()
            => IncrementVersion();
    }

    private class TestAggregateGuid : AggregateRoot<Guid>
    {
        public TestAggregateGuid() : base() { }

        public TestAggregateGuid(Guid id) : base(id) { }

        public void RaiseDomainEvent(IDomainEvent domainEvent)
            => AddDomainEvent(domainEvent);
    }

    private record TestDomainEvent(string Message) : DomainEvent;

    private record TestOtherDomainEvent(int Code) : DomainEvent;

    #endregion

    #region Interface Conformance Tests

    [Fact]
    public void AggregateRoot_Implements_IAggregateRoot()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt(1);

        // Assert
        aggregate.Should().BeAssignableTo<IAggregateRoot>();
    }

    [Fact]
    public void AggregateRoot_Implements_IAggregateRootGeneric()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt(1);

        // Assert
        aggregate.Should().BeAssignableTo<IAggregateRoot<int>>();
    }

    [Fact]
    public void AggregateRoot_Implements_IBusinessEntity()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt(1);

        // Assert
        aggregate.Should().BeAssignableTo<IBusinessEntity>();
    }

    [Fact]
    public void AggregateRoot_Implements_IBusinessEntityGeneric()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt(1);

        // Assert
        aggregate.Should().BeAssignableTo<IBusinessEntity<int>>();
    }

    #endregion

    #region Identity Tests

    [Fact]
    public void AggregateRoot_ConstructorWithId_SetsId()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt(42);

        // Assert
        aggregate.Id.Should().Be(42);
    }

    [Fact]
    public void AggregateRoot_DefaultConstructor_IdIsDefault()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt();

        // Assert
        aggregate.Id.Should().Be(0);
    }

    [Fact]
    public void AggregateRoot_GuidKey_SetsId()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var aggregate = new TestAggregateGuid(id);

        // Assert
        aggregate.Id.Should().Be(id);
    }

    #endregion

    #region Version Tests

    [Fact]
    public void AggregateRoot_DefaultVersion_IsZero()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt(1);

        // Assert
        aggregate.Version.Should().Be(0);
    }

    [Fact]
    public void IncrementVersion_IncrementsVersionByOne()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);

        // Act
        aggregate.BumpVersion();

        // Assert
        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void IncrementVersion_CalledMultipleTimes_VersionIncrementsCorrectly()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);

        // Act
        aggregate.BumpVersion();
        aggregate.BumpVersion();
        aggregate.BumpVersion();

        // Assert
        aggregate.Version.Should().Be(3);
    }

    #endregion

    #region Domain Event Add/Remove/Clear Tests

    [Fact]
    public void AddDomainEvent_AddsEventToDomainEvents()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");

        // Act
        aggregate.RaiseDomainEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_MultipleEvents_AllPresent()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);
        var event1 = new TestDomainEvent("first");
        var event2 = new TestOtherDomainEvent(42);

        // Act
        aggregate.RaiseDomainEvent(event1);
        aggregate.RaiseDomainEvent(event2);

        // Assert
        aggregate.DomainEvents.Should().HaveCount(2);
        aggregate.DomainEvents.Should().Contain(event1);
        aggregate.DomainEvents.Should().Contain(event2);
    }

    [Fact]
    public void RemoveDomainEvent_RemovesEventFromDomainEvents()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");
        aggregate.RaiseDomainEvent(domainEvent);

        // Act
        aggregate.UndoDomainEvent(domainEvent);

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);
        aggregate.RaiseDomainEvent(new TestDomainEvent("first"));
        aggregate.RaiseDomainEvent(new TestOtherDomainEvent(42));

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_WhenEmpty_ReturnsEmptyCollection()
    {
        // Arrange & Act
        var aggregate = new TestAggregateInt(1);

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.DomainEvents.Should().NotBeNull();
    }

    #endregion

    #region Dual-List Sync Tests (DomainEvents + LocalEvents)

    [Fact]
    public void AddDomainEvent_AlsoAppearsInLocalEvents()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");

        // Act
        aggregate.RaiseDomainEvent(domainEvent);

        // Assert — event appears in both collections
        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
        aggregate.LocalEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
    }

    [Fact]
    public void RemoveDomainEvent_AlsoRemovesFromLocalEvents()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");
        aggregate.RaiseDomainEvent(domainEvent);

        // Act
        aggregate.UndoDomainEvent(domainEvent);

        // Assert — event removed from both collections
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.LocalEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_AlsoClearsLocalEvents()
    {
        // Arrange
        var aggregate = new TestAggregateInt(1);
        aggregate.RaiseDomainEvent(new TestDomainEvent("one"));
        aggregate.RaiseDomainEvent(new TestDomainEvent("two"));

        // Act
        aggregate.ClearDomainEvents();

        // Assert — both collections cleared
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.LocalEvents.Should().BeEmpty();
    }

    #endregion

    #region Event Pipeline Integration Tests

    [Fact]
    public async Task DomainEvents_FlowThrough_EntityEventTracker()
    {
        // Arrange
        var mockEventRouter = new Mock<IEventRouter>();
        mockEventRouter.Setup(x => x.RouteEventsAsync()).Returns(Task.CompletedTask);
        var tracker = new InMemoryEntityEventTracker(mockEventRouter.Object);

        var aggregate = new TestAggregateInt(1);
        aggregate.AllowEventTracking = true;
        var domainEvent = new TestDomainEvent("integration test");
        aggregate.RaiseDomainEvent(domainEvent);

        // Act
        tracker.AddEntity(aggregate);
        await tracker.EmitTransactionalEventsAsync();

        // Assert — the domain event (which IS-A ISerializableEvent) was routed
        mockEventRouter.Verify(
            x => x.AddTransactionalEvents(It.Is<IEnumerable<ISerializableEvent>>(
                events => events.Contains(domainEvent))),
            Times.AtLeastOnce);
        mockEventRouter.Verify(x => x.RouteEventsAsync(), Times.Once);
    }

    #endregion

    #region Inherited BusinessEntity Behavior Tests

    [Fact]
    public void AggregateRoot_GetKeys_ReturnsId()
    {
        // Arrange
        var aggregate = new TestAggregateInt(42);

        // Act
        var keys = aggregate.GetKeys();

        // Assert
        keys.Should().ContainSingle().Which.Should().Be(42);
    }

    [Fact]
    public void AggregateRoot_EntityEquals_SameId_ReturnsTrue()
    {
        // Arrange
        var aggregate1 = new TestAggregateInt(42);
        var aggregate2 = new TestAggregateInt(42);

        // Act & Assert
        aggregate1.EntityEquals(aggregate2).Should().BeTrue();
    }

    #endregion
}
```

- [ ] **Step 18: Run test to verify it fails**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~AggregateRootTests" --no-restore -v quiet`
Expected: Build failure — `AggregateRoot<TKey>`, `IAggregateRoot`, `IAggregateRoot<TKey>` types do not exist yet.

### Task 8: AggregateRoot — Implement

**Files:**
- Create: `Src/RCommon.Entities/IAggregateRoot.cs`
- Create: `Src/RCommon.Entities/AggregateRoot.cs`

- [ ] **Step 19: Create IAggregateRoot.cs**

```csharp
// Src/RCommon.Entities/IAggregateRoot.cs
namespace RCommon.Entities
{
    /// <summary>
    /// Non-generic marker interface for aggregate roots.
    /// Useful for infrastructure scenarios such as repository filtering, middleware, and generic constraints.
    /// </summary>
    public interface IAggregateRoot : IBusinessEntity
    {
        /// <summary>
        /// The version number used for optimistic concurrency control.
        /// </summary>
        int Version { get; }

        /// <summary>
        /// The collection of domain events raised by this aggregate that have not yet been dispatched.
        /// </summary>
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    }

    /// <summary>
    /// Generic interface for aggregate roots in the domain model.
    /// Extends IBusinessEntity to maintain compatibility with existing repository and event tracking infrastructure.
    /// Note: The IEquatable constraint is stricter than IBusinessEntity&lt;TKey&gt; — this is intentional
    /// because aggregate roots require identity equality for consistency guarantees.
    /// </summary>
    public interface IAggregateRoot<TKey> : IAggregateRoot, IBusinessEntity<TKey>
        where TKey : IEquatable<TKey>
    {
    }
}
```

- [ ] **Step 20: Create AggregateRoot.cs**

```csharp
// Src/RCommon.Entities/AggregateRoot.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RCommon.Entities
{
    /// <summary>
    /// Abstract base class for aggregate roots. Extends BusinessEntity to reuse event tracking,
    /// key support, and entity equality. Adds versioning for optimistic concurrency and typed
    /// domain event methods.
    /// </summary>
    /// <typeparam name="TKey">The type of the aggregate's identity.</typeparam>
    [Serializable]
    public abstract class AggregateRoot<TKey> : BusinessEntity<TKey>, IAggregateRoot<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        /// <summary>
        /// Version number for optimistic concurrency control. Incremented via <see cref="IncrementVersion"/>.
        /// Decorated with [ConcurrencyCheck] to signal ORM-level concurrency checking.
        /// </summary>
        [ConcurrencyCheck]
        public virtual int Version { get; protected set; }

        /// <summary>
        /// Returns the domain events that have been raised by this aggregate but not yet dispatched.
        /// </summary>
        [NotMapped]
        public IReadOnlyCollection<IDomainEvent> DomainEvents
            => _domainEvents.AsReadOnly();

        /// <summary>
        /// Raises a domain event on this aggregate. The event is added to both the DomainEvents
        /// collection and the base LocalEvents collection for dispatch via the event tracking pipeline.
        /// </summary>
        protected void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
            AddLocalEvent(domainEvent);
        }

        /// <summary>
        /// Removes a previously raised domain event before it has been dispatched.
        /// </summary>
        protected void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
            RemoveLocalEvent(domainEvent);
        }

        /// <summary>
        /// Clears all pending domain events from this aggregate.
        /// </summary>
        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
            ClearLocalEvents();
        }

        /// <summary>
        /// Increments the version number for optimistic concurrency control.
        /// Call this when the aggregate's state changes.
        /// Note: This is not thread-safe. Aggregates are designed for single-threaded access.
        /// </summary>
        protected void IncrementVersion()
            => Version++;
    }
}
```

- [ ] **Step 21: Run tests to verify they pass**

Run: `dotnet test Tests/RCommon.Entities.Tests/ --filter "FullyQualifiedName~AggregateRootTests" --no-restore -v quiet`
Expected: All 21 tests PASS.

- [ ] **Step 22: Commit**

```bash
git add Src/RCommon.Entities/IAggregateRoot.cs Src/RCommon.Entities/AggregateRoot.cs Tests/RCommon.Entities.Tests/AggregateRootTests.cs
git commit -m "feat: add AggregateRoot<TKey> and IAggregateRoot interfaces

Extends BusinessEntity<TKey> with domain event management, versioning,
and optimistic concurrency. Domain events flow through existing
IEntityEventTracker pipeline via AddLocalEvent delegation."
```

---

## Chunk 5: Full Test Suite Verification

### Task 9: Run all existing tests to confirm no regressions

- [ ] **Step 23: Run the full RCommon.Entities.Tests suite**

Run: `dotnet test Tests/RCommon.Entities.Tests/ -v quiet`
Expected: All tests pass (existing + new). Zero failures.

- [ ] **Step 24: Build the entire solution to verify no compilation errors**

Run: `dotnet build Src/RCommon.sln --no-restore -v quiet`
Expected: Build succeeded. 0 errors.

- [ ] **Step 25: Final commit (if any formatting/cleanup needed)**

Only commit if the build or tests required any adjustments. If everything passed cleanly, skip this step.
