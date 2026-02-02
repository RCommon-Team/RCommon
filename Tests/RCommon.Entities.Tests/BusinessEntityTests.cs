using Bogus;
using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for BusinessEntity and BusinessEntity{TKey} classes.
/// </summary>
public class BusinessEntityTests
{
    private readonly Faker _faker;

    public BusinessEntityTests()
    {
        _faker = new Faker();
    }

    #region Test Entities

    /// <summary>
    /// Concrete implementation of BusinessEntity{TKey} for testing with int key.
    /// </summary>
    private class TestEntityInt : BusinessEntity<int>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntityInt() : base() { }

        public TestEntityInt(int id) : base(id) { }
    }

    /// <summary>
    /// Concrete implementation of BusinessEntity{TKey} for testing with Guid key.
    /// </summary>
    private class TestEntityGuid : BusinessEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntityGuid() : base() { }

        public TestEntityGuid(Guid id) : base(id) { }
    }

    /// <summary>
    /// Concrete implementation of BusinessEntity{TKey} for testing with string key.
    /// </summary>
    private class TestEntityString : BusinessEntity<string>
    {
        public string Name { get; set; } = string.Empty;

        public TestEntityString() : base() { }

        public TestEntityString(string id) : base(id) { }
    }

    /// <summary>
    /// Test event implementing ISerializableEvent.
    /// </summary>
    private class TestSerializableEvent : ISerializableEvent
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void BusinessEntity_DefaultConstructor_InitializesLocalEventsCollection()
    {
        // Arrange & Act
        var entity = new TestEntityInt();

        // Assert
        entity.LocalEvents.Should().NotBeNull();
        entity.LocalEvents.Should().BeEmpty();
    }

    [Fact]
    public void BusinessEntity_ConstructorWithIntId_SetsIdCorrectly()
    {
        // Arrange
        var expectedId = _faker.Random.Int(1, 1000);

        // Act
        var entity = new TestEntityInt(expectedId);

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    [Fact]
    public void BusinessEntity_ConstructorWithGuidId_SetsIdCorrectly()
    {
        // Arrange
        var expectedId = _faker.Random.Guid();

        // Act
        var entity = new TestEntityGuid(expectedId);

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    [Fact]
    public void BusinessEntity_ConstructorWithStringId_SetsIdCorrectly()
    {
        // Arrange
        var expectedId = _faker.Random.AlphaNumeric(10);

        // Act
        var entity = new TestEntityString(expectedId);

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    #endregion

    #region AllowEventTracking Tests

    [Fact]
    public void AllowEventTracking_DefaultValue_IsTrue()
    {
        // Arrange & Act
        var entity = new TestEntityInt();

        // Assert
        entity.AllowEventTracking.Should().BeTrue();
    }

    [Fact]
    public void AllowEventTracking_SetToFalse_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityInt();

        // Act
        entity.AllowEventTracking = false;

        // Assert
        entity.AllowEventTracking.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AllowEventTracking_SetValue_ReturnsCorrectValue(bool expectedValue)
    {
        // Arrange
        var entity = new TestEntityInt();

        // Act
        entity.AllowEventTracking = expectedValue;

        // Assert
        entity.AllowEventTracking.Should().Be(expectedValue);
    }

    #endregion

    #region GetKeys Tests

    [Fact]
    public void GetKeys_ReturnsArrayContainingId()
    {
        // Arrange
        var expectedId = _faker.Random.Int(1, 1000);
        var entity = new TestEntityInt(expectedId);

        // Act
        var keys = entity.GetKeys();

        // Assert
        keys.Should().HaveCount(1);
        keys[0].Should().Be(expectedId);
    }

    [Fact]
    public void GetKeys_WithGuidId_ReturnsCorrectKey()
    {
        // Arrange
        var expectedId = _faker.Random.Guid();
        var entity = new TestEntityGuid(expectedId);

        // Act
        var keys = entity.GetKeys();

        // Assert
        keys.Should().HaveCount(1);
        keys[0].Should().Be(expectedId);
    }

    [Fact]
    public void GetKeys_WithStringId_ReturnsCorrectKey()
    {
        // Arrange
        var expectedId = _faker.Random.AlphaNumeric(10);
        var entity = new TestEntityString(expectedId);

        // Act
        var keys = entity.GetKeys();

        // Assert
        keys.Should().HaveCount(1);
        keys[0].Should().Be(expectedId);
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WithTypedEntity_ReturnsFormattedString()
    {
        // Arrange
        var id = _faker.Random.Int(1, 1000);
        var entity = new TestEntityInt(id);

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Contain("ENTITY:");
        result.Should().Contain("TestEntityInt");
        result.Should().Contain($"Id = {id}");
    }

    [Fact]
    public void ToString_WithGuidId_ReturnsFormattedString()
    {
        // Arrange
        var id = _faker.Random.Guid();
        var entity = new TestEntityGuid(id);

        // Act
        var result = entity.ToString();

        // Assert
        result.Should().Contain("ENTITY:");
        result.Should().Contain("TestEntityGuid");
        result.Should().Contain($"Id = {id}");
    }

    #endregion

    #region LocalEvents Tests

    [Fact]
    public void AddLocalEvent_AddsEventToCollection()
    {
        // Arrange
        var entity = new TestEntityInt();
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };

        // Act
        entity.AddLocalEvent(testEvent);

        // Assert
        entity.LocalEvents.Should().HaveCount(1);
        entity.LocalEvents.Should().Contain(testEvent);
    }

    [Fact]
    public void AddLocalEvent_MultipleEvents_AddsAllEvents()
    {
        // Arrange
        var entity = new TestEntityInt();
        var events = Enumerable.Range(0, 5)
            .Select(_ => new TestSerializableEvent { EventName = _faker.Lorem.Word() })
            .ToList();

        // Act
        foreach (var testEvent in events)
        {
            entity.AddLocalEvent(testEvent);
        }

        // Assert
        entity.LocalEvents.Should().HaveCount(5);
        foreach (var testEvent in events)
        {
            entity.LocalEvents.Should().Contain(testEvent);
        }
    }

    [Fact]
    public void RemoveLocalEvent_RemovesEventFromCollection()
    {
        // Arrange
        var entity = new TestEntityInt();
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        entity.AddLocalEvent(testEvent);

        // Act
        entity.RemoveLocalEvent(testEvent);

        // Assert
        entity.LocalEvents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLocalEvent_RemovesSpecificEvent_LeavesOthers()
    {
        // Arrange
        var entity = new TestEntityInt();
        var event1 = new TestSerializableEvent { EventName = "Event1" };
        var event2 = new TestSerializableEvent { EventName = "Event2" };
        var event3 = new TestSerializableEvent { EventName = "Event3" };
        entity.AddLocalEvent(event1);
        entity.AddLocalEvent(event2);
        entity.AddLocalEvent(event3);

        // Act
        entity.RemoveLocalEvent(event2);

        // Assert
        entity.LocalEvents.Should().HaveCount(2);
        entity.LocalEvents.Should().Contain(event1);
        entity.LocalEvents.Should().Contain(event3);
        entity.LocalEvents.Should().NotContain(event2);
    }

    [Fact]
    public void ClearLocalEvents_RemovesAllEvents()
    {
        // Arrange
        var entity = new TestEntityInt();
        var events = Enumerable.Range(0, 5)
            .Select(_ => new TestSerializableEvent { EventName = _faker.Lorem.Word() })
            .ToList();
        foreach (var testEvent in events)
        {
            entity.AddLocalEvent(testEvent);
        }

        // Act
        entity.ClearLocalEvents();

        // Assert
        entity.LocalEvents.Should().BeEmpty();
    }

    [Fact]
    public void LocalEvents_IsReadOnlyCollection()
    {
        // Arrange
        var entity = new TestEntityInt();

        // Assert
        entity.LocalEvents.Should().BeAssignableTo<IReadOnlyCollection<ISerializableEvent>>();
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public void AddLocalEvent_RaisesTransactionalEventAddedEvent()
    {
        // Arrange
        var entity = new TestEntityInt();
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        TransactionalEventsChangedEventArgs? capturedArgs = null;
        entity.TransactionalEventAdded += (sender, args) => capturedArgs = args;

        // Act
        entity.AddLocalEvent(testEvent);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
        capturedArgs.EventData.Should().Be(testEvent);
    }

    [Fact]
    public void RemoveLocalEvent_RaisesTransactionalEventRemovedEvent()
    {
        // Arrange
        var entity = new TestEntityInt();
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        entity.AddLocalEvent(testEvent);
        TransactionalEventsChangedEventArgs? capturedArgs = null;
        entity.TransactionalEventRemoved += (sender, args) => capturedArgs = args;

        // Act
        entity.RemoveLocalEvent(testEvent);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Entity.Should().Be(entity);
        capturedArgs.EventData.Should().Be(testEvent);
    }

    [Fact]
    public void ClearLocalEvents_RaisesTransactionalEventsClearedEvent()
    {
        // Arrange
        var entity = new TestEntityInt();
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
    public void EventHandlers_NotSubscribed_DoesNotThrow()
    {
        // Arrange
        var entity = new TestEntityInt();
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };

        // Act & Assert - Should not throw even without subscribers
        var addAction = () => entity.AddLocalEvent(testEvent);
        var removeAction = () => entity.RemoveLocalEvent(testEvent);
        var clearAction = () => entity.ClearLocalEvents();

        addAction.Should().NotThrow();
        removeAction.Should().NotThrow();
        clearAction.Should().NotThrow();
    }

    [Fact]
    public void TransactionalEventAdded_MultipleSubscribers_AllReceiveNotification()
    {
        // Arrange
        var entity = new TestEntityInt();
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        var notificationCount = 0;
        entity.TransactionalEventAdded += (sender, args) => notificationCount++;
        entity.TransactionalEventAdded += (sender, args) => notificationCount++;

        // Act
        entity.AddLocalEvent(testEvent);

        // Assert
        notificationCount.Should().Be(2);
    }

    #endregion

    #region EntityEquals Tests

    [Fact]
    public void EntityEquals_SameInstance_ReturnsTrue()
    {
        // Arrange
        var entity = new TestEntityInt(_faker.Random.Int(1, 1000));

        // Act
        var result = entity.EntityEquals(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EntityEquals_NullEntity_ReturnsFalse()
    {
        // Arrange
        var entity = new TestEntityInt(_faker.Random.Int(1, 1000));

        // Act
        var result = entity.EntityEquals(null!);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Id Property Tests

    [Fact]
    public void Id_DefaultValue_IsDefaultForType()
    {
        // Arrange & Act
        var intEntity = new TestEntityInt();
        var guidEntity = new TestEntityGuid();
        var stringEntity = new TestEntityString();

        // Assert
        intEntity.Id.Should().Be(default(int));
        guidEntity.Id.Should().Be(default(Guid));
        stringEntity.Id.Should().BeNull();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void Id_WithVariousIntValues_ReturnsCorrectValue(int expectedId)
    {
        // Arrange & Act
        var entity = new TestEntityInt(expectedId);

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    #endregion
}
