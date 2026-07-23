using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.EventHandling.Routing;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Covers the fail-loud startup diagnostic that rejects a durable event route that names a
/// datastore with no registered outbox.  Without this check, <c>UseOutbox("Ghost")</c> would
/// silently drop domain events at runtime because no poller drains "Ghost".
/// </summary>
public class DurableRouteOutboxValidationTests
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Builds a real <see cref="EventRoutingRegistry"/> with the supplied durable event
    /// store name(s) already registered (one dummy event type per store name).
    /// </summary>
    private static IEventRoutingRegistry BuildRoutingRegistry(params string[] durableStoreNames)
    {
        var registry = new EventRoutingRegistry();
        // Register one synthetic event type per store so DurableStoreNames is populated.
        for (var i = 0; i < durableStoreNames.Length; i++)
        {
            // Use a distinct Type placeholder for each store name.
            // We abuse RuntimeHelpers-assigned types via a generic; index is encoded via
            // an array of sentinel types defined below.
            registry.MarkDurable(SentinelTypes[i], durableStoreNames[i]);
        }
        return registry;
    }

    // A small bank of distinct sentinel types (one per durable store name used in tests).
    private static readonly Type[] SentinelTypes =
    [
        typeof(Sentinel0),
        typeof(Sentinel1),
        typeof(Sentinel2),
    ];

    private sealed class Sentinel0 { }
    private sealed class Sentinel1 { }
    private sealed class Sentinel2 { }

    /// <summary>
    /// Builds a real <see cref="OutboxDataStoreRegistry"/> with the supplied registered store
    /// name(s).
    /// </summary>
    private static IOutboxDataStoreRegistry BuildOutboxRegistry(params string[] registeredNames)
    {
        var registrationOptions = Options.Create(new OutboxDataStoreRegistrationOptions
        {
            Names = new List<string?>(registeredNames)
        });
        var defaultOptions = Options.Create(new DefaultDataStoreOptions
        {
            DefaultDataStoreName = string.Empty
        });
        return new OutboxDataStoreRegistry(registrationOptions, defaultOptions);
    }

    /// <summary>
    /// Builds a <see cref="DurableRouteOutboxValidationHostedService"/> with the two registries
    /// wired through a minimal DI container (so the service uses the same IServiceProvider path
    /// it will use in production).
    /// </summary>
    private static DurableRouteOutboxValidationHostedService BuildService(
        IEventRoutingRegistry routingRegistry,
        IOutboxDataStoreRegistry outboxRegistry)
    {
        var services = new ServiceCollection();
        services.AddSingleton(routingRegistry);
        services.AddSingleton(outboxRegistry);
        var sp = services.BuildServiceProvider();
        return new DurableRouteOutboxValidationHostedService(sp);
    }

    // ---------------------------------------------------------------------------
    // Tests
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task StartAsync_Throws_When_DurableStore_Has_No_Registered_Outbox()
    {
        // Arrange: durable route names "Ghost"; outbox only registered for "Orders".
        var routing = BuildRoutingRegistry("Ghost");
        var outbox = BuildOutboxRegistry("Orders");
        var sut = BuildService(routing, outbox);

        // Act
        Func<Task> act = () => sut.StartAsync(CancellationToken.None);

        // Assert: exception thrown and message names the offending store.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ghost*");
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_When_DurableStore_Has_Registered_Outbox()
    {
        // Arrange: durable route and outbox both name "Orders".
        var routing = BuildRoutingRegistry("Orders");
        var outbox = BuildOutboxRegistry("Orders");
        var sut = BuildService(routing, outbox);

        // Act & Assert
        Func<Task> act = () => sut.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_When_Comparison_Is_CaseInsensitive()
    {
        // Arrange: durable route uses lowercase "orders"; outbox registered with "Orders".
        var routing = BuildRoutingRegistry("orders");
        var outbox = BuildOutboxRegistry("Orders");
        var sut = BuildService(routing, outbox);

        // Act & Assert
        Func<Task> act = () => sut.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow_When_NoDurableRoutes()
    {
        // Arrange: no durable routes registered at all.
        var routing = BuildRoutingRegistry(); // empty
        var outbox = BuildOutboxRegistry();   // empty
        var sut = BuildService(routing, outbox);

        // Act & Assert
        Func<Task> act = () => sut.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_AlwaysCompletes()
    {
        var routing = BuildRoutingRegistry();
        var outbox = BuildOutboxRegistry();
        var sut = BuildService(routing, outbox);

        await sut.StopAsync(CancellationToken.None); // should not throw
    }
}
