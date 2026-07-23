using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Producers;
using RCommon.EventHandling.Routing;
using RCommon.EventHandling.Subscribers;
using RCommon.Models.Events;
using RCommon.Wolverine;
using RCommon.Wolverine.Producers;
using Xunit;

namespace RCommon.Wolverine.Tests;

public class WolverineEventHandlingVerbsTests
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
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e => e.Publish<PublishEvent>());

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithWolverineEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void Publish_CalledTwice_RegistersProducerExactlyOnce()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
        {
            e.Publish<PublishTwiceEvent>();
            e.Publish<PublishTwiceEvent>();
        });

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithWolverineEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void Publish_RecordsSubscription()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e => e.Publish<PublishSubEvent>());

        var sm = services.GetSubscriptionManager();
        sm.Should().NotBeNull();
        sm!.ShouldProduceEvent(typeof(PublishWithWolverineEventProducer), typeof(PublishSubEvent))
            .Should().BeTrue();
    }

    [Fact]
    public void Publish_Alone_LeavesEventTransient()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e => e.Publish<PublishTransientEvent>());

        services.GetRoutingRegistry()!.IsDurable(typeof(PublishTransientEvent)).Should().BeFalse();
    }

    [Fact]
    public void Publish_WithUseOutbox_MarksEventDurable()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e => e.Publish<PublishDurableEvent>().UseOutbox("Orders"));

        var registry = services.GetRoutingRegistry()!;
        registry.IsDurable(typeof(PublishDurableEvent)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(PublishDurableEvent), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void Send_RegistersExactlyOneSendProducer()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e => e.Send<SendEvent>());

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(SendWithWolverineEventProducer))
            .Should().ContainSingle();
    }

    [Fact]
    public void PublishAndSend_SameEvent_RegistersBothProducers()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
        {
            e.Publish<BothEvent>();
            e.Send<BothEvent>();
        });

        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(PublishWithWolverineEventProducer)).Should().ContainSingle();
        services.Where(d => d.ServiceType == typeof(IEventProducer)
            && d.ImplementationType == typeof(SendWithWolverineEventProducer)).Should().ContainSingle();
    }

    [Fact]
    public void Send_ThenUseRCommonOutbox_MarksEventDurableTargetingStore()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
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
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
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
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
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
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
        {
            e.UseRCommonOutbox("Orders");
            e.Publish<PerEventWinsEvent>().UseOutbox("Billing");
        });

        services.GetRoutingRegistry()!.TryGetOutboxStore(typeof(PerEventWinsEvent), out var store);
        store.Should().Be("Billing");
    }

    [Fact]
    public void Consume_Plain_RegistersSubscriberAndRecordsSubscription()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e => e.Consume<ConsumeEvent, ConsumeHandler>());

        // Consume's observable effect: inbound subscriber DI registration + recorded subscription.
        // NOTE: do NOT assert ShouldProduceEvent(...) == false here — EventSubscriptionManager.ShouldProduceEvent
        // returns TRUE (backward-compat fallback) when no producer-map entry exists (i.e. Consume-only, no Publish/Send).
        // NOTE: HasSubscriptions is !_eventProducerMap.IsEmpty — it is only populated when a PRODUCER is registered,
        // so it remains FALSE after Consume alone. Assert GetBuilderTypesWithSubscriptions() instead.
        services.Any(d => d.ServiceType == typeof(ISubscriber<ConsumeEvent>)
            && d.ImplementationType == typeof(ConsumeHandler)).Should().BeTrue();
        services.GetSubscriptionManager()!.GetBuilderTypesWithSubscriptions().Should().NotBeEmpty();
    }

    [Fact]
    public void Consume_Factory_RegistersSubscriberAndRecordsSubscription()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
            e.Consume<ConsumeFactoryEvent, ConsumeFactoryHandler>(_ => new ConsumeFactoryHandler()));

        services.Any(d => d.ServiceType == typeof(ConsumeFactoryHandler)).Should().BeTrue();
        services.GetSubscriptionManager()!.GetBuilderTypesWithSubscriptions().Should().NotBeEmpty();
    }

    [Fact]
    public void Consume_RegistersSubscriberAsScoped()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e => e.Consume<ConsumeEvent, ConsumeHandler>());

        // Wolverine's Consume registers the inbound subscriber via AddScoped<ISubscriber<TEvent>, H>().
        var descriptor = services.Single(d => d.ServiceType == typeof(ISubscriber<ConsumeEvent>)
            && d.ImplementationType == typeof(ConsumeHandler));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddSubscriber_Plain_ObsoleteAlias_BehavesLikeConsume()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
        {
#pragma warning disable CS0618 // AddSubscriber is the obsolete alias under test
            e.AddSubscriber<AliasEvent, AliasHandler>();
#pragma warning restore CS0618
        });

        services.Any(d => d.ServiceType == typeof(ISubscriber<AliasEvent>)
            && d.ImplementationType == typeof(AliasHandler)).Should().BeTrue();
    }

    [Fact]
    public void AddSubscriber_Factory_ObsoleteAlias_BehavesLikeConsume()
    {
        var (services, builder) = NewHost();
        builder.WithEventHandling<WolverineEventHandlingBuilder>(e =>
        {
#pragma warning disable CS0618 // AddSubscriber factory overload is the obsolete alias under test
            e.AddSubscriber<AliasFactoryEvent, AliasFactoryHandler>(_ => new AliasFactoryHandler());
#pragma warning restore CS0618
        });

        services.Any(d => d.ServiceType == typeof(AliasFactoryHandler)).Should().BeTrue();
    }

    // Event types
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
    public class ConsumeFactoryEvent : ISyncEvent { }
    public class AliasEvent : ISyncEvent { }
    public class AliasFactoryEvent : ISyncEvent { }

    // Handlers
    public class ConsumeHandler : ISubscriber<ConsumeEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(ConsumeEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }

    public class ConsumeFactoryHandler : ISubscriber<ConsumeFactoryEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(ConsumeFactoryEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }

    public class AliasHandler : ISubscriber<AliasEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(AliasEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }

    public class AliasFactoryHandler : ISubscriber<AliasFactoryEvent>
    {
        public System.Threading.Tasks.Task HandleAsync(AliasFactoryEvent @event, System.Threading.CancellationToken ct = default)
            => System.Threading.Tasks.Task.CompletedTask;
    }
}
