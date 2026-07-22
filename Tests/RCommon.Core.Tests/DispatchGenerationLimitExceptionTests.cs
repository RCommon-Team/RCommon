using FluentAssertions;
using RCommon.EventHandling.Producers;
using Xunit;

namespace RCommon.Core.Tests;

public class DispatchGenerationLimitExceptionTests
{
    [Fact]
    public void Records_The_Limit_And_Describes_The_Cascade()
    {
        var ex = new DispatchGenerationLimitException(16);

        ex.MaxDispatchGenerations.Should().Be(16);
        ex.Message.Should().Contain("16");
        ex.Message.ToLowerInvariant().Should().Contain("cascade");
    }
}
