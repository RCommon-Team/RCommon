using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

public class EventSubscriptionManagerTests
{
    #region AddProducerForBuilder Tests

    [Fact]
    public void AddProducerForBuilder_WithValidTypes_RecordsMapping()
    {
        // Arrange
        var manager = new EventSubscriptionManager();

        // Act
        manager.AddProducerForBuilder(typeof(InMemoryEventBusBuilder), typeof(PublishWithEventBusEventProducer));

        // Assert - verify via AddSubscription + GetProducersForEvent
        manager.AddSubscription(typeof(InMemoryEventBusBuilder), typeof(TestSyncEvent));
        var mockProducer = new Mock<IEventProducer>();
        var producers = new List<IEventProducer> { mockProducer.Object };

        // The producer type doesn't match PublishWithEventBusEventProducer, so it should be filtered out
        var result = manager.GetProducersForEvent(producers, typeof(TestSyncEvent));
        result.Should().BeEmpty();
    }

    [Fact]
    public void AddProducerForBuilder_MultipleProducersForSameBuilder_RecordsAll()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        var mockProducerA = new Mock<IEventProducer>();
        var mockProducerB = new Mock<IEventProducer>();

        // Act
        manager.AddProducerForBuilder(typeof(InMemoryEventBusBuilder), typeof(FakeProducerA));
        manager.AddProducerForBuilder(typeof(InMemoryEventBusBuilder), typeof(FakeProducerB));
        manager.AddSubscription(typeof(InMemoryEventBusBuilder), typeof(TestSyncEvent));

        // Assert - both producer types should be allowed for this event
        var producerA = new FakeProducerA();
        var producerB = new FakeProducerB();
        var allProducers = new List<IEventProducer> { producerA, producerB };

        var result = manager.GetProducersForEvent(allProducers, typeof(TestSyncEvent)).ToList();
        result.Should().HaveCount(2);
        result.Should().Contain(producerA);
        result.Should().Contain(producerB);
    }

    [Fact]
    public void AddProducerForBuilder_DifferentBuilders_TracksIndependently()
    {
        // Arrange
        var manager = new EventSubscriptionManager();

        // Act - register different producers for different builders
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddProducerForBuilder(typeof(FakeBuilderB), typeof(FakeProducerB));

        // Subscribe event to builder A only
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        // Assert
        var producerA = new FakeProducerA();
        var producerB = new FakeProducerB();
        var allProducers = new List<IEventProducer> { producerA, producerB };

        var result = manager.GetProducersForEvent(allProducers, typeof(TestSyncEvent)).ToList();
        result.Should().HaveCount(1);
        result.Should().Contain(producerA);
        result.Should().NotContain(producerB);
    }

    #endregion

    #region AddSubscription Tests

    [Fact]
    public void AddSubscription_WithNoProducersRegistered_DoesNotCreateMapping()
    {
        // Arrange
        var manager = new EventSubscriptionManager();

        // Act - subscribe without any producers registered for builder
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        // Assert - HasSubscriptions should be false since no producer types were mapped
        manager.HasSubscriptions.Should().BeFalse();
    }

    [Fact]
    public void AddSubscription_WithRegisteredProducers_CreatesEventMapping()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));

        // Act
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        // Assert
        manager.HasSubscriptions.Should().BeTrue();
    }

    [Fact]
    public void AddSubscription_SameEventDifferentBuilders_MergesProducers()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddProducerForBuilder(typeof(FakeBuilderB), typeof(FakeProducerB));

        // Act - subscribe same event through both builders
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));
        manager.AddSubscription(typeof(FakeBuilderB), typeof(TestSyncEvent));

        // Assert - both producers should handle this event
        var producerA = new FakeProducerA();
        var producerB = new FakeProducerB();
        var allProducers = new List<IEventProducer> { producerA, producerB };

        var result = manager.GetProducersForEvent(allProducers, typeof(TestSyncEvent)).ToList();
        result.Should().HaveCount(2);
        result.Should().Contain(producerA);
        result.Should().Contain(producerB);
    }

    [Fact]
    public void AddSubscription_DifferentEventsOnSameBuilder_AllMappedToSameProducer()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));

        // Act - subscribe multiple events through the same builder
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));
        manager.AddSubscription(typeof(FakeBuilderA), typeof(AnotherTestSyncEvent));

        // Assert - both events should map to the same producer
        var producerA = new FakeProducerA();
        var allProducers = new List<IEventProducer> { producerA };

        var resultEvent1 = manager.GetProducersForEvent(allProducers, typeof(TestSyncEvent)).ToList();
        var resultEvent2 = manager.GetProducersForEvent(allProducers, typeof(AnotherTestSyncEvent)).ToList();

        resultEvent1.Should().HaveCount(1).And.Contain(producerA);
        resultEvent2.Should().HaveCount(1).And.Contain(producerA);
    }

    #endregion

    #region GetProducersForEvent Tests

    [Fact]
    public void GetProducersForEvent_NoSubscriptions_ReturnsAllProducers()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        var producerA = new FakeProducerA();
        var producerB = new FakeProducerB();
        var allProducers = new List<IEventProducer> { producerA, producerB };

        // Act - no subscriptions registered at all
        var result = manager.GetProducersForEvent(allProducers, typeof(TestSyncEvent)).ToList();

        // Assert - backward compatible: returns all producers
        result.Should().HaveCount(2);
        result.Should().Contain(producerA);
        result.Should().Contain(producerB);
    }

    [Fact]
    public void GetProducersForEvent_WithSubscriptions_FiltersToMatchingProducers()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        var producerA = new FakeProducerA();
        var producerB = new FakeProducerB();
        var allProducers = new List<IEventProducer> { producerA, producerB };

        // Act
        var result = manager.GetProducersForEvent(allProducers, typeof(TestSyncEvent)).ToList();

        // Assert - only the subscribed producer is returned
        result.Should().HaveCount(1);
        result.Should().Contain(producerA);
    }

    [Fact]
    public void GetProducersForEvent_UnsubscribedEventType_ReturnsAllProducers()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        var producerA = new FakeProducerA();
        var producerB = new FakeProducerB();
        var allProducers = new List<IEventProducer> { producerA, producerB };

        // Act - query for an event type that has no subscriptions
        var result = manager.GetProducersForEvent(allProducers, typeof(AnotherTestSyncEvent)).ToList();

        // Assert - backward compatible: returns all producers for unregistered event type
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetProducersForEvent_EmptyProducersList_ReturnsEmpty()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        var allProducers = new List<IEventProducer>();

        // Act
        var result = manager.GetProducersForEvent(allProducers, typeof(TestSyncEvent)).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region ShouldProduceEvent Tests

    [Fact]
    public void ShouldProduceEvent_NoSubscriptions_ReturnsTrue()
    {
        // Arrange
        var manager = new EventSubscriptionManager();

        // Act
        var result = manager.ShouldProduceEvent(typeof(FakeProducerA), typeof(TestSyncEvent));

        // Assert - backward compatible: no subscriptions means all producers allowed
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProduceEvent_EventSubscribedToProducer_ReturnsTrue()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        // Act
        var result = manager.ShouldProduceEvent(typeof(FakeProducerA), typeof(TestSyncEvent));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProduceEvent_EventNotSubscribedToProducer_ReturnsFalse()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        // Act - FakeProducerB was NOT registered for this event
        var result = manager.ShouldProduceEvent(typeof(FakeProducerB), typeof(TestSyncEvent));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldProduceEvent_EventSubscribedThroughBothBuilders_ReturnsTrueForBothProducers()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddProducerForBuilder(typeof(FakeBuilderB), typeof(FakeProducerB));

        // Subscribe the same event through both builders
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));
        manager.AddSubscription(typeof(FakeBuilderB), typeof(TestSyncEvent));

        // Act & Assert - both producers should be allowed for this event
        manager.ShouldProduceEvent(typeof(FakeProducerA), typeof(TestSyncEvent)).Should().BeTrue();
        manager.ShouldProduceEvent(typeof(FakeProducerB), typeof(TestSyncEvent)).Should().BeTrue();
    }

    [Fact]
    public void ShouldProduceEvent_UnsubscribedEventType_ReturnsTrue()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        // Act - query for a different event type that has no subscriptions
        var result = manager.ShouldProduceEvent(typeof(FakeProducerB), typeof(AnotherTestSyncEvent));

        // Assert - backward compatible: unregistered event type allows all producers
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldProduceEvent_MixedIsolationAndShared_CorrectlyFilters()
    {
        // Arrange - simulate the example scenario:
        // AnotherTestSyncEvent -> FakeBuilderA/FakeProducerA only (isolated)
        // ThirdTestSyncEvent -> FakeBuilderB/FakeProducerB only (isolated)
        // TestSyncEvent -> both builders (shared)
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        manager.AddProducerForBuilder(typeof(FakeBuilderB), typeof(FakeProducerB));

        // Isolated subscriptions
        manager.AddSubscription(typeof(FakeBuilderA), typeof(AnotherTestSyncEvent));
        manager.AddSubscription(typeof(FakeBuilderB), typeof(ThirdTestSyncEvent));

        // Shared subscription
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));
        manager.AddSubscription(typeof(FakeBuilderB), typeof(TestSyncEvent));

        // Assert - isolated events only allow their respective producer
        manager.ShouldProduceEvent(typeof(FakeProducerA), typeof(AnotherTestSyncEvent)).Should().BeTrue();
        manager.ShouldProduceEvent(typeof(FakeProducerB), typeof(AnotherTestSyncEvent)).Should().BeFalse();

        manager.ShouldProduceEvent(typeof(FakeProducerA), typeof(ThirdTestSyncEvent)).Should().BeFalse();
        manager.ShouldProduceEvent(typeof(FakeProducerB), typeof(ThirdTestSyncEvent)).Should().BeTrue();

        // Assert - shared event allows both producers
        manager.ShouldProduceEvent(typeof(FakeProducerA), typeof(TestSyncEvent)).Should().BeTrue();
        manager.ShouldProduceEvent(typeof(FakeProducerB), typeof(TestSyncEvent)).Should().BeTrue();
    }

    #endregion

    #region HasSubscriptions Tests

    [Fact]
    public void HasSubscriptions_WhenEmpty_ReturnsFalse()
    {
        // Arrange & Act
        var manager = new EventSubscriptionManager();

        // Assert
        manager.HasSubscriptions.Should().BeFalse();
    }

    [Fact]
    public void HasSubscriptions_AfterSubscription_ReturnsTrue()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));

        // Act
        manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));

        // Assert
        manager.HasSubscriptions.Should().BeTrue();
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public void AddProducerForBuilder_ConcurrentCalls_DoesNotThrow()
    {
        // Arrange
        var manager = new EventSubscriptionManager();

        // Act
        var act = () => Parallel.For(0, 100, i =>
        {
            manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));
        });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddSubscription_ConcurrentCalls_DoesNotThrow()
    {
        // Arrange
        var manager = new EventSubscriptionManager();
        manager.AddProducerForBuilder(typeof(FakeBuilderA), typeof(FakeProducerA));

        // Act
        var act = () => Parallel.For(0, 100, i =>
        {
            manager.AddSubscription(typeof(FakeBuilderA), typeof(TestSyncEvent));
        });

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Test Helpers

    public class TestSyncEvent : ISyncEvent { }
    public class AnotherTestSyncEvent : ISyncEvent { }
    public class ThirdTestSyncEvent : ISyncEvent { }

    public class FakeBuilderA : IEventHandlingBuilder
    {
        public IServiceCollection Services => null!;
    }

    public class FakeBuilderB : IEventHandlingBuilder
    {
        public IServiceCollection Services => null!;
    }

    public class FakeProducerA : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }

    public class FakeProducerB : IEventProducer
    {
        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent => Task.CompletedTask;
    }

    #endregion
}
