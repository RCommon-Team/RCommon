using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class SequentialGuidGeneratorOptionsTests
{
    #region DefaultSequentialGuidType Property Tests

    [Fact]
    public void DefaultSequentialGuidType_InitiallyNull()
    {
        // Arrange & Act
        var options = new SequentialGuidGeneratorOptions();

        // Assert
        options.DefaultSequentialGuidType.Should().BeNull();
    }

    [Fact]
    public void DefaultSequentialGuidType_CanBeSet()
    {
        // Arrange
        var options = new SequentialGuidGeneratorOptions();

        // Act
        options.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString;

        // Assert
        options.DefaultSequentialGuidType.Should().Be(SequentialGuidType.SequentialAsString);
    }

    [Theory]
    [InlineData(SequentialGuidType.SequentialAsString)]
    [InlineData(SequentialGuidType.SequentialAsBinary)]
    [InlineData(SequentialGuidType.SequentialAtEnd)]
    public void DefaultSequentialGuidType_AcceptsAllGuidTypes(SequentialGuidType guidType)
    {
        // Arrange
        var options = new SequentialGuidGeneratorOptions();

        // Act
        options.DefaultSequentialGuidType = guidType;

        // Assert
        options.DefaultSequentialGuidType.Should().Be(guidType);
    }

    #endregion

    #region GetDefaultSequentialGuidType Method Tests

    [Fact]
    public void GetDefaultSequentialGuidType_WhenNull_ReturnsSequentialAtEnd()
    {
        // Arrange
        var options = new SequentialGuidGeneratorOptions
        {
            DefaultSequentialGuidType = null
        };

        // Act
        var result = options.GetDefaultSequentialGuidType();

        // Assert
        result.Should().Be(SequentialGuidType.SequentialAtEnd);
    }

    [Fact]
    public void GetDefaultSequentialGuidType_WhenSetToSequentialAsString_ReturnsSequentialAsString()
    {
        // Arrange
        var options = new SequentialGuidGeneratorOptions
        {
            DefaultSequentialGuidType = SequentialGuidType.SequentialAsString
        };

        // Act
        var result = options.GetDefaultSequentialGuidType();

        // Assert
        result.Should().Be(SequentialGuidType.SequentialAsString);
    }

    [Fact]
    public void GetDefaultSequentialGuidType_WhenSetToSequentialAsBinary_ReturnsSequentialAsBinary()
    {
        // Arrange
        var options = new SequentialGuidGeneratorOptions
        {
            DefaultSequentialGuidType = SequentialGuidType.SequentialAsBinary
        };

        // Act
        var result = options.GetDefaultSequentialGuidType();

        // Assert
        result.Should().Be(SequentialGuidType.SequentialAsBinary);
    }

    [Fact]
    public void GetDefaultSequentialGuidType_WhenSetToSequentialAtEnd_ReturnsSequentialAtEnd()
    {
        // Arrange
        var options = new SequentialGuidGeneratorOptions
        {
            DefaultSequentialGuidType = SequentialGuidType.SequentialAtEnd
        };

        // Act
        var result = options.GetDefaultSequentialGuidType();

        // Assert
        result.Should().Be(SequentialGuidType.SequentialAtEnd);
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void NewInstance_HasExpectedDefaults()
    {
        // Arrange & Act
        var options = new SequentialGuidGeneratorOptions();

        // Assert
        options.DefaultSequentialGuidType.Should().BeNull();
        options.GetDefaultSequentialGuidType().Should().Be(SequentialGuidType.SequentialAtEnd);
    }

    #endregion
}
