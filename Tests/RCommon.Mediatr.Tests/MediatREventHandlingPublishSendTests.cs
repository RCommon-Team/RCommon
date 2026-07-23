using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.MediatR;
using RCommon.MediatR.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Mediatr.Tests;

// =============================================================================
// Task 2: Mediator Publish<T>() verb
// =============================================================================

/// <summary>
/// Verifies the Publish&lt;T&gt;() mediator fluent verb:
/// producer registration (idempotent), subscription recording, and durability.
/// </summary>
public class MediatREventHandlingPublishTests
{
    #region Producer Registration Tests

    [Fact]
    public void Publish_RegistersExactlyOnePublishWithMediatREventProducer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Publish_SingleEvent>();
        });

        // Assert -- exactly one IEventProducer descriptor for PublishWithMediatREventProducer
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithMediatREventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    [Fact]
    public void Publish_CalledTwice_RegistersProducerExactlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Publish_TwiceIdempotentEvent>();
            events.Publish<Publish_TwiceIdempotentEvent>();
        });

        // Assert -- idempotent: calling Publish twice must still produce exactly one descriptor
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithMediatREventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    [Fact]
    public void Publish_AndAddSubscriber_RegistersProducerExactlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- both Publish and AddSubscriber should be idempotent wrt producer registration
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Publish_WithSubscriberEvent>();
            events.AddSubscriber<Publish_WithSubscriberEvent, Publish_WithSubscriberEventHandler>();
        });

        // Assert -- still exactly one PublishWithMediatREventProducer descriptor
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithMediatREventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    #endregion

    #region Subscription Manager Tests

    [Fact]
    public void Publish_RecordsSubscriptionSoShouldProduceEventIsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Publish_SubscriptionEvent>();
        });

        // Assert -- ShouldProduceEvent must be true after Publish registers the producer and subscription
        var subscriptionManager = services.GetSubscriptionManager();
        subscriptionManager.Should().NotBeNull();
        subscriptionManager!.ShouldProduceEvent(
            typeof(PublishWithMediatREventProducer),
            typeof(Publish_SubscriptionEvent))
            .Should().BeTrue();
    }

    #endregion

    #region Durability Tests

    [Fact]
    public void Publish_Alone_LeavesEventTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Publish_TransientEvent>();
        });

        // Assert -- no .UseOutbox() called; event must remain transient
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(Publish_TransientEvent)).Should().BeFalse();
    }

    [Fact]
    public void Publish_WithUseOutbox_MarksEventDurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Publish_DurableEvent>().UseOutbox("Orders");
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(Publish_DurableEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(Publish_DurableEvent), out var store);
        store.Should().Be("Orders");
    }

    #endregion

    #region Test Event / Handler Classes

    public class Publish_SingleEvent : ISyncEvent { }
    public class Publish_TwiceIdempotentEvent : ISyncEvent { }
    public class Publish_WithSubscriberEvent : ISyncEvent { }
    public class Publish_SubscriptionEvent : ISyncEvent { }
    public class Publish_TransientEvent : ISyncEvent { }
    public class Publish_DurableEvent : ISyncEvent { }

    public class Publish_WithSubscriberEventHandler : RCommon.EventHandling.Subscribers.ISubscriber<Publish_WithSubscriberEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(
            Publish_WithSubscriberEvent @event,
            System.Threading.CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }

    #endregion
}

// =============================================================================
// Task 3: Mediator Send<T>() verb
// =============================================================================

/// <summary>
/// Verifies the Send&lt;T&gt;() mediator fluent verb:
/// producer registration (idempotent), subscription recording, and durability.
/// Also verifies that Publish and Send for the same event register both producers.
/// </summary>
public class MediatREventHandlingSendTests
{
    #region Producer Registration Tests

    [Fact]
    public void Send_RegistersExactlyOneSendWithMediatREventProducer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Send<Send_SingleEvent>();
        });

        // Assert
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(SendWithMediatREventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    [Fact]
    public void Send_CalledTwice_RegistersProducerExactlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Send<Send_TwiceIdempotentEvent>();
            events.Send<Send_TwiceIdempotentEvent>();
        });

        // Assert -- idempotent
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(SendWithMediatREventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    #endregion

    #region Subscription Manager Tests

    [Fact]
    public void Send_RecordsSubscriptionSoShouldProduceEventIsTrue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Send<Send_SubscriptionEvent>();
        });

        // Assert
        var subscriptionManager = services.GetSubscriptionManager();
        subscriptionManager.Should().NotBeNull();
        subscriptionManager!.ShouldProduceEvent(
            typeof(SendWithMediatREventProducer),
            typeof(Send_SubscriptionEvent))
            .Should().BeTrue();
    }

    #endregion

    #region Durability Tests

    [Fact]
    public void Send_Alone_LeavesEventTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Send<Send_TransientEvent>();
        });

        // Assert -- no .UseOutbox() called; event must remain transient
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(Send_TransientEvent)).Should().BeFalse();
    }

    [Fact]
    public void Send_WithUseOutbox_MarksEventDurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Send<Send_DurableEvent>().UseOutbox("Orders");
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(Send_DurableEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(Send_DurableEvent), out var store);
        store.Should().Be("Orders");
    }

    #endregion

    #region Publish + Send Both Register Tests

    [Fact]
    public void PublishAndSend_SameEvent_RegistersBothProducers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Send_BothProducersEvent>();
            events.Send<Send_BothProducersEvent>();
        });

        // Assert -- both producers registered
        var publishDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithMediatREventProducer));
        var sendDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(SendWithMediatREventProducer));

        publishDescriptors.Should().ContainSingle();
        sendDescriptors.Should().ContainSingle();
    }

    [Fact]
    public void PublishAndSend_SameEvent_BothProducersAreTargets()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<Send_BothProducersTargetEvent>();
            events.Send<Send_BothProducersTargetEvent>();
        });

        // Assert -- ShouldProduceEvent is true for both producer types
        var subscriptionManager = services.GetSubscriptionManager();
        subscriptionManager.Should().NotBeNull();
        subscriptionManager!.ShouldProduceEvent(
            typeof(PublishWithMediatREventProducer),
            typeof(Send_BothProducersTargetEvent))
            .Should().BeTrue();
        subscriptionManager.ShouldProduceEvent(
            typeof(SendWithMediatREventProducer),
            typeof(Send_BothProducersTargetEvent))
            .Should().BeTrue();
    }

    #endregion

    #region Test Event Classes

    public class Send_SingleEvent : ISyncEvent { }
    public class Send_TwiceIdempotentEvent : ISyncEvent { }
    public class Send_SubscriptionEvent : ISyncEvent { }
    public class Send_TransientEvent : ISyncEvent { }
    public class Send_DurableEvent : ISyncEvent { }
    public class Send_BothProducersEvent : ISyncEvent { }
    public class Send_BothProducersTargetEvent : ISyncEvent { }

    #endregion
}
