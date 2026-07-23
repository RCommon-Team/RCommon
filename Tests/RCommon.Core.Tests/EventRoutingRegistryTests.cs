using FluentAssertions;
using RCommon.EventHandling.Routing;
using Xunit;

namespace RCommon.Core.Tests;

public class EventRoutingRegistryTests
{
    #region Empty Registry Tests

    [Fact]
    public void IsDurable_WhenRegistryIsEmpty_ReturnsFalse()
    {
        // Arrange
        var registry = new EventRoutingRegistry();

        // Act
        var result = registry.IsDurable(typeof(OrderCreatedEvent));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void TryGetOutboxStore_WhenRegistryIsEmpty_ReturnsFalseAndNullStore()
    {
        // Arrange
        var registry = new EventRoutingRegistry();

        // Act
        var result = registry.TryGetOutboxStore(typeof(OrderCreatedEvent), out var store);

        // Assert
        result.Should().BeFalse();
        store.Should().BeNull();
    }

    [Fact]
    public void DurableStoreNames_WhenRegistryIsEmpty_IsEmpty()
    {
        // Arrange
        var registry = new EventRoutingRegistry();

        // Act
        var names = registry.DurableStoreNames;

        // Assert
        names.Should().BeEmpty();
    }

    #endregion

    #region MarkDurable Tests

    [Fact]
    public void IsDurable_AfterMarkDurable_ReturnsTrue()
    {
        // Arrange
        var registry = new EventRoutingRegistry();

        // Act
        registry.MarkDurable(typeof(OrderCreatedEvent), "Orders");

        // Assert
        registry.IsDurable(typeof(OrderCreatedEvent)).Should().BeTrue();
    }

    [Fact]
    public void TryGetOutboxStore_AfterMarkDurable_ReturnsTrueAndStoreName()
    {
        // Arrange
        var registry = new EventRoutingRegistry();
        registry.MarkDurable(typeof(OrderCreatedEvent), "Orders");

        // Act
        var result = registry.TryGetOutboxStore(typeof(OrderCreatedEvent), out var store);

        // Assert
        result.Should().BeTrue();
        store.Should().Be("Orders");
    }

    [Fact]
    public void DurableStoreNames_AfterMarkDurable_ContainsStoreName()
    {
        // Arrange
        var registry = new EventRoutingRegistry();
        registry.MarkDurable(typeof(OrderCreatedEvent), "Orders");

        // Act
        var names = registry.DurableStoreNames;

        // Assert
        names.Should().Contain("Orders");
    }

    [Fact]
    public void MarkDurable_CalledTwiceForSameEventType_OverwritesWithLatestStore()
    {
        // Arrange
        var registry = new EventRoutingRegistry();
        registry.MarkDurable(typeof(OrderCreatedEvent), "Orders");

        // Act
        registry.MarkDurable(typeof(OrderCreatedEvent), "Billing");

        // Assert
        registry.TryGetOutboxStore(typeof(OrderCreatedEvent), out var store);
        store.Should().Be("Billing");
    }

    [Fact]
    public void DurableStoreNames_TwoEventTypesTwoDifferentStores_ContainsBoth()
    {
        // Arrange
        var registry = new EventRoutingRegistry();
        registry.MarkDurable(typeof(OrderCreatedEvent), "Orders");
        registry.MarkDurable(typeof(PaymentProcessedEvent), "Billing");

        // Act
        var names = registry.DurableStoreNames;

        // Assert
        names.Should().Contain("Orders");
        names.Should().Contain("Billing");
        names.Should().HaveCount(2);
    }

    [Fact]
    public void DurableStoreNames_TwoEventTypesSameStore_ReturnsDistinctStoreName()
    {
        // Arrange
        var registry = new EventRoutingRegistry();
        registry.MarkDurable(typeof(OrderCreatedEvent), "Orders");
        registry.MarkDurable(typeof(PaymentProcessedEvent), "Orders");

        // Act
        var names = registry.DurableStoreNames;

        // Assert
        names.Should().ContainSingle().Which.Should().Be("Orders");
    }

    #endregion

    #region Guard Tests

    [Fact]
    public void MarkDurable_WithNullEventType_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventRoutingRegistry();

        // Act
        var act = () => registry.MarkDurable(null!, "Orders");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MarkDurable_WithWhitespaceDataStoreName_ThrowsArgumentException()
    {
        // Arrange
        var registry = new EventRoutingRegistry();

        // Act
        var act = () => registry.MarkDurable(typeof(OrderCreatedEvent), " ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkDurable_WithNullDataStoreName_ThrowsArgumentNullException()
    {
        // Arrange
        var registry = new EventRoutingRegistry();

        // Act
        var act = () => registry.MarkDurable(typeof(OrderCreatedEvent), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Test Event Classes

    private class OrderCreatedEvent { }
    private class PaymentProcessedEvent { }

    #endregion
}
