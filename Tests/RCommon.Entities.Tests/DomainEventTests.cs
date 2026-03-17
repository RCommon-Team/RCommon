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
