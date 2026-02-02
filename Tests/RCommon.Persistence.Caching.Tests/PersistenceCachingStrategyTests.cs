using FluentAssertions;
using RCommon.Persistence.Caching;
using Xunit;

namespace RCommon.Persistence.Caching.Tests;

public class PersistenceCachingStrategyTests
{
    #region Enum Value Tests

    [Fact]
    public void PersistenceCachingStrategy_HasDefaultValue()
    {
        // Act
        var defaultValue = PersistenceCachingStrategy.Default;

        // Assert
        defaultValue.Should().BeDefined();
    }

    [Fact]
    public void PersistenceCachingStrategy_DefaultHasCorrectUnderlyingValue()
    {
        // Act
        var underlyingValue = (int)PersistenceCachingStrategy.Default;

        // Assert
        underlyingValue.Should().Be(0);
    }

    [Fact]
    public void PersistenceCachingStrategy_CanBeParsedFromString()
    {
        // Act
        var parsed = Enum.Parse<PersistenceCachingStrategy>("Default");

        // Assert
        parsed.Should().Be(PersistenceCachingStrategy.Default);
    }

    [Fact]
    public void PersistenceCachingStrategy_ToStringReturnsExpectedValue()
    {
        // Act
        var stringValue = PersistenceCachingStrategy.Default.ToString();

        // Assert
        stringValue.Should().Be("Default");
    }

    [Theory]
    [InlineData(PersistenceCachingStrategy.Default)]
    public void PersistenceCachingStrategy_IsDefined(PersistenceCachingStrategy strategy)
    {
        // Act
        var isDefined = Enum.IsDefined(strategy);

        // Assert
        isDefined.Should().BeTrue();
    }

    [Fact]
    public void PersistenceCachingStrategy_GetValuesReturnsAllValues()
    {
        // Act
        var values = Enum.GetValues<PersistenceCachingStrategy>();

        // Assert
        values.Should().Contain(PersistenceCachingStrategy.Default);
        values.Should().HaveCount(1);
    }

    [Fact]
    public void PersistenceCachingStrategy_GetNamesReturnsAllNames()
    {
        // Act
        var names = Enum.GetNames<PersistenceCachingStrategy>();

        // Assert
        names.Should().Contain("Default");
        names.Should().HaveCount(1);
    }

    #endregion

    #region Usage Tests

    [Fact]
    public void PersistenceCachingStrategy_CanBeUsedAsKeyInDictionary()
    {
        // Arrange
        var dictionary = new Dictionary<PersistenceCachingStrategy, string>();

        // Act
        dictionary[PersistenceCachingStrategy.Default] = "DefaultStrategy";

        // Assert
        dictionary.Should().ContainKey(PersistenceCachingStrategy.Default);
        dictionary[PersistenceCachingStrategy.Default].Should().Be("DefaultStrategy");
    }

    [Fact]
    public void PersistenceCachingStrategy_EqualsWorksCorrectly()
    {
        // Arrange
        var strategy1 = PersistenceCachingStrategy.Default;
        var strategy2 = PersistenceCachingStrategy.Default;

        // Act & Assert
        strategy1.Equals(strategy2).Should().BeTrue();
        (strategy1 == strategy2).Should().BeTrue();
    }

    [Fact]
    public void PersistenceCachingStrategy_GetHashCodeIsConsistent()
    {
        // Arrange
        var strategy1 = PersistenceCachingStrategy.Default;
        var strategy2 = PersistenceCachingStrategy.Default;

        // Act
        var hashCode1 = strategy1.GetHashCode();
        var hashCode2 = strategy2.GetHashCode();

        // Assert
        hashCode1.Should().Be(hashCode2);
    }

    [Fact]
    public void PersistenceCachingStrategy_CanBeUsedInSwitch()
    {
        // Arrange
        var strategy = PersistenceCachingStrategy.Default;
        string result = string.Empty;

        // Act
        switch (strategy)
        {
            case PersistenceCachingStrategy.Default:
                result = "Default";
                break;
        }

        // Assert
        result.Should().Be("Default");
    }

    [Fact]
    public void PersistenceCachingStrategy_CanBeUsedInSwitchExpression()
    {
        // Arrange
        var strategy = PersistenceCachingStrategy.Default;

        // Act
        var result = strategy switch
        {
            PersistenceCachingStrategy.Default => "Default Strategy",
            _ => "Unknown"
        };

        // Assert
        result.Should().Be("Default Strategy");
    }

    #endregion
}
