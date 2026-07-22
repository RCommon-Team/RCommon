using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.EventHandling.Routing;
using RCommon.Models.Events;
using Xunit;

namespace RCommon.Core.Tests;

/// <summary>
/// Verifies the UseRCommonOutbox() builder-level default durable store, covering all ordering
/// scenarios to ensure order-independent precedence between builder-level defaults and
/// per-event .UseOutbox() calls.
/// </summary>
public class InMemoryEventBusUseRCommonOutboxTests
{
    #region Ordering Scenario (i): UseRCommonOutbox before Publish

    [Fact]
    public void UseRCommonOutbox_ThenPublish_MarksEventDurableToBuilderDefaultStore()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- builder default set before Publish
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.UseRCommonOutbox("Orders");
            eb.Publish<ScenarioI_Event>();
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(ScenarioI_Event)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(ScenarioI_Event), out var store);
        store.Should().Be("Orders");
    }

    #endregion

    #region Ordering Scenario (ii): Publish before UseRCommonOutbox (retroactive)

    [Fact]
    public void Publish_ThenUseRCommonOutbox_MarksAlreadyPublishedEventDurable()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- Publish comes first, builder default retroactively applies
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<ScenarioII_Event>();
            eb.UseRCommonOutbox("Orders");
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(ScenarioII_Event)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(ScenarioII_Event), out var store);
        store.Should().Be("Orders");
    }

    #endregion

    #region Ordering Scenario (iii): UseRCommonOutbox before Publish+UseOutbox (per-event wins)

    [Fact]
    public void UseRCommonOutbox_ThenPublishWithUseOutbox_PerEventStoreTakesPrecedence()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- builder default "Orders", but per-event ".UseOutbox("Billing")" should win
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.UseRCommonOutbox("Orders");
            eb.Publish<ScenarioIII_Event>().UseOutbox("Billing");
        });

        // Assert -- "Billing" wins because it is explicit per-event
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(ScenarioIII_Event)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(ScenarioIII_Event), out var store);
        store.Should().Be("Billing");
    }

    #endregion

    #region Ordering Scenario (iv): Publish+UseOutbox before UseRCommonOutbox (explicit not clobbered)

    [Fact]
    public void PublishWithUseOutbox_ThenUseRCommonOutbox_ExplicitNotClobberedByRetroactive()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- explicit per-event ".UseOutbox("Billing")" set first; retroactive should NOT overwrite it
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<ScenarioIV_Event>().UseOutbox("Billing");
            eb.UseRCommonOutbox("Orders");
        });

        // Assert -- "Billing" still wins; retroactive "Orders" must not clobber it
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(ScenarioIV_Event)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(ScenarioIV_Event), out var store);
        store.Should().Be("Billing");
    }

    #endregion

    #region Ordering Scenario (v): No builder default, no UseOutbox => transient

    [Fact]
    public void Publish_WithNoBuilderDefaultAndNoUseOutbox_RemainsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- no UseRCommonOutbox, no .UseOutbox
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<ScenarioV_Event>();
        });

        // Assert -- event remains transient
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(ScenarioV_Event)).Should().BeFalse();
    }

    #endregion

    #region Additional: UseRCommonOutbox returns builder for chaining

    [Fact]
    public void UseRCommonOutbox_ReturnsBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);
        IInMemoryEventBusBuilder? captured = null;

        // Act
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            // UseRCommonOutbox should return the builder to allow chaining
            var returned = eb.UseRCommonOutbox("Orders");
            captured = returned as IInMemoryEventBusBuilder;
        });

        // Assert
        captured.Should().NotBeNull();
    }

    #endregion

    #region Additional: multiple events, only some with UseRCommonOutbox coverage

    [Fact]
    public void UseRCommonOutbox_AppliesDefaultToAllPublishedEvents_NotJustFirst()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- Publish two events before setting builder default; both should become durable
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<ScenarioMulti_EventA>();
            eb.Publish<ScenarioMulti_EventB>();
            eb.UseRCommonOutbox("Orders");
        });

        // Assert -- both retroactively marked durable
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();
        registry!.IsDurable(typeof(ScenarioMulti_EventA)).Should().BeTrue();
        registry!.IsDurable(typeof(ScenarioMulti_EventB)).Should().BeTrue();
        registry.TryGetOutboxStore(typeof(ScenarioMulti_EventA), out var storeA);
        registry.TryGetOutboxStore(typeof(ScenarioMulti_EventB), out var storeB);
        storeA.Should().Be("Orders");
        storeB.Should().Be("Orders");
    }

    [Fact]
    public void UseRCommonOutbox_DoesNotApplyToExplicitEvents_OnlyTransientOnes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- one explicit, one transient, then builder default
        rcommonBuilder.WithEventHandling<InMemoryEventBusBuilder>(eb =>
        {
            eb.Publish<ScenarioMixed_ExplicitEvent>().UseOutbox("Billing");
            eb.Publish<ScenarioMixed_DefaultEvent>();
            eb.UseRCommonOutbox("Orders");
        });

        // Assert
        var registry = services.GetRoutingRegistry();
        registry.Should().NotBeNull();

        // Explicit event keeps "Billing"
        registry.TryGetOutboxStore(typeof(ScenarioMixed_ExplicitEvent), out var explicitStore);
        explicitStore.Should().Be("Billing");

        // Default event gets "Orders" from builder default
        registry.TryGetOutboxStore(typeof(ScenarioMixed_DefaultEvent), out var defaultStore);
        defaultStore.Should().Be("Orders");
    }

    #endregion

    #region Test Event Classes (distinct per test to avoid registry bleed)

    public class ScenarioI_Event : ISyncEvent { }
    public class ScenarioII_Event : ISyncEvent { }
    public class ScenarioIII_Event : ISyncEvent { }
    public class ScenarioIV_Event : ISyncEvent { }
    public class ScenarioV_Event : ISyncEvent { }
    public class ScenarioMulti_EventA : ISyncEvent { }
    public class ScenarioMulti_EventB : ISyncEvent { }
    public class ScenarioMixed_ExplicitEvent : ISyncEvent { }
    public class ScenarioMixed_DefaultEvent : ISyncEvent { }

    #endregion
}
