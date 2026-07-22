using FluentAssertions;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Unit tests for the datastore-aware AddEntity overload on InMemoryEntityEventTracker (AC-8, AC-17).
/// </summary>
public class InMemoryEntityEventTrackerTests
{
    private static InMemoryEntityEventTracker CreateTracker()
    {
        var routerMock = new Mock<IEventRouter>();
        return new InMemoryEntityEventTracker(routerMock.Object);
    }

    private static Mock<IBusinessEntity> TrackableEntity()
    {
        var mock = new Mock<IBusinessEntity>();
        mock.Setup(e => e.AllowEventTracking).Returns(true);
        mock.Setup(e => e.LocalEvents).Returns(new List<ISerializableEvent>().AsReadOnly());
        return mock;
    }

    [Fact]
    public void AddEntity_WithDataStoreName_AssociatesEntityWithName()
    {
        var tracker = CreateTracker();
        var entity = TrackableEntity();

        tracker.AddEntity(entity.Object, "StoreA");

        var paired = tracker.TrackedEntitiesWithDataStore;
        paired.Should().ContainSingle(p => p.Entity == entity.Object && p.DataStoreName == "StoreA");
    }

    [Fact]
    public void AddEntity_WithoutDataStoreName_AssociatesEntityWithNull()
    {
        var tracker = CreateTracker();
        var entity = TrackableEntity();

        tracker.AddEntity(entity.Object);

        var paired = tracker.TrackedEntitiesWithDataStore;
        paired.Should().ContainSingle(p => p.Entity == entity.Object && p.DataStoreName == null);
    }

    [Fact]
    public void AddEntity_MultipleEntitiesWithDifferentStores_ExposesCorrectAssociations()
    {
        var tracker = CreateTracker();
        var entityA = TrackableEntity();
        var entityB = TrackableEntity();
        var entityC = TrackableEntity();

        tracker.AddEntity(entityA.Object, "A");
        tracker.AddEntity(entityB.Object, "B");
        tracker.AddEntity(entityC.Object); // no name → null (use-default)

        var paired = tracker.TrackedEntitiesWithDataStore.ToList();

        paired.Should().HaveCount(3);
        paired.Should().Contain(p => p.Entity == entityA.Object && p.DataStoreName == "A");
        paired.Should().Contain(p => p.Entity == entityB.Object && p.DataStoreName == "B");
        paired.Should().Contain(p => p.Entity == entityC.Object && p.DataStoreName == null);
    }

    [Fact]
    public void TrackedEntities_BackCompatProjection_ContainsAllTrackedEntities()
    {
        var tracker = CreateTracker();
        var entityA = TrackableEntity();
        var entityB = TrackableEntity();

        tracker.AddEntity(entityA.Object, "StoreX");
        tracker.AddEntity(entityB.Object);

        tracker.TrackedEntities.Should().Contain(entityA.Object);
        tracker.TrackedEntities.Should().Contain(entityB.Object);
        tracker.TrackedEntities.Should().HaveCount(2);
    }

    [Fact]
    public void AddEntity_EntityWithEventTrackingDisabled_IsNotTracked()
    {
        var tracker = CreateTracker();
        var entity = new Mock<IBusinessEntity>();
        entity.Setup(e => e.AllowEventTracking).Returns(false);

        tracker.AddEntity(entity.Object, "StoreA");

        tracker.TrackedEntitiesWithDataStore.Should().BeEmpty();
        tracker.TrackedEntities.Should().BeEmpty();
    }
}
