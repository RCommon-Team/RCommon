using Bogus;
using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for TransactionalEventsClearedEventArgs class.
/// </summary>
public class TransactionalEventsClearedEventArgsTests
{
    private readonly Faker _faker;

    public TransactionalEventsClearedEventArgsTests()
    {
        _faker = new Faker();
    }

    #region Test Entities

    /// <summary>
    /// Concrete implementation of BusinessEntity{TKey} for testing.
    /// </summary>
    private class TestEntity : BusinessEntity<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntity() : base() { }

        public TestEntity(int id) : base(id)
        {
            Name = $"Entity_{id}";
        }
    }

    /// <summary>
    /// Test entity with Guid key.
    /// </summary>
    private class TestEntityGuid : BusinessEntity<Guid>
    {
        public string Description { get; set; } = string.Empty;

        public TestEntityGuid() : base() { }

        public TestEntityGuid(Guid id) : base(id) { }
    }

    /// <summary>
    /// Test event implementing ISerializableEvent.
    /// </summary>
    private class TestSerializableEvent : ISerializableEvent
    {
        public string EventName { get; set; } = string.Empty;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidEntity_CreatesInstance()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        // Act
        var args = new TransactionalEventsClearedEventArgs(entity);

        // Assert
        args.Should().NotBeNull();
        args.Entity.Should().Be(entity);
    }

    [Fact]
    public void Constructor_WithNullEntity_AllowsNullEntity()
    {
        // Arrange & Act
        var args = new TransactionalEventsClearedEventArgs(null!);

        // Assert
        args.Entity.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDifferentEntityTypes_WorksCorrectly()
    {
        // Arrange
        var intEntity = new TestEntity(_faker.Random.Int(1, 1000));
        var guidEntity = new TestEntityGuid(_faker.Random.Guid());

        // Act
        var argsInt = new TransactionalEventsClearedEventArgs(intEntity);
        var argsGuid = new TransactionalEventsClearedEventArgs(guidEntity);

        // Assert
        argsInt.Entity.Should().Be(intEntity);
        argsGuid.Entity.Should().Be(guidEntity);
    }

    #endregion

    #region Entity Property Tests

    [Fact]
    public void Entity_ReturnsCorrectEntity()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var args = new TransactionalEventsClearedEventArgs(entity);

        // Act
        var result = args.Entity;

        // Assert
        result.Should().BeSameAs(entity);
    }

    [Fact]
    public void Entity_IsReadOnly()
    {
        // Arrange
        var entityProperty = typeof(TransactionalEventsClearedEventArgs).GetProperty("Entity");

        // Assert
        entityProperty.Should().NotBeNull();
        entityProperty!.CanRead.Should().BeTrue();
        entityProperty.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void Entity_ReturnsIBusinessEntity()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var args = new TransactionalEventsClearedEventArgs(entity);

        // Assert
        args.Entity.Should().BeAssignableTo<IBusinessEntity>();
    }

    [Fact]
    public void Entity_PreservesEntityProperties()
    {
        // Arrange
        var entityId = _faker.Random.Int(1, 1000);
        var entityName = _faker.Lorem.Word();
        var entity = new TestEntity(entityId) { Name = entityName };
        var args = new TransactionalEventsClearedEventArgs(entity);

        // Act
        var retrievedEntity = args.Entity as TestEntity;

        // Assert
        retrievedEntity.Should().NotBeNull();
        retrievedEntity!.Id.Should().Be(entityId);
        retrievedEntity.Name.Should().Be(entityName);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void TransactionalEventsClearedEventArgs_InheritsFromEventArgs()
    {
        // Arrange
        var entity = new TestEntity();

        // Act
        var args = new TransactionalEventsClearedEventArgs(entity);

        // Assert
        args.Should().BeAssignableTo<EventArgs>();
    }

    #endregion

    #region Usage With Event Handler Tests

    [Fact]
    public void EventArgs_CanBeUsedWithEventHandler()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        TransactionalEventsClearedEventArgs? capturedArgs = null;

        EventHandler<TransactionalEventsClearedEventArgs> handler = (sender, args) =>
        {
            capturedArgs = args;
        };

        // Act
        handler(entity, new TransactionalEventsClearedEventArgs(entity));

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
    }

    [Fact]
    public void EventArgs_CanBeUsedWithBusinessEntityClearEvents()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        entity.AddLocalEvent(testEvent);
        TransactionalEventsClearedEventArgs? capturedArgs = null;

        entity.TransactionalEventsCleared += (sender, args) => capturedArgs = args;

        // Act
        entity.ClearLocalEvents();

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
    }

    [Fact]
    public void EventArgs_RaisedWhenClearingEmptyEventsList()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        // Not adding any events
        TransactionalEventsClearedEventArgs? capturedArgs = null;

        entity.TransactionalEventsCleared += (sender, args) => capturedArgs = args;

        // Act
        entity.ClearLocalEvents();

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
    }

    [Fact]
    public void EventArgs_RaisedMultipleTimesWhenClearingMultipleTimes()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var clearCount = 0;

        entity.TransactionalEventsCleared += (sender, args) => clearCount++;

        // Act
        entity.ClearLocalEvents();
        entity.AddLocalEvent(new TestSerializableEvent { EventName = "Event1" });
        entity.ClearLocalEvents();
        entity.ClearLocalEvents();

        // Assert
        clearCount.Should().Be(3);
    }

    #endregion

    #region Mock Entity Tests

    [Fact]
    public void Constructor_WithMockedEntity_WorksCorrectly()
    {
        // Arrange
        var mockEntity = new Mock<IBusinessEntity>();
        mockEntity.Setup(e => e.GetKeys()).Returns(new object[] { 1 });

        // Act
        var args = new TransactionalEventsClearedEventArgs(mockEntity.Object);

        // Assert
        args.Entity.Should().Be(mockEntity.Object);
    }

    [Fact]
    public void MockedEntity_CanAccessEntityMethods()
    {
        // Arrange
        var expectedKeys = new object[] { 42, "test" };
        var mockEntity = new Mock<IBusinessEntity>();
        mockEntity.Setup(e => e.GetKeys()).Returns(expectedKeys);

        // Act
        var args = new TransactionalEventsClearedEventArgs(mockEntity.Object);

        // Assert
        args.Entity.GetKeys().Should().BeEquivalentTo(expectedKeys);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void MultipleInstances_WithSameEntity_AreNotSameReference()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        // Act
        var args1 = new TransactionalEventsClearedEventArgs(entity);
        var args2 = new TransactionalEventsClearedEventArgs(entity);

        // Assert
        args1.Should().NotBeSameAs(args2);
        args1.Entity.Should().BeSameAs(args2.Entity);
    }

    [Fact]
    public void MultipleInstances_WithDifferentEntities_HaveDifferentEntityReferences()
    {
        // Arrange
        var entity1 = new TestEntity(1);
        var entity2 = new TestEntity(2);

        // Act
        var args1 = new TransactionalEventsClearedEventArgs(entity1);
        var args2 = new TransactionalEventsClearedEventArgs(entity2);

        // Assert
        args1.Entity.Should().NotBeSameAs(args2.Entity);
    }

    [Fact]
    public void Entity_WithLocalEvents_CanBeCleared()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var events = Enumerable.Range(0, 5)
            .Select(_ => new TestSerializableEvent { EventName = _faker.Lorem.Word() })
            .ToList();
        foreach (var evt in events)
        {
            entity.AddLocalEvent(evt);
        }
        TransactionalEventsClearedEventArgs? capturedArgs = null;

        entity.TransactionalEventsCleared += (sender, args) => capturedArgs = args;

        // Act
        entity.ClearLocalEvents();

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
        entity.LocalEvents.Should().BeEmpty();
    }

    #endregion

    #region Comparison With TransactionalEventsChangedEventArgs Tests

    [Fact]
    public void ClearedEventArgs_DoesNotContainEventData()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        // Act
        var clearedArgs = new TransactionalEventsClearedEventArgs(entity);

        // Assert - TransactionalEventsClearedEventArgs only has Entity property
        var properties = typeof(TransactionalEventsClearedEventArgs).GetProperties();
        properties.Should().HaveCount(1);
        properties.First().Name.Should().Be("Entity");
    }

    [Fact]
    public void ClearedEventArgs_HasFewerProperties_ThanChangedEventArgs()
    {
        // Assert
        var clearedProperties = typeof(TransactionalEventsClearedEventArgs).GetProperties();
        var changedProperties = typeof(TransactionalEventsChangedEventArgs).GetProperties();

        clearedProperties.Length.Should().BeLessThan(changedProperties.Length);
    }

    #endregion

    #region Sender Tests

    [Fact]
    public void EventHandler_ReceivesCorrectSender()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        object? capturedSender = null;

        entity.TransactionalEventsCleared += (sender, args) => capturedSender = sender;

        // Act
        entity.ClearLocalEvents();

        // Assert
        capturedSender.Should().Be(entity);
    }

    [Fact]
    public void EventHandler_SenderMatchesArgsEntity()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        object? capturedSender = null;
        TransactionalEventsClearedEventArgs? capturedArgs = null;

        entity.TransactionalEventsCleared += (sender, args) =>
        {
            capturedSender = sender;
            capturedArgs = args;
        };

        // Act
        entity.ClearLocalEvents();

        // Assert
        capturedSender.Should().BeSameAs(capturedArgs!.Entity);
    }

    #endregion
}
