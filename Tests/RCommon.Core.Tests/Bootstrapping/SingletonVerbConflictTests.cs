using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace RCommon.Core.Tests.Bootstrapping;

public class SingletonVerbConflictTests
{
    [Fact]
    public void WithSimpleGuidGenerator_CalledTwice_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithSimpleGuidGenerator();
        Action secondCall = () => builder.WithSimpleGuidGenerator();

        secondCall.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(IGuidGenerator)).Should().Be(1);
    }

    [Fact]
    public void WithSequentialGuidGenerator_CalledTwice_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithSequentialGuidGenerator(o => { });
        Action secondCall = () => builder.WithSequentialGuidGenerator(o => { });

        secondCall.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(IGuidGenerator)).Should().Be(1);
    }

    [Fact]
    public void WithSimpleGuidGenerator_AfterSequentialGuidGenerator_ThrowsRCommonBuilderException()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithSequentialGuidGenerator(o => { });

        Action act = () => builder.WithSimpleGuidGenerator();

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*SequentialGuidGenerator*SimpleGuidGenerator*");
    }

    [Fact]
    public void WithSequentialGuidGenerator_AfterSimpleGuidGenerator_ThrowsRCommonBuilderException()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithSimpleGuidGenerator();

        Action act = () => builder.WithSequentialGuidGenerator(o => { });

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*SimpleGuidGenerator*SequentialGuidGenerator*");
    }

    [Fact]
    public void WithDateTimeSystem_CalledTwice_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithDateTimeSystem(o => { });
        Action secondCall = () => builder.WithDateTimeSystem(o => { });

        secondCall.Should().NotThrow();
        services.Count(d => d.ServiceType == typeof(ISystemTime)).Should().Be(1);
    }
}
