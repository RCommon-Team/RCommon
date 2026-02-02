using Bogus;
using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for TransactionalEventsChangedEventArgs class.
/// </summary>
public class TransactionalEventsChangedEventArgsTests
{
    private readonly Faker _faker;

    public TransactionalEventsChangedEventArgsTests()
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
    /// Test event implementing ISerializableEvent.
    /// </summary>
    private class TestSerializableEvent : ISerializableEvent
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Another test event implementing ISerializableEvent.
    /// </summary>
    private class AnotherTestEvent : ISerializableEvent
    {
        public int EventId { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };

        // Act
        var args = new TransactionalEventsChangedEventArgs(entity, eventData);

        // Assert
        args.Should().NotBeNull();
        args.Entity.Should().Be(entity);
        args.EventData.Should().Be(eventData);
    }

    [Fact]
    public void Constructor_WithNullEntity_AllowsNullEntity()
    {
        // Arrange
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };

        // Act
        var args = new TransactionalEventsChangedEventArgs(null!, eventData);

        // Assert
        args.Entity.Should().BeNull();
        args.EventData.Should().Be(eventData);
    }

    [Fact]
    public void Constructor_WithNullEventData_AllowsNullEventData()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        // Act
        var args = new TransactionalEventsChangedEventArgs(entity, null!);

        // Assert
        args.Entity.Should().Be(entity);
        args.EventData.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithBothNull_AllowsBothNull()
    {
        // Arrange & Act
        var args = new TransactionalEventsChangedEventArgs(null!, null!);

        // Assert
        args.Entity.Should().BeNull();
        args.EventData.Should().BeNull();
    }

    #endregion

    #region Entity Property Tests

    [Fact]
    public void Entity_ReturnsCorrectEntity()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        var args = new TransactionalEventsChangedEventArgs(entity, eventData);

        // Act
        var result = args.Entity;

        // Assert
        result.Should().BeSameAs(entity);
    }

    [Fact]
    public void Entity_IsReadOnly()
    {
        // Arrange
        var entityProperty = typeof(TransactionalEventsChangedEventArgs).GetProperty("Entity");

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
        var eventData = new TestSerializableEvent();
        var args = new TransactionalEventsChangedEventArgs(entity, eventData);

        // Assert
        args.Entity.Should().BeAssignableTo<IBusinessEntity>();
    }

    #endregion

    #region EventData Property Tests

    [Fact]
    public void EventData_ReturnsCorrectEventData()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        var args = new TransactionalEventsChangedEventArgs(entity, eventData);

        // Act
        var result = args.EventData;

        // Assert
        result.Should().BeSameAs(eventData);
    }

    [Fact]
    public void EventData_IsReadOnly()
    {
        // Arrange
        var eventDataProperty = typeof(TransactionalEventsChangedEventArgs).GetProperty("EventData");

        // Assert
        eventDataProperty.Should().NotBeNull();
        eventDataProperty!.CanRead.Should().BeTrue();
        eventDataProperty.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void EventData_ReturnsISerializableEvent()
    {
        // Arrange
        var entity = new TestEntity();
        var eventData = new TestSerializableEvent();
        var args = new TransactionalEventsChangedEventArgs(entity, eventData);

        // Assert
        args.EventData.Should().BeAssignableTo<ISerializableEvent>();
    }

    [Fact]
    public void EventData_WithDifferentEventTypes_ReturnsCorrectType()
    {
        // Arrange
        var entity = new TestEntity();
        var eventData = new AnotherTestEvent { EventId = _faker.Random.Int(), Description = _faker.Lorem.Sentence() };
        var args = new TransactionalEventsChangedEventArgs(entity, eventData);

        // Assert
        args.EventData.Should().BeOfType<AnotherTestEvent>();
        ((AnotherTestEvent)args.EventData).EventId.Should().Be(eventData.EventId);
        ((AnotherTestEvent)args.EventData).Description.Should().Be(eventData.Description);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void TransactionalEventsChangedEventArgs_InheritsFromEventArgs()
    {
        // Arrange
        var entity = new TestEntity();
        var eventData = new TestSerializableEvent();

        // Act
        var args = new TransactionalEventsChangedEventArgs(entity, eventData);

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
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        TransactionalEventsChangedEventArgs? capturedArgs = null;

        EventHandler<TransactionalEventsChangedEventArgs> handler = (sender, args) =>
        {
            capturedArgs = args;
        };

        // Act
        handler(entity, new TransactionalEventsChangedEventArgs(entity, eventData));

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
        capturedArgs.EventData.Should().Be(eventData);
    }

    [Fact]
    public void EventArgs_CanBeUsedWithBusinessEntityEvents()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        TransactionalEventsChangedEventArgs? capturedArgs = null;

        entity.TransactionalEventAdded += (sender, args) => capturedArgs = args;

        // Act
        entity.AddLocalEvent(eventData);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
        capturedArgs.EventData.Should().Be(eventData);
    }

    [Fact]
    public void EventArgs_RemovedEventContainsCorrectData()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        entity.AddLocalEvent(eventData);
        TransactionalEventsChangedEventArgs? capturedArgs = null;

        entity.TransactionalEventRemoved += (sender, args) => capturedArgs = args;

        // Act
        entity.RemoveLocalEvent(eventData);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
        capturedArgs.EventData.Should().Be(eventData);
    }

    #endregion

    #region Mock Entity Tests

    [Fact]
    public void Constructor_WithMockedEntity_WorksCorrectly()
    {
        // Arrange
        var mockEntity = new Mock<IBusinessEntity>();
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };

        // Act
        var args = new TransactionalEventsChangedEventArgs(mockEntity.Object, eventData);

        // Assert
        args.Entity.Should().Be(mockEntity.Object);
        args.EventData.Should().Be(eventData);
    }

    [Fact]
    public void Constructor_WithMockedEventData_WorksCorrectly()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var mockEventData = new Mock<ISerializableEvent>();

        // Act
        var args = new TransactionalEventsChangedEventArgs(entity, mockEventData.Object);

        // Assert
        args.Entity.Should().Be(entity);
        args.EventData.Should().Be(mockEventData.Object);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void MultipleInstances_WithSameData_AreNotSameReference()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var eventData = new TestSerializableEvent { EventName = _faker.Lorem.Word() };

        // Act
        var args1 = new TransactionalEventsChangedEventArgs(entity, eventData);
        var args2 = new TransactionalEventsChangedEventArgs(entity, eventData);

        // Assert
        args1.Should().NotBeSameAs(args2);
        args1.Entity.Should().BeSameAs(args2.Entity);
        args1.EventData.Should().BeSameAs(args2.EventData);
    }

    [Fact]
    public void EventData_WithComplexEventType_PreservesAllProperties()
    {
        // Arrange
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var complexEvent = new TestSerializableEvent
        {
            EventName = _faker.Lorem.Word(),
            OccurredAt = _faker.Date.Recent()
        };

        // Act
        var args = new TransactionalEventsChangedEventArgs(entity, complexEvent);

        // Assert
        var retrievedEvent = args.EventData as TestSerializableEvent;
        retrievedEvent.Should().NotBeNull();
        retrievedEvent!.EventName.Should().Be(complexEvent.EventName);
        retrievedEvent.OccurredAt.Should().Be(complexEvent.OccurredAt);
    }

    #endregion
}
