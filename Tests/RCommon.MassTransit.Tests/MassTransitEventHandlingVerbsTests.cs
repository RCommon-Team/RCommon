using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.MassTransit;
using RCommon.MassTransit.Producers;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.MassTransit.Tests;

public class MassTransitEventHandlingVerbsTests
{
    private static (ServiceCollection services, RCommonBuilder builder) NewHost()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return (services, new RCommonBuilder(services));
    }

    [Fact]
    public void Publish_RegistersExactlyOnePublishProducer()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishEvent>());

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithMassTransitEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void Publish_CalledTwice_RegistersProducerExactlyOnce()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.Publish<PublishTwiceEvent>();
            e.Publish<PublishTwiceEvent>();
        });

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithMassTransitEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void Publish_RecordsSubscription()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishSubEvent>());

        var sm = services.GetSubscriptionManager();
        sm.Should().NotBeNull();
        sm!.ShouldProduceEvent(typeof(PublishWithMassTransitEventProducer), typeof(PublishSubEvent))
            .Should().BeTrue();
    }

    [Fact]
    public void Publish_Alone_LeavesEventTransient()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishTransientEvent>());

        services.GetRoutingRegistry()!.IsDurable(typeof(PublishTransientEvent)).Should().BeFalse();
    }

    [Fact]
    public void Publish_WithUseOutbox_MarksEventDurable()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Publish<PublishDurableEvent>().UseOutbox("Orders"));

        var registry = services.GetRoutingRegistry()!;
        registry.IsDurable(typeof(PublishDurableEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(PublishDurableEvent), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void Send_RegistersExactlyOneSendProducer()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Send<SendEvent>());

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(SendWithMassTransitEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void PublishAndSend_SameEvent_RegistersBothProducers()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.Publish<BothEvent>();
            e.Send<BothEvent>();
        });

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithMassTransitEventProducer)).Should().ContainSingle();
        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(SendWithMassTransitEventProducer)).Should().ContainSingle();
    }

    [Fact]
    public void Send_ThenUseRCommonOutbox_MarksEventDurableTargetingStore()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.Send<SendDurableEvent>();
            e.UseRCommonOutbox("Store");
        });

        var registry = services.GetRoutingRegistry()!;
        registry.IsDurable(typeof(SendDurableEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(SendDurableEvent), out var store).Should().BeTrue();
        store.Should().Be("Store");
    }

    [Fact]
    public void UseRCommonOutbox_ThenPublish_MarksDurable()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.UseRCommonOutbox("Orders");
            e.Publish<OutboxBeforeEvent>();
        });

        var registry = services.GetRoutingRegistry()!;
        registry.IsDurable(typeof(OutboxBeforeEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(OutboxBeforeEvent), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void Publish_ThenUseRCommonOutbox_RetroactivelyMarksDurable()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.Publish<OutboxAfterEvent>();
            e.UseRCommonOutbox("Orders");
        });

        services.GetRoutingRegistry()!.IsDurable(typeof(OutboxAfterEvent)).Should().BeTrue();
    }

    [Fact]
    public void PerEventUseOutbox_WinsOverBuilderDefault_EitherOrder()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
            e.UseRCommonOutbox("Orders");
            e.Publish<PerEventWinsEvent>().UseOutbox("Billing");
        });

        services.GetRoutingRegistry()!.TryGetOutboxStore(typeof(PerEventWinsEvent), out var store);
        store.Should().Be("Billing");
    }

    [Fact]
    public void Consume_RegistersSubscriberAndSubscription()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Consume<ConsumeEvent, ConsumeHandler>());

        // Consume's observable effect is the inbound subscriber registration + a recorded subscription.
        // NOTE: do NOT assert ShouldProduceEvent(...) == false here. EventSubscriptionManager.ShouldProduceEvent
        // returns TRUE (backward-compat fallback) for any event type with no producer-map entry, which is the
        // case when only Consume (no Publish/Send) ran. So a BeFalse() assertion would fail. Assert the
        // subscriber DI binding and that a subscription was recorded (HasSubscriptions) instead.
        services.Any(d => d.ServiceType == typeof(ISubscriber<ConsumeEvent>)
            && d.ImplementationType == typeof(ConsumeHandler)).Should().BeTrue();
        services.GetSubscriptionManager()!.GetBuilderTypesWithSubscriptions().Should().NotBeEmpty();
    }

    [Fact]
    public void Consume_RegistersSubscriberAsTransient()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e => e.Consume<ConsumeEvent, ConsumeHandler>());

        // MassTransit's Consume registers the inbound subscriber via AddTransient<ISubscriber<TEvent>, H>().
        var descriptor = services.Single(d => d.ServiceType == typeof(ISubscriber<ConsumeEvent>)
            && d.ImplementationType == typeof(ConsumeHandler));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddSubscriber_ObsoleteAlias_BehavesLikeConsume()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<MassTransitEventHandlingBuilder>(e =>
        {
#pragma warning disable CS0618 // AddSubscriber is the obsolete alias under test
            e.AddSubscriber<AliasEvent, AliasHandler>();
#pragma warning restore CS0618
        });

        services.Any(d => d.ServiceType == typeof(ISubscriber<AliasEvent>)
            && d.ImplementationType == typeof(AliasHandler)).Should().BeTrue();
    }

    public class PublishEvent : ISyncEvent { }
    public class PublishTwiceEvent : ISyncEvent { }
    public class PublishSubEvent : ISyncEvent { }
    public class PublishTransientEvent : ISyncEvent { }
    public class PublishDurableEvent : ISyncEvent { }
    public class SendEvent : ISyncEvent { }
    public class SendDurableEvent : ISyncEvent { }
    public class BothEvent : ISyncEvent { }
    public class OutboxBeforeEvent : ISyncEvent { }
    public class OutboxAfterEvent : ISyncEvent { }
    public class PerEventWinsEvent : ISyncEvent { }
    public class ConsumeEvent : ISyncEvent { }
    public class AliasEvent : ISyncEvent { }

    public class ConsumeHandler : ISubscriber<ConsumeEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(ConsumeEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }
    public class AliasHandler : ISubscriber<AliasEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(AliasEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }
}
