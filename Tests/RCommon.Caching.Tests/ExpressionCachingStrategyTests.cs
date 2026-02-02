using FluentAssertions;
using RCommon.Caching;
using Xunit;

namespace RCommon.Caching.Tests;

public class ExpressionCachingStrategyTests
{
    #region Enum Value Tests

    [Fact]
    public void ExpressionCachingStrategy_DefaultValue_Exists()
    {
        // Arrange & Act
        var strategy = ExpressionCachingStrategy.Default;

        // Assert
        strategy.Should().Be(ExpressionCachingStrategy.Default);
    }

    [Fact]
    public void ExpressionCachingStrategy_DefaultValue_HasExpectedNumericValue()
    {
        // Arrange & Act
        var numericValue = (int)ExpressionCachingStrategy.Default;

        // Assert
        numericValue.Should().Be(0);
    }

    [Fact]
    public void ExpressionCachingStrategy_IsDefined_ReturnsTrue()
    {
        // Arrange
        var strategy = ExpressionCachingStrategy.Default;

        // Act
        var isDefined = Enum.IsDefined(typeof(ExpressionCachingStrategy), strategy);

        // Assert
        isDefined.Should().BeTrue();
    }

    [Fact]
    public void ExpressionCachingStrategy_GetValues_ContainsDefault()
    {
        // Arrange & Act
        var values = Enum.GetValues<ExpressionCachingStrategy>();

        // Assert
        values.Should().Contain(ExpressionCachingStrategy.Default);
    }

    [Fact]
    public void ExpressionCachingStrategy_HasOnlyOneValue()
    {
        // Arrange & Act
        var values = Enum.GetValues<ExpressionCachingStrategy>();

        // Assert
        values.Should().HaveCount(1);
    }

    #endregion

    #region Enum Parsing Tests

    [Fact]
    public void ExpressionCachingStrategy_Parse_ReturnsDefaultForValidString()
    {
        // Arrange
        var input = "Default";

        // Act
        var result = Enum.Parse<ExpressionCachingStrategy>(input);

        // Assert
        result.Should().Be(ExpressionCachingStrategy.Default);
    }

    [Fact]
    public void ExpressionCachingStrategy_Parse_IsCaseInsensitive()
    {
        // Arrange
        var input = "default";

        // Act
        var result = Enum.Parse<ExpressionCachingStrategy>(input, ignoreCase: true);

        // Assert
        result.Should().Be(ExpressionCachingStrategy.Default);
    }

    [Fact]
    public void ExpressionCachingStrategy_TryParse_ReturnsTrueForValidString()
    {
        // Arrange
        var input = "Default";

        // Act
        var success = Enum.TryParse<ExpressionCachingStrategy>(input, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(ExpressionCachingStrategy.Default);
    }

    [Fact]
    public void ExpressionCachingStrategy_TryParse_ReturnsFalseForInvalidString()
    {
        // Arrange
        var input = "InvalidValue";

        // Act
        var success = Enum.TryParse<ExpressionCachingStrategy>(input, out var result);

        // Assert
        success.Should().BeFalse();
    }

    #endregion

    #region Enum ToString Tests

    [Fact]
    public void ExpressionCachingStrategy_ToString_ReturnsDefault()
    {
        // Arrange
        var strategy = ExpressionCachingStrategy.Default;

        // Act
        var result = strategy.ToString();

        // Assert
        result.Should().Be("Default");
    }

    #endregion

    #region Enum GetName Tests

    [Fact]
    public void ExpressionCachingStrategy_GetName_ReturnsExpectedName()
    {
        // Arrange
        var strategy = ExpressionCachingStrategy.Default;

        // Act
        var name = Enum.GetName(typeof(ExpressionCachingStrategy), strategy);

        // Assert
        name.Should().Be("Default");
    }

    [Fact]
    public void ExpressionCachingStrategy_GetNames_ContainsDefault()
    {
        // Arrange & Act
        var names = Enum.GetNames(typeof(ExpressionCachingStrategy));

        // Assert
        names.Should().Contain("Default");
        names.Should().HaveCount(1);
    }

    #endregion

    #region Enum Comparison Tests

    [Fact]
    public void ExpressionCachingStrategy_Equality_WorksCorrectly()
    {
        // Arrange
        var strategy1 = ExpressionCachingStrategy.Default;
        var strategy2 = ExpressionCachingStrategy.Default;

        // Act & Assert
        strategy1.Should().Be(strategy2);
        (strategy1 == strategy2).Should().BeTrue();
    }

    [Fact]
    public void ExpressionCachingStrategy_CanBeUsedInSwitch()
    {
        // Arrange
        var strategy = ExpressionCachingStrategy.Default;
        var result = string.Empty;

        // Act
        switch (strategy)
        {
            case ExpressionCachingStrategy.Default:
                result = "matched";
                break;
        }

        // Assert
        result.Should().Be("matched");
    }

    #endregion

    #region Nullable Tests

    [Fact]
    public void ExpressionCachingStrategy_Nullable_CanBeNull()
    {
        // Arrange
        ExpressionCachingStrategy? nullableStrategy = null;

        // Act & Assert
        nullableStrategy.Should().BeNull();
    }

    [Fact]
    public void ExpressionCachingStrategy_Nullable_CanHaveValue()
    {
        // Arrange
        ExpressionCachingStrategy? nullableStrategy = ExpressionCachingStrategy.Default;

        // Act & Assert
        nullableStrategy.Should().NotBeNull();
        nullableStrategy.Should().Be(ExpressionCachingStrategy.Default);
    }

    [Fact]
    public void ExpressionCachingStrategy_Nullable_HasValueProperty()
    {
        // Arrange
        ExpressionCachingStrategy? nullableStrategy = ExpressionCachingStrategy.Default;

        // Act & Assert
        nullableStrategy.HasValue.Should().BeTrue();
        nullableStrategy.Value.Should().Be(ExpressionCachingStrategy.Default);
    }

    #endregion
}
