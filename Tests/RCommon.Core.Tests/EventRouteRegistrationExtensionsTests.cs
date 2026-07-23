using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using RCommon.EventHandling;
using RCommon.EventHandling.Routing;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

/// <summary>
/// Direct tests for the public, builder-agnostic route-recording helpers in
/// <see cref="EventRouteRegistrationExtensions"/>. These helpers encapsulate the
/// concrete <see cref="EventRoutingRegistry"/> internals so callers in other assemblies
/// (e.g. the mediator verbs) can record durability routes without touching internals.
/// </summary>
public class EventRouteRegistrationExtensionsTests
{
    private static readonly System.Type BuilderType = typeof(InMemoryEventBusBuilder);

    /// <summary>
    /// Builds a service collection with core event handling configured so that the
    /// routing registry singleton is present, then returns the underlying services.
    /// </summary>
    private static IServiceCollection BuildServicesWithEventHandling()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(_ => { });
        return services;
    }

    [Fact]
    public void RecordPublishRoute_ReturnedHandle_UseOutbox_MarksEventDurable()
    {
        // Arrange
        var services = BuildServicesWithEventHandling();

        // Act
        var handle = services.RecordPublishRoute(BuilderType, typeof(HelperEventA));
        handle.UseOutbox("Orders");

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(HelperEventA)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(HelperEventA), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void RecordPublishRoute_AfterApplyBuilderOutboxDefault_ReturnsHandleForAlreadyDurableEvent()
    {
        // Arrange
        var services = BuildServicesWithEventHandling();

        // Act -- default set first, then record the route (order-independent)
        services.ApplyBuilderOutboxDefault(BuilderType, "Orders");
        var handle = services.RecordPublishRoute(BuilderType, typeof(HelperEventB));

        // Assert -- recording the route applies the pre-set builder default
        handle.Should().NotBeNull();
        var registry = services.GetRoutingRegistry();
        registry!.IsDurable(typeof(HelperEventB)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(HelperEventB), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void ApplyBuilderOutboxDefault_AfterRecordPublishRoute_RetroactivelyMarksDurable()
    {
        // Arrange
        var services = BuildServicesWithEventHandling();

        // Act -- record first, apply default after (retroactive)
        services.RecordPublishRoute(BuilderType, typeof(HelperEventC));
        services.ApplyBuilderOutboxDefault(BuilderType, "Orders");

        // Assert
        var registry = services.GetRoutingRegistry();
        registry!.IsDurable(typeof(HelperEventC)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(HelperEventC), out var store);
        store.Should().Be("Orders");
    }

    [Fact]
    public void ExplicitUseOutbox_NotClobberedByLaterApplyBuilderOutboxDefault()
    {
        // Arrange
        var services = BuildServicesWithEventHandling();

        // Act -- explicit per-event store, then builder default applied after
        services.RecordPublishRoute(BuilderType, typeof(HelperEventD)).UseOutbox("Billing");
        services.ApplyBuilderOutboxDefault(BuilderType, "Orders");

        // Assert -- explicit "Billing" must not be clobbered by retroactive "Orders"
        var registry = services.GetRoutingRegistry();
        registry!.IsDurable(typeof(HelperEventD)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(HelperEventD), out var store);
        store.Should().Be("Billing");
    }

    #region Test Event Classes

    public class HelperEventA : ISyncEvent { }
    public class HelperEventB : ISyncEvent { }
    public class HelperEventC : ISyncEvent { }
    public class HelperEventD : ISyncEvent { }

    #endregion
}
