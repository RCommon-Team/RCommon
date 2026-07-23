using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

/// <summary>
/// Verifies the Publish&lt;T&gt;() in-memory bus fluent verb and the .UseOutbox() chaining
/// that marks events durable in the <see cref="IEventRoutingRegistry"/>.
/// </summary>
public class InMemoryEventBusPublishTests
{
    #region Producer Registration Tests

    [Fact]
    public void Publish_RegistersExactlyOnePublishWithEventBusEventProducer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>();
        });

        // Assert -- exactly one IEventProducer descriptor for PublishWithEventBusEventProducer
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithEventBusEventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    [Fact]
    public void Publish_CalledTwiceForSameEvent_RegistersProducerExactlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>();
            eb.Publish<PublishTestEvent>();
        });

        // Assert -- calling Publish twice must remain idempotent
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithEventBusEventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    [Fact]
    public void Publish_AndAddSubscriberForSameEvent_RegistersProducerExactlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- Publish and AddSubscriber both call AddProducer<PublishWithEventBusEventProducer>
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>();
            eb.AddSubscriber<PublishTestEvent, PublishTestEventHandler>();
        });

        // Assert -- must still be exactly one producer descriptor, not two
        var producerDescriptors = services.Where(d =>
            d.ServiceType == typeof(IEventProducer) &&
            d.ImplementationType == typeof(PublishWithEventBusEventProducer));

        producerDescriptors.Should().ContainSingle();
    }

    #endregion

    #region Subscription Manager Tests

    [Fact]
    public void Publish_RecordsSubscriptionInEventSubscriptionManager()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>();
        });

        // Assert -- the subscription manager should track this event type
        var subscriptionManager = services.GetSubscriptionManager();
        subscriptionManager.Should().NotBeNull();
        subscriptionManager!.HasSubscriptions.Should().BeTrue();
    }

    [Fact]
    public void Publish_ShouldProduceEvent_ReturnsTrueForRegisteredEvent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>();
        });

        // Assert -- the event should be routable to PublishWithEventBusEventProducer
        var subscriptionManager = services.GetSubscriptionManager();
        subscriptionManager.Should().NotBeNull();
        subscriptionManager!.ShouldProduceEvent(
            typeof(PublishWithEventBusEventProducer),
            typeof(PublishTestEvent))
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
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>();
        });

        // Assert -- no .UseOutbox() called; event must remain transient
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(PublishTestEvent)).Should().BeFalse();
    }

    [Fact]
    public void Publish_WithUseOutbox_MarksEventDurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>().UseOutbox("Orders");
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(PublishTestEvent)).Should().BeTrue();
    }

    [Fact]
    public void Publish_WithUseOutbox_StoresCorrectStoreName()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>().UseOutbox("Orders");
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        var found = registry!.TryGetOutboxStore(typeof(PublishTestEvent), out var storeName);
        found.Should().BeTrue();
        storeName.Should().Be("Orders");
    }

    [Fact]
    public void Publish_TwoEvents_OneWithOutboxOneWithout_OnlyFirstIsDurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<PublishTestEvent>().UseOutbox("Orders");
            eb.Publish<SecondPublishTestEvent>();
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(PublishTestEvent)).Should().BeTrue();
        registry.IsDurable(typeof(SecondPublishTestEvent)).Should().BeFalse();
    }

    #endregion

    #region IEventRouteHandle Fluent Chaining Tests

    [Fact]
    public void UseOutbox_ReturnsHandleInstance_AllowsFurtherChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        IEventRouteHandle? handle = null;

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            handle = eb.Publish<PublishTestEvent>().UseOutbox("Orders");
        });

        // Assert -- UseOutbox returns a non-null handle to allow further chaining
        handle.Should().NotBeNull();
    }

    #endregion

    #region Test Event / Handler Classes

    public class PublishTestEvent : ISyncEvent { }
    public class SecondPublishTestEvent : ISyncEvent { }

    public class PublishTestEventHandler : ISubscriber<PublishTestEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(
            PublishTestEvent @event,
            System.Threading.CancellationToken cancellationToken = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }

    #endregion
}
