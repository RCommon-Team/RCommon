using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class CachingOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultsCachingEnabledToFalse()
    {
        // Arrange & Act
        var options = new CachingOptions();

        // Assert
        options.CachingEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_DefaultsCacheDynamicallyCompiledExpressionsToFalse()
    {
        // Arrange & Act
        var options = new CachingOptions();

        // Assert
        options.CacheDynamicallyCompiledExpressions.Should().BeFalse();
    }

    #endregion

    #region CachingEnabled Property Tests

    [Fact]
    public void CachingEnabled_CanBeSetToTrue()
    {
        // Arrange
        var options = new CachingOptions();

        // Act
        options.CachingEnabled = true;

        // Assert
        options.CachingEnabled.Should().BeTrue();
    }

    [Fact]
    public void CachingEnabled_CanBeSetToFalse()
    {
        // Arrange
        var options = new CachingOptions { CachingEnabled = true };

        // Act
        options.CachingEnabled = false;

        // Assert
        options.CachingEnabled.Should().BeFalse();
    }

    #endregion

    #region CacheDynamicallyCompiledExpressions Property Tests

    [Fact]
    public void CacheDynamicallyCompiledExpressions_CanBeSetToTrue()
    {
        // Arrange
        var options = new CachingOptions();

        // Act
        options.CacheDynamicallyCompiledExpressions = true;

        // Assert
        options.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_CanBeSetToFalse()
    {
        // Arrange
        var options = new CachingOptions { CacheDynamicallyCompiledExpressions = true };

        // Act
        options.CacheDynamicallyCompiledExpressions = false;

        // Assert
        options.CacheDynamicallyCompiledExpressions.Should().BeFalse();
    }

    #endregion

    #region Combined Property Tests

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Properties_CanBeSetInAnyCombination(bool cachingEnabled, bool cacheExpressions)
    {
        // Arrange
        var options = new CachingOptions();

        // Act
        options.CachingEnabled = cachingEnabled;
        options.CacheDynamicallyCompiledExpressions = cacheExpressions;

        // Assert
        options.CachingEnabled.Should().Be(cachingEnabled);
        options.CacheDynamicallyCompiledExpressions.Should().Be(cacheExpressions);
    }

    #endregion

    #region Object Initialization Tests

    [Fact]
    public void ObjectInitializer_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new CachingOptions
        {
            CachingEnabled = true,
            CacheDynamicallyCompiledExpressions = true
        };

        // Assert
        options.CachingEnabled.Should().BeTrue();
        options.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    #endregion
}
