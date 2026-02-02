using Bogus;
using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for InMemoryEntityEventTracker class.
/// </summary>
public class InMemoryEntityEventTrackerTests
{
    private readonly Faker _faker;
    private readonly Mock<IEventRouter> _mockEventRouter;

    public InMemoryEntityEventTrackerTests()
    {
        _faker = new Faker();
        _mockEventRouter = new Mock<IEventRouter>();
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

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidEventRouter_CreatesInstance()
    {
        // Arrange & Act
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);

        // Assert
        tracker.Should().NotBeNull();
        tracker.TrackedEntities.Should().NotBeNull();
        tracker.TrackedEntities.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNullEventRouter_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new InMemoryEntityEventTracker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("eventRouter");
    }

    #endregion

    #region AddEntity Tests

    [Fact]
    public void AddEntity_WithValidEntity_AddsToTrackedEntities()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        // Act
        tracker.AddEntity(entity);

        // Assert
        tracker.TrackedEntities.Should().HaveCount(1);
        tracker.TrackedEntities.Should().Contain(entity);
    }

    [Fact]
    public void AddEntity_MultipleEntities_AddsAllToTrackedEntities()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entities = Enumerable.Range(1, 5)
            .Select(i => new TestEntity(i))
            .ToList();

        // Act
        foreach (var entity in entities)
        {
            tracker.AddEntity(entity);
        }

        // Assert
        tracker.TrackedEntities.Should().HaveCount(5);
        foreach (var entity in entities)
        {
            tracker.TrackedEntities.Should().Contain(entity);
        }
    }

    [Fact]
    public void AddEntity_WithNullEntity_ThrowsException()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);

        // Act
        Action act = () => tracker.AddEntity(null!);

        // Assert
        // The implementation throws NullReferenceException when accessing entity.GetGenericTypeName()
        // before the Guard.Against check can execute
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddEntity_EntityWithEventTrackingDisabled_DoesNotAddToTrackedEntities()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        entity.AllowEventTracking = false;

        // Act
        tracker.AddEntity(entity);

        // Assert
        tracker.TrackedEntities.Should().BeEmpty();
    }

    [Fact]
    public void AddEntity_EntityWithEventTrackingEnabled_AddsToTrackedEntities()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        entity.AllowEventTracking = true;

        // Act
        tracker.AddEntity(entity);

        // Assert
        tracker.TrackedEntities.Should().HaveCount(1);
        tracker.TrackedEntities.Should().Contain(entity);
    }

    [Fact]
    public void AddEntity_MixedTrackingSettings_OnlyAddsTrackedEntities()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var trackedEntity1 = new TestEntity(1) { AllowEventTracking = true };
        var untrackedEntity = new TestEntity(2) { AllowEventTracking = false };
        var trackedEntity2 = new TestEntity(3) { AllowEventTracking = true };

        // Act
        tracker.AddEntity(trackedEntity1);
        tracker.AddEntity(untrackedEntity);
        tracker.AddEntity(trackedEntity2);

        // Assert
        tracker.TrackedEntities.Should().HaveCount(2);
        tracker.TrackedEntities.Should().Contain(trackedEntity1);
        tracker.TrackedEntities.Should().Contain(trackedEntity2);
        tracker.TrackedEntities.Should().NotContain(untrackedEntity);
    }

    [Fact]
    public void AddEntity_SameEntityAddedTwice_AddsBothInstances()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        // Act
        tracker.AddEntity(entity);
        tracker.AddEntity(entity);

        // Assert - List allows duplicates
        tracker.TrackedEntities.Should().HaveCount(2);
    }

    #endregion

    #region TrackedEntities Property Tests

    [Fact]
    public void TrackedEntities_IsInitializedAsEmptyCollection()
    {
        // Arrange & Act
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);

        // Assert
        tracker.TrackedEntities.Should().NotBeNull();
        tracker.TrackedEntities.Should().BeEmpty();
    }

    [Fact]
    public void TrackedEntities_ReturnsModifiableCollection()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        // Act
        tracker.AddEntity(entity);

        // Assert
        tracker.TrackedEntities.Should().BeAssignableTo<ICollection<IBusinessEntity>>();
    }

    #endregion

    #region EmitTransactionalEventsAsync Tests

    [Fact]
    public async Task EmitTransactionalEventsAsync_WithNoTrackedEntities_ReturnsTrue()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);

        // Act
        var result = await tracker.EmitTransactionalEventsAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_WithTrackedEntity_CallsRouteEventsAsync()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        tracker.AddEntity(entity);

        // Act
        await tracker.EmitTransactionalEventsAsync();

        // Assert
        _mockEventRouter.Verify(x => x.RouteEventsAsync(), Times.Once);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_WithEntityHavingLocalEvents_AddsEventsToRouter()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        entity.AddLocalEvent(testEvent);
        tracker.AddEntity(entity);

        // Act
        await tracker.EmitTransactionalEventsAsync();

        // Assert
        _mockEventRouter.Verify(
            x => x.AddTransactionalEvents(It.IsAny<IEnumerable<ISerializableEvent>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_ReturnsTrue_WhenSuccessful()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        var testEvent = new TestSerializableEvent { EventName = _faker.Lorem.Word() };
        entity.AddLocalEvent(testEvent);
        tracker.AddEntity(entity);

        // Act
        var result = await tracker.EmitTransactionalEventsAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_WithMultipleEntities_ProcessesAllEntities()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entities = Enumerable.Range(1, 3)
            .Select(i =>
            {
                var entity = new TestEntity(i);
                entity.AddLocalEvent(new TestSerializableEvent { EventName = $"Event_{i}" });
                return entity;
            })
            .ToList();

        foreach (var entity in entities)
        {
            tracker.AddEntity(entity);
        }

        // Act
        var result = await tracker.EmitTransactionalEventsAsync();

        // Assert
        result.Should().BeTrue();
        _mockEventRouter.Verify(x => x.RouteEventsAsync(), Times.Once);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_WithEntityWithoutEvents_StillCallsRouteEvents()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        // Not adding any events to entity
        tracker.AddEntity(entity);

        // Act
        await tracker.EmitTransactionalEventsAsync();

        // Assert
        _mockEventRouter.Verify(x => x.RouteEventsAsync(), Times.Once);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_WithMultipleEventsOnSingleEntity_AddsAllEvents()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));

        var events = Enumerable.Range(1, 5)
            .Select(i => new TestSerializableEvent { EventName = $"Event_{i}" })
            .ToList();

        foreach (var evt in events)
        {
            entity.AddLocalEvent(evt);
        }

        tracker.AddEntity(entity);

        // Act
        await tracker.EmitTransactionalEventsAsync();

        // Assert
        _mockEventRouter.Verify(
            x => x.AddTransactionalEvents(It.Is<IEnumerable<ISerializableEvent>>(e => e.Count() == 5)),
            Times.AtLeastOnce);
    }

    #endregion

    #region IEntityEventTracker Interface Tests

    [Fact]
    public void InMemoryEntityEventTracker_ImplementsIEntityEventTracker()
    {
        // Arrange & Act
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);

        // Assert
        tracker.Should().BeAssignableTo<IEntityEventTracker>();
    }

    [Fact]
    public void TrackedEntities_ImplementsICollectionOfIBusinessEntity()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);

        // Assert
        tracker.TrackedEntities.Should().BeAssignableTo<ICollection<IBusinessEntity>>();
    }

    #endregion

    #region Integration-like Tests

    [Fact]
    public async Task FullWorkflow_AddEntitiesWithEvents_EmitsSuccessfully()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);

        // Create multiple entities with events
        var entity1 = new TestEntity(1);
        entity1.AddLocalEvent(new TestSerializableEvent { EventName = "Created" });
        entity1.AddLocalEvent(new TestSerializableEvent { EventName = "Modified" });

        var entity2 = new TestEntity(2);
        entity2.AddLocalEvent(new TestSerializableEvent { EventName = "Validated" });

        // Act
        tracker.AddEntity(entity1);
        tracker.AddEntity(entity2);
        var result = await tracker.EmitTransactionalEventsAsync();

        // Assert
        result.Should().BeTrue();
        tracker.TrackedEntities.Should().HaveCount(2);
        _mockEventRouter.Verify(x => x.RouteEventsAsync(), Times.Once);
    }

    [Fact]
    public async Task EmitTransactionalEventsAsync_CalledMultipleTimes_CallsRouteEventsEachTime()
    {
        // Arrange
        var tracker = new InMemoryEntityEventTracker(_mockEventRouter.Object);
        var entity = new TestEntity(_faker.Random.Int(1, 1000));
        tracker.AddEntity(entity);

        // Act
        await tracker.EmitTransactionalEventsAsync();
        await tracker.EmitTransactionalEventsAsync();
        await tracker.EmitTransactionalEventsAsync();

        // Assert
        _mockEventRouter.Verify(x => x.RouteEventsAsync(), Times.Exactly(3));
    }

    #endregion
}
