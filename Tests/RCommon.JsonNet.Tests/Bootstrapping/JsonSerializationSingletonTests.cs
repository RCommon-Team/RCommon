using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Json;
using RCommon.JsonNet;
using RCommon.SystemTextJson;
using Xunit;

namespace RCommon.JsonNet.Tests.Bootstrapping;

public class JsonSerializationSingletonTests
{
    [Fact]
    public void WithJsonSerialization_SameType_IsIdempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();

        builder.WithJsonSerialization<JsonNetBuilder>();
        Action secondCall = () => builder.WithJsonSerialization<JsonNetBuilder>();

        secondCall.Should().NotThrow();
    }

    [Fact]
    public void WithJsonSerialization_SameType_BothActionsApplied()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        var firstActionRan = false;
        var secondActionRan = false;

        builder.WithJsonSerialization<JsonNetBuilder>((Action<JsonNetBuilder>)(b => firstActionRan = true));
        builder.WithJsonSerialization<JsonNetBuilder>((Action<JsonNetBuilder>)(b => secondActionRan = true));

        firstActionRan.Should().BeTrue();
        secondActionRan.Should().BeTrue();
    }

    [Fact]
    public void WithJsonSerialization_DifferentType_Throws()
    {
        var services = new ServiceCollection();
        var builder = services.AddRCommon();
        builder.WithJsonSerialization<JsonNetBuilder>();

        Action act = () => builder.WithJsonSerialization<TextJsonBuilder>();

        act.Should().Throw<RCommonBuilderException>()
            .WithMessage("*JsonNetBuilder*TextJsonBuilder*");
    }
}
