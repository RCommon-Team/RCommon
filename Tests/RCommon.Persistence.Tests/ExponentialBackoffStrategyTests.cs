using FluentAssertions;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public class ExponentialBackoffStrategyTests
{
    [Fact]
    public void ComputeDelay_RetryCount0_ReturnsBaseDelay()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(30));
        strategy.ComputeDelay(0).Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ComputeDelay_RetryCount1_ReturnsBaseTimesMultiplier()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(30));
        strategy.ComputeDelay(1).Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void ComputeDelay_RetryCount3_ReturnsExponentialDelay()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(30));
        // 5 * 2^3 = 40 seconds
        strategy.ComputeDelay(3).Should().Be(TimeSpan.FromSeconds(40));
    }

    [Fact]
    public void ComputeDelay_ExceedsMax_CapsAtMaxDelay()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(60));
        // 5 * 2^10 = 5120 seconds, capped at 60
        strategy.ComputeDelay(10).Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void ComputeDelay_CustomMultiplier_UsesMultiplier()
    {
        var strategy = new ExponentialBackoffStrategy(
            TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(30), multiplier: 3.0);
        // 5 * 3^2 = 45 seconds
        strategy.ComputeDelay(2).Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public void Constructor_MaxDelaySmallerThanBaseDelay_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new ExponentialBackoffStrategy(
            TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10));
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maxDelay");
    }

    [Fact]
    public void Constructor_ZeroBaseDelay_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new ExponentialBackoffStrategy(
            TimeSpan.Zero, TimeSpan.FromMinutes(30));
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("baseDelay");
    }

    [Fact]
    public void Constructor_MultiplierLessThanOrEqualOne_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new ExponentialBackoffStrategy(
            TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(30), multiplier: 1.0);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("multiplier");
    }
}
