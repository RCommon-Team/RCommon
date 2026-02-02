using FluentAssertions;
using RCommon.Json;
using Xunit;

namespace RCommon.Json.Tests;

public class JsonSerializeOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_DefaultValues_CamelCaseIsTrue()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions();

        // Assert
        options.CamelCase.Should().BeTrue();
    }

    [Fact]
    public void Constructor_DefaultValues_IndentedIsFalse()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions();

        // Assert
        options.Indented.Should().BeFalse();
    }

    [Fact]
    public void Constructor_CreatesNewInstance()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions();

        // Assert
        options.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_HasExpectedDefaults()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions();

        // Assert
        options.CamelCase.Should().BeTrue();
        options.Indented.Should().BeFalse();
    }

    #endregion

    #region CamelCase Property Tests

    [Fact]
    public void CamelCase_CanBeSetToFalse()
    {
        // Arrange
        var options = new JsonSerializeOptions();

        // Act
        options.CamelCase = false;

        // Assert
        options.CamelCase.Should().BeFalse();
    }

    [Fact]
    public void CamelCase_CanBeSetToTrue()
    {
        // Arrange
        var options = new JsonSerializeOptions();
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
        var options = new JsonSerializeOptions();

        // Act
        options.CamelCase = value;

        // Assert
        options.CamelCase.Should().Be(value);
    }

    #endregion

    #region Indented Property Tests

    [Fact]
    public void Indented_CanBeSetToTrue()
    {
        // Arrange
        var options = new JsonSerializeOptions();

        // Act
        options.Indented = true;

        // Assert
        options.Indented.Should().BeTrue();
    }

    [Fact]
    public void Indented_CanBeSetToFalse()
    {
        // Arrange
        var options = new JsonSerializeOptions();
        options.Indented = true;

        // Act
        options.Indented = false;

        // Assert
        options.Indented.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Indented_GetterReturnsSetValue(bool value)
    {
        // Arrange
        var options = new JsonSerializeOptions();

        // Act
        options.Indented = value;

        // Assert
        options.Indented.Should().Be(value);
    }

    #endregion

    #region Object Initialization Tests

    [Fact]
    public void ObjectInitializer_CanSetCamelCase()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions
        {
            CamelCase = false
        };

        // Assert
        options.CamelCase.Should().BeFalse();
        options.Indented.Should().BeFalse();
    }

    [Fact]
    public void ObjectInitializer_CanSetIndented()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions
        {
            Indented = true
        };

        // Assert
        options.CamelCase.Should().BeTrue();
        options.Indented.Should().BeTrue();
    }

    [Fact]
    public void ObjectInitializer_CanSetBothProperties()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions
        {
            CamelCase = false,
            Indented = true
        };

        // Assert
        options.CamelCase.Should().BeFalse();
        options.Indented.Should().BeTrue();
    }

    [Fact]
    public void ObjectInitializer_WithAllPropertiesTrue()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions
        {
            CamelCase = true,
            Indented = true
        };

        // Assert
        options.CamelCase.Should().BeTrue();
        options.Indented.Should().BeTrue();
    }

    [Fact]
    public void ObjectInitializer_WithAllPropertiesFalse()
    {
        // Arrange & Act
        var options = new JsonSerializeOptions
        {
            CamelCase = false,
            Indented = false
        };

        // Assert
        options.CamelCase.Should().BeFalse();
        options.Indented.Should().BeFalse();
    }

    #endregion

    #region Multiple Instance Tests

    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange
        var options1 = new JsonSerializeOptions();
        var options2 = new JsonSerializeOptions();

        // Act
        options1.CamelCase = false;
        options1.Indented = true;

        // Assert
        options1.CamelCase.Should().BeFalse();
        options1.Indented.Should().BeTrue();
        options2.CamelCase.Should().BeTrue();
        options2.Indented.Should().BeFalse();
    }

    [Fact]
    public void MultipleInstances_HaveSameDefaultValues()
    {
        // Arrange & Act
        var options1 = new JsonSerializeOptions();
        var options2 = new JsonSerializeOptions();

        // Assert
        options1.CamelCase.Should().Be(options2.CamelCase);
        options1.Indented.Should().Be(options2.Indented);
    }

    #endregion

    #region Property Combination Tests

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void AllPropertyCombinations_CanBeSet(bool camelCase, bool indented)
    {
        // Arrange
        var options = new JsonSerializeOptions();

        // Act
        options.CamelCase = camelCase;
        options.Indented = indented;

        // Assert
        options.CamelCase.Should().Be(camelCase);
        options.Indented.Should().Be(indented);
    }

    [Fact]
    public void SettingOneProperty_DoesNotAffectOther()
    {
        // Arrange
        var options = new JsonSerializeOptions();
        var originalIndented = options.Indented;

        // Act
        options.CamelCase = false;

        // Assert
        options.CamelCase.Should().BeFalse();
        options.Indented.Should().Be(originalIndented);
    }

    [Fact]
    public void SettingIndented_DoesNotAffectCamelCase()
    {
        // Arrange
        var options = new JsonSerializeOptions();
        var originalCamelCase = options.CamelCase;

        // Act
        options.Indented = true;

        // Assert
        options.Indented.Should().BeTrue();
        options.CamelCase.Should().Be(originalCamelCase);
    }

    #endregion
}
