using FluentAssertions;
using RCommon.ApplicationServices;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class CqrsCachingOptionsTests
{
    [Fact]
    public void Constructor_Default_SetsUseCacheForHandlersToFalse()
    {
        // Arrange & Act
        var options = new CqrsCachingOptions();

        // Assert
        options.UseCacheForHandlers.Should().BeFalse();
    }

    [Fact]
    public void UseCacheForHandlers_CanBeSetToTrue()
    {
        // Arrange
        var options = new CqrsCachingOptions();

        // Act
        options.UseCacheForHandlers = true;

        // Assert
        options.UseCacheForHandlers.Should().BeTrue();
    }

    [Fact]
    public void UseCacheForHandlers_CanBeSetToFalse()
    {
        // Arrange
        var options = new CqrsCachingOptions { UseCacheForHandlers = true };

        // Act
        options.UseCacheForHandlers = false;

        // Assert
        options.UseCacheForHandlers.Should().BeFalse();
    }

    [Fact]
    public void ObjectInitializer_SetsProperty()
    {
        // Arrange & Act
        var options = new CqrsCachingOptions
        {
            UseCacheForHandlers = true
        };

        // Assert
        options.UseCacheForHandlers.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseCacheForHandlers_CanBeSetToAnyBoolValue(bool value)
    {
        // Arrange
        var options = new CqrsCachingOptions();

        // Act
        options.UseCacheForHandlers = value;

        // Assert
        options.UseCacheForHandlers.Should().Be(value);
    }

    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange
        var options1 = new CqrsCachingOptions { UseCacheForHandlers = true };
        var options2 = new CqrsCachingOptions { UseCacheForHandlers = false };

        // Assert
        options1.UseCacheForHandlers.Should().BeTrue();
        options2.UseCacheForHandlers.Should().BeFalse();
    }
}
