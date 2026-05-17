using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class SubBuilderCacheTests
{
    [Fact]
    public void GetOrAddBuilder_FirstCall_InvokesFactoryOnce()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        var factoryCount = 0;

        builder.GetOrAddBuilder(() =>
        {
            factoryCount++;
            return new TestSubBuilder(services);
        });

        factoryCount.Should().Be(1);
    }

    [Fact]
    public void GetOrAddBuilder_SecondCallForSameType_DoesNotInvokeFactory()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        var factoryCount = 0;

        builder.GetOrAddBuilder(() => { factoryCount++; return new TestSubBuilder(services); });
        builder.GetOrAddBuilder(() => { factoryCount++; return new TestSubBuilder(services); });

        factoryCount.Should().Be(1);
    }

    [Fact]
    public void GetOrAddBuilder_SecondCallForSameType_ReturnsCachedInstance()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        var first = builder.GetOrAddBuilder(() => new TestSubBuilder(services));
        var second = builder.GetOrAddBuilder(() => new TestSubBuilder(services));

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void GetOrAddBuilder_DifferentTypes_ReturnsDistinctInstances()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        var subA = builder.GetOrAddBuilder(() => new TestSubBuilder(services));
        var subB = builder.GetOrAddBuilder(() => new OtherTestSubBuilder(services));

        ((object)subA).Should().NotBeSameAs(subB);
    }

    private sealed class TestSubBuilder
    {
        public TestSubBuilder(IServiceCollection services) { }
    }

    private sealed class OtherTestSubBuilder
    {
        public OtherTestSubBuilder(IServiceCollection services) { }
    }
}
