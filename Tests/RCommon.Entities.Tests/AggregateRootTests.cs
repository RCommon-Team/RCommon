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
        var aggregate = new TestAggregateInt(1);
        aggregate.Should().BeAssignableTo<IAggregateRoot>();
    }

    [Fact]
    public void AggregateRoot_Implements_IAggregateRootGeneric()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.Should().BeAssignableTo<IAggregateRoot<int>>();
    }

    [Fact]
    public void AggregateRoot_Implements_IBusinessEntity()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.Should().BeAssignableTo<IBusinessEntity>();
    }

    [Fact]
    public void AggregateRoot_Implements_IBusinessEntityGeneric()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.Should().BeAssignableTo<IBusinessEntity<int>>();
    }

    #endregion

    #region Identity Tests

    [Fact]
    public void AggregateRoot_ConstructorWithId_SetsId()
    {
        var aggregate = new TestAggregateInt(42);
        aggregate.Id.Should().Be(42);
    }

    [Fact]
    public void AggregateRoot_DefaultConstructor_IdIsDefault()
    {
        var aggregate = new TestAggregateInt();
        aggregate.Id.Should().Be(0);
    }

    [Fact]
    public void AggregateRoot_GuidKey_SetsId()
    {
        var id = Guid.NewGuid();
        var aggregate = new TestAggregateGuid(id);
        aggregate.Id.Should().Be(id);
    }

    #endregion

    #region Version Tests

    [Fact]
    public void AggregateRoot_DefaultVersion_IsZero()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.Version.Should().Be(0);
    }

    [Fact]
    public void IncrementVersion_IncrementsVersionByOne()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.BumpVersion();
        aggregate.Version.Should().Be(1);
    }

    [Fact]
    public void IncrementVersion_CalledMultipleTimes_VersionIncrementsCorrectly()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.BumpVersion();
        aggregate.BumpVersion();
        aggregate.BumpVersion();
        aggregate.Version.Should().Be(3);
    }

    #endregion

    #region Domain Event Add/Remove/Clear Tests

    [Fact]
    public void AddDomainEvent_AddsEventToDomainEvents()
    {
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");
        aggregate.RaiseDomainEvent(domainEvent);
        aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().Be(domainEvent);
    }

    [Fact]
    public void AddDomainEvent_MultipleEvents_AllPresent()
    {
        var aggregate = new TestAggregateInt(1);
        var event1 = new TestDomainEvent("first");
        var event2 = new TestOtherDomainEvent(42);
        aggregate.RaiseDomainEvent(event1);
        aggregate.RaiseDomainEvent(event2);
        aggregate.DomainEvents.Should().HaveCount(2);
        aggregate.DomainEvents.Should().Contain(event1);
        aggregate.DomainEvents.Should().Contain(event2);
    }

    [Fact]
    public void RemoveDomainEvent_RemovesEventFromDomainEvents()
    {
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");
        aggregate.RaiseDomainEvent(domainEvent);
        aggregate.UndoDomainEvent(domainEvent);
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllEvents()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.RaiseDomainEvent(new TestDomainEvent("first"));
        aggregate.RaiseDomainEvent(new TestOtherDomainEvent(42));
        aggregate.ClearDomainEvents();
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_WhenEmpty_ReturnsEmptyCollection()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.DomainEvents.Should().NotBeNull();
    }

    #endregion

    #region Dual-List Sync Tests (DomainEvents + LocalEvents)

    [Fact]
    public void AddDomainEvent_AlsoAppearsInLocalEvents()
    {
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");
        aggregate.RaiseDomainEvent(domainEvent);
        aggregate.DomainEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
        aggregate.LocalEvents.Should().ContainSingle().Which.Should().Be(domainEvent);
    }

    [Fact]
    public void RemoveDomainEvent_AlsoRemovesFromLocalEvents()
    {
        var aggregate = new TestAggregateInt(1);
        var domainEvent = new TestDomainEvent("test");
        aggregate.RaiseDomainEvent(domainEvent);
        aggregate.UndoDomainEvent(domainEvent);
        aggregate.DomainEvents.Should().BeEmpty();
        aggregate.LocalEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_AlsoClearsLocalEvents()
    {
        var aggregate = new TestAggregateInt(1);
        aggregate.RaiseDomainEvent(new TestDomainEvent("one"));
        aggregate.RaiseDomainEvent(new TestDomainEvent("two"));
        aggregate.ClearDomainEvents();
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
        var aggregate = new TestAggregateInt(42);
        var keys = aggregate.GetKeys();
        keys.Should().ContainSingle().Which.Should().Be(42);
    }

    [Fact]
    public void AggregateRoot_EntityEquals_SameId_ReturnsTrue()
    {
        var aggregate1 = new TestAggregateInt(42);
        var aggregate2 = new TestAggregateInt(42);
        aggregate1.EntityEquals(aggregate2).Should().BeTrue();
    }

    #endregion
}
