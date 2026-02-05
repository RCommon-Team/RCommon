using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

/// <summary>
/// Tests that verify event subscription isolation: events subscribed through one builder's fluent interface
/// are only routed to producers registered on that same builder, not to producers from other builders.
/// </summary>
public class EventSubscriptionIsolationTests
{
    private static (IServiceCollection services, IRCommonBuilder builder) CreateBuilder()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var builder = new RCommonBuilder(services);
        return (services, builder);
    }

    #region Single Builder Tests

    [Fact]
    public async Task RouteEventsAsync_SingleBuilder_EventRoutedToItsProducer()
    {
        // Arrange
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<InMemoryEventBusBuilder>(eventHandling =>
            {
                eventHandling.AddProducer<FakeProducerA>();
                eventHandling.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act
        var testEvent = new TestSyncEvent { Message = "Hello from builder A" };
        await router.RouteEventsAsync(new List<ISerializableEvent> { testEvent });

        // Assert - the event was routed (FakeProducerA was called)
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        producers.Should().HaveCount(1);
        producers[0].Should().BeOfType<FakeProducerA>();
        ((FakeProducerA)producers[0]).EventsProduced.Should().HaveCount(1);
    }

    #endregion

    #region Multi-Builder Isolation Tests

    [Fact]
    public async Task RouteEventsAsync_TwoBuilders_EventOnlyRoutedToSubscribedProducer()
    {
        // Arrange - Configure two builders with different producers and different event subscriptions
        var (services, rCommonBuilder) = CreateBuilder();

        // Builder A subscribes TestSyncEvent to ProducerA
        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builderA =>
            {
                builderA.AddProducer<FakeProducerA>();
                builderA.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        // Builder B subscribes AnotherTestSyncEvent to ProducerB
        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builderB =>
            {
                builderB.AddProducer<FakeProducerB>();
                builderB.AddSubscriber<AnotherTestSyncEvent, AnotherTestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act - Route a TestSyncEvent (subscribed only through Builder A)
        var testEvent = new TestSyncEvent { Message = "Should only go to ProducerA" };
        await router.RouteEventsAsync(new List<ISerializableEvent> { testEvent });

        // Assert
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        producers.Should().HaveCount(2);

        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().HaveCount(1, "ProducerA should receive TestSyncEvent");
        producerB.EventsProduced.Should().BeEmpty("ProducerB should NOT receive TestSyncEvent");
    }

    [Fact]
    public async Task RouteEventsAsync_TwoBuilders_SecondEventOnlyRoutedToSecondProducer()
    {
        // Arrange
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builderA =>
            {
                builderA.AddProducer<FakeProducerA>();
                builderA.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builderB =>
            {
                builderB.AddProducer<FakeProducerB>();
                builderB.AddSubscriber<AnotherTestSyncEvent, AnotherTestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act - Route an AnotherTestSyncEvent (subscribed only through Builder B)
        var anotherEvent = new AnotherTestSyncEvent { Value = 42 };
        await router.RouteEventsAsync(new List<ISerializableEvent> { anotherEvent });

        // Assert
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().BeEmpty("ProducerA should NOT receive AnotherTestSyncEvent");
        producerB.EventsProduced.Should().HaveCount(1, "ProducerB should receive AnotherTestSyncEvent");
    }

    [Fact]
    public async Task RouteEventsAsync_TwoBuilders_BothEventsRoutedToCorrectProducers()
    {
        // Arrange
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builderA =>
            {
                builderA.AddProducer<FakeProducerA>();
                builderA.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builderB =>
            {
                builderB.AddProducer<FakeProducerB>();
                builderB.AddSubscriber<AnotherTestSyncEvent, AnotherTestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act - Route both event types
        var events = new List<ISerializableEvent>
        {
            new TestSyncEvent { Message = "For ProducerA" },
            new AnotherTestSyncEvent { Value = 99 }
        };
        await router.RouteEventsAsync(events);

        // Assert
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().HaveCount(1, "ProducerA should only receive TestSyncEvent");
        producerA.EventsProduced[0].Should().BeOfType<TestSyncEvent>();

        producerB.EventsProduced.Should().HaveCount(1, "ProducerB should only receive AnotherTestSyncEvent");
        producerB.EventsProduced[0].Should().BeOfType<AnotherTestSyncEvent>();
    }

    #endregion

    #region Shared Subscription Tests

    [Fact]
    public async Task RouteEventsAsync_SameEventSubscribedToBothBuilders_BothProducersReceiveIt()
    {
        // Arrange
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builderA =>
            {
                builderA.AddProducer<FakeProducerA>();
                builderA.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builderB =>
            {
                builderB.AddProducer<FakeProducerB>();
                builderB.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act
        var testEvent = new TestSyncEvent { Message = "Goes to both" };
        await router.RouteEventsAsync(new List<ISerializableEvent> { testEvent });

        // Assert - both producers should receive the event since both builders subscribed to it
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().HaveCount(1);
        producerB.EventsProduced.Should().HaveCount(1);
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public async Task RouteEventsAsync_UnsubscribedEventType_FallsBackToAllProducers()
    {
        // Arrange - Register two producers with subscriptions, but route an event type
        // that neither builder subscribed to. This simulates domain events added directly.
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builderA =>
            {
                builderA.AddProducer<FakeProducerA>();
                builderA.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builderB =>
            {
                builderB.AddProducer<FakeProducerB>();
                builderB.AddSubscriber<AnotherTestSyncEvent, AnotherTestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act - Route an event type that was NOT subscribed through any builder
        var unsubscribedEvent = new UnsubscribedSyncEvent();
        await router.RouteEventsAsync(new List<ISerializableEvent> { unsubscribedEvent });

        // Assert - backward compatible: all producers receive events with no explicit subscription
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().HaveCount(1, "Unsubscribed events fall back to all producers");
        producerB.EventsProduced.Should().HaveCount(1, "Unsubscribed events fall back to all producers");
    }

    #endregion

    #region Async Event Isolation Tests

    [Fact]
    public async Task RouteEventsAsync_AsyncEvents_IsolatedToCorrectProducer()
    {
        // Arrange
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builderA =>
            {
                builderA.AddProducer<FakeProducerA>();
                builderA.AddSubscriber<TestAsyncEvent, TestAsyncEventHandler>();
            });

        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builderB =>
            {
                builderB.AddProducer<FakeProducerB>();
                builderB.AddSubscriber<AnotherTestSyncEvent, AnotherTestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act
        var asyncEvent = new TestAsyncEvent { Message = "Async event for A" };
        await router.RouteEventsAsync(new List<ISerializableEvent> { asyncEvent });

        // Assert
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().HaveCount(1, "ProducerA should receive the async event");
        producerB.EventsProduced.Should().BeEmpty("ProducerB should NOT receive the async event");
    }

    #endregion

    #region Transactional Event Store Tests

    [Fact]
    public async Task RouteEventsAsync_StoredTransactionalEvents_IsolatedCorrectly()
    {
        // Arrange
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builderA =>
            {
                builderA.AddProducer<FakeProducerA>();
                builderA.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builderB =>
            {
                builderB.AddProducer<FakeProducerB>();
                builderB.AddSubscriber<AnotherTestSyncEvent, AnotherTestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Add events to the transactional store
        router.AddTransactionalEvent(new TestSyncEvent { Message = "Stored event for A" });
        router.AddTransactionalEvent(new AnotherTestSyncEvent { Value = 123 });

        // Act - route stored events
        await router.RouteEventsAsync();

        // Assert
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().HaveCount(1);
        producerA.EventsProduced[0].Should().BeOfType<TestSyncEvent>();

        producerB.EventsProduced.Should().HaveCount(1);
        producerB.EventsProduced[0].Should().BeOfType<AnotherTestSyncEvent>();
    }

    #endregion

    #region AddProducer / AddSubscriber Order Tests

    [Fact]
    public async Task RouteEventsAsync_ProducerAddedBeforeSubscriber_SubscriptionWorks()
    {
        // Arrange - the typical order: AddProducer then AddSubscriber
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builder =>
            {
                builder.AddProducer<FakeProducerA>();
                builder.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act
        await router.RouteEventsAsync(new List<ISerializableEvent> { new TestSyncEvent() });

        // Assert
        var producerA = serviceProvider.GetServices<IEventProducer>().OfType<FakeProducerA>().Single();
        producerA.EventsProduced.Should().HaveCount(1);
    }

    [Fact]
    public async Task RouteEventsAsync_MultipleSubscribersOnSameBuilder_AllShareSameProducer()
    {
        // Arrange
        var (services, rCommonBuilder) = CreateBuilder();

        rCommonBuilder
            .WithEventHandling<FakeBuilderA>(builder =>
            {
                builder.AddProducer<FakeProducerA>();
                builder.AddSubscriber<TestSyncEvent, TestSyncEventHandler>();
                builder.AddSubscriber<AnotherTestSyncEvent, AnotherTestSyncEventHandler>();
            });

        rCommonBuilder
            .WithEventHandling<FakeBuilderB>(builder =>
            {
                builder.AddProducer<FakeProducerB>();
                // No subscribers on builder B
            });

        var serviceProvider = services.BuildServiceProvider();
        var router = serviceProvider.GetRequiredService<IEventRouter>();

        // Act - route both event types
        var events = new List<ISerializableEvent>
        {
            new TestSyncEvent { Message = "Event 1" },
            new AnotherTestSyncEvent { Value = 42 }
        };
        await router.RouteEventsAsync(events);

        // Assert - both events go to ProducerA only
        var producers = serviceProvider.GetServices<IEventProducer>().ToList();
        var producerA = producers.OfType<FakeProducerA>().Single();
        var producerB = producers.OfType<FakeProducerB>().Single();

        producerA.EventsProduced.Should().HaveCount(2);
        producerB.EventsProduced.Should().BeEmpty("ProducerB has no subscribers, should not receive events");
    }

    #endregion

    #region Test Helpers

    // Event types
    public class TestSyncEvent : ISyncEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public class AnotherTestSyncEvent : ISyncEvent
    {
        public int Value { get; set; }
    }

    public class TestAsyncEvent : IAsyncEvent
    {
        public string Message { get; set; } = string.Empty;
    }

    public class UnsubscribedSyncEvent : ISyncEvent { }

    // Event handlers
    public class TestSyncEventHandler : ISubscriber<TestSyncEvent>
    {
        public Task HandleAsync(TestSyncEvent @event, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public class AnotherTestSyncEventHandler : ISubscriber<AnotherTestSyncEvent>
    {
        public Task HandleAsync(AnotherTestSyncEvent @event, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public class TestAsyncEventHandler : ISubscriber<TestAsyncEvent>
    {
        public Task HandleAsync(TestAsyncEvent @event, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    // Fake builders (implement IInMemoryEventBusBuilder so AddSubscriber extension methods resolve)
    public class FakeBuilderA : IInMemoryEventBusBuilder
    {
        public FakeBuilderA(IRCommonBuilder builder)
        {
            Services = builder.Services;
        }

        public IServiceCollection Services { get; }
    }

    public class FakeBuilderB : IInMemoryEventBusBuilder
    {
        public FakeBuilderB(IRCommonBuilder builder)
        {
            Services = builder.Services;
        }

        public IServiceCollection Services { get; }
    }

    // Fake producers that track which events they received
    public class FakeProducerA : IEventProducer
    {
        public List<ISerializableEvent> EventsProduced { get; } = new();

        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            EventsProduced.Add(@event);
            return Task.CompletedTask;
        }
    }

    public class FakeProducerB : IEventProducer
    {
        public List<ISerializableEvent> EventsProduced { get; } = new();

        public Task ProduceEventAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : ISerializableEvent
        {
            EventsProduced.Add(@event);
            return Task.CompletedTask;
        }
    }

    #endregion
}
