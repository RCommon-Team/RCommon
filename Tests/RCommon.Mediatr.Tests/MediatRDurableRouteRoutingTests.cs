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
// Task 5: Durable mediator route / outbox pipeline proof
// =============================================================================

/// <summary>
/// Proves that a durable mediator route wires both the routing registry and the
/// subscription manager correctly so that the Phase-3a outbox pipeline will
/// (a) persist the event and (b) record <see cref="PublishWithMediatREventProducer"/>
/// as the target producer — without any new pipeline code.
///
/// Each test builds an isolated service collection to avoid singleton registry
/// state bleed between tests.
/// </summary>
public class MediatRDurableRouteRoutingTests
{
    // -------------------------------------------------------------------------
    // (a) Durable mediator Publish route: registry + subscription both wired
    // -------------------------------------------------------------------------

    [Fact]
    public void Publish_WithUseOutbox_MarksRegistryDurableAndRecordsSubscription()
    {
        // Arrange — fresh provider per test; no singleton state shared with other tests
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<OrderConfirmed>().UseOutbox("Orders");
        });

        // Assert (routing registry)
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull("IEventRoutingRegistry must be registered by the builder");

        var isDurable = registry!.TryGetOutboxStore(typeof(OrderConfirmed), out var storeName);
        isDurable.Should().BeTrue("a durable Publish route must be flagged in the routing registry");
        storeName.Should().Be("Orders", "the store name passed to UseOutbox must be preserved");

        // Assert (subscription manager)
        // ShouldProduceEvent returns true  => the Phase-3a FilterProducers call will keep
        // PublishWithMediatREventProducer in the TargetProducers set written to the outbox row,
        // and the post-commit relay will dispatch to it.
        var subscriptionManager = services.GetSubscriptionManager();
        subscriptionManager.Should().NotBeNull("EventSubscriptionManager must be registered by the builder");

        subscriptionManager!
            .ShouldProduceEvent(typeof(PublishWithMediatREventProducer), typeof(OrderConfirmed))
            .Should().BeTrue(
                "the mediator producer must survive the subscription filter so the outbox pipeline " +
                "records it as the target and later dispatches to it post-commit");
    }

    // -------------------------------------------------------------------------
    // (b) Transient mediator Publish route (no UseOutbox): must NOT be durable
    // -------------------------------------------------------------------------

    [Fact]
    public void Publish_WithoutUseOutbox_LeavesEventTransient()
    {
        // Arrange — fresh provider
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        rcommonBuilder.WithEventHandling<MediatREventHandlingBuilder>(events =>
        {
            events.Publish<OrderPlaced>(); // no .UseOutbox() chained
        });

        // Assert — transient events are dispatched pre-commit by the tracker,
        // not persisted to the outbox, so IsDurable must be false.
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull("IEventRoutingRegistry must be registered by the builder");

        registry!.IsDurable(typeof(OrderPlaced))
            .Should().BeFalse(
                "a Publish route without UseOutbox must remain transient and be dispatched pre-commit");
    }

    // -------------------------------------------------------------------------
    // Local event types (trivial, isolated to this test class)
    // -------------------------------------------------------------------------

    public class OrderConfirmed : ISyncEvent { }
    public class OrderPlaced : ISyncEvent { }
}
