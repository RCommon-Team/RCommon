using FluentAssertions;
using RCommon.Json;
using Xunit;

namespace RCommon.Json.Tests;

public class JsonDeserializeOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultValues_CamelCaseIsTrue()
    {
        // Arrange & Act
        var options = new JsonDeserializeOptions();

        // Assert
        options.CamelCase.Should().BeTrue();
    }

    [Fact]
    public void Constructor_CreatesNewInstance()
    {
        // Arrange & Act
        var options = new JsonDeserializeOptions();

        // Assert
        options.Should().NotBeNull();
    }

    #endregion

    #region CamelCase Property Tests

    [Fact]
    public void CamelCase_CanBeSetToFalse()
    {
        // Arrange
        var options = new JsonDeserializeOptions();

        // Act
        options.CamelCase = false;

        // Assert
        options.CamelCase.Should().BeFalse();
    }

    [Fact]
    public void CamelCase_CanBeSetToTrue()
    {
        // Arrange
        var options = new JsonDeserializeOptions();
        options.CamelCase = false;

        // Act
        options.CamelCase = true;

        // Assert
        options.CamelCase.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CamelCase_GetterReturnsSetValue(bool value)
    {
        // Arrange
        var options = new JsonDeserializeOptions();

        // Act
        options.CamelCase = value;

        // Assert
        options.CamelCase.Should().Be(value);
    }

    #endregion

    #region Object Initialization Tests

    [Fact]
    public void ObjectInitializer_CanSetCamelCase()
    {
        // Arrange & Act
        var options = new JsonDeserializeOptions
        {
            CamelCase = false
        };

        // Assert
        options.CamelCase.Should().BeFalse();
    }

    [Fact]
    public void ObjectInitializer_WithCamelCaseTrue()
    {
        // Arrange & Act
        var options = new JsonDeserializeOptions
        {
            CamelCase = true
        };

        // Assert
        options.CamelCase.Should().BeTrue();
    }

    #endregion

    #region Multiple Instance Tests

    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange
        var options1 = new JsonDeserializeOptions();
        var options2 = new JsonDeserializeOptions();

        // Act
        options1.CamelCase = false;

        // Assert
        options1.CamelCase.Should().BeFalse();
        options2.CamelCase.Should().BeTrue();
    }

    [Fact]
    public void MultipleInstances_HaveSameDefaultValues()
    {
        // Arrange & Act
        var options1 = new JsonDeserializeOptions();
        var options2 = new JsonDeserializeOptions();

        // Assert
        options1.CamelCase.Should().Be(options2.CamelCase);
    }

    #endregion
}
