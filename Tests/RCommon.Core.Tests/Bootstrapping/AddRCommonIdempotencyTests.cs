using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class AddRCommonIdempotencyTests
{
    [Fact]
    public void AddRCommon_CalledTwice_ReturnsSameBuilderInstance()
    {
        var services = new ServiceCollection();

        var first = services.AddRCommon();
        var second = services.AddRCommon();

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void AddRCommon_CalledTwice_RegistersEventBusOnlyOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon();
        services.AddRCommon();

        services.Count(d => d.ServiceType == typeof(IEventBus)).Should().Be(1);
    }

    [Fact]
    public void AddRCommon_CalledTwice_RegistersEventSubscriptionManagerOnlyOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon();
        services.AddRCommon();

        services.Count(d => d.ServiceType == typeof(EventSubscriptionManager)).Should().Be(1);
    }

    [Fact]
    public void AddRCommon_CalledTwice_RegistersEventRouterOnlyOnce()
    {
        var services = new ServiceCollection();

        services.AddRCommon();
        services.AddRCommon();

        services.Count(d => d.ServiceType == typeof(IEventRouter)).Should().Be(1);
    }

    [Fact]
    public void AddRCommon_CalledTwice_HasIdenticalDescriptorCountToCalledOnce()
    {
        var servicesA = new ServiceCollection();
        var servicesB = new ServiceCollection();

        servicesA.AddRCommon();
        servicesB.AddRCommon();
        servicesB.AddRCommon();

        servicesB.Count.Should().Be(servicesA.Count);
    }

    [Fact]
    public void IsRCommonInitialized_BeforeAddRCommon_ReturnsFalse()
    {
        var services = new ServiceCollection();

        services.IsRCommonInitialized().Should().BeFalse();
    }

    [Fact]
    public void IsRCommonInitialized_AfterAddRCommon_ReturnsTrue()
    {
        var services = new ServiceCollection();

        services.AddRCommon();

        services.IsRCommonInitialized().Should().BeTrue();
    }
}
