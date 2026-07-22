using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
using Xunit;

namespace RCommon.Core.Tests;

public class EventHandlingOptionsTests
{
    [Fact]
    public void MaxDispatchGenerations_Defaults_To_16()
    {
        var options = new EventHandlingOptions();
        options.MaxDispatchGenerations.Should().Be(16);
    }

    [Fact]
    public void MaxDispatchGenerations_Is_Configurable_Via_Options()
    {
        var services = new ServiceCollection();
        services.AddOptions<EventHandlingOptions>().Configure(o => o.MaxDispatchGenerations = 4);
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IOptions<EventHandlingOptions>>()
            .Value.MaxDispatchGenerations.Should().Be(4);
    }
}
