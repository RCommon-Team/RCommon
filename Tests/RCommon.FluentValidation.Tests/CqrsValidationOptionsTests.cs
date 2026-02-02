using FluentAssertions;
using RCommon.ApplicationServices;
using Xunit;

namespace RCommon.FluentValidation.Tests;

public class CqrsValidationOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var options = new CqrsValidationOptions();

        // Assert
        options.ValidateQueries.Should().BeFalse();
        options.ValidateCommands.Should().BeFalse();
    }

    #endregion

    #region ValidateQueries Property Tests

    [Fact]
    public void ValidateQueries_CanBeSetToTrue()
    {
        // Arrange
        var options = new CqrsValidationOptions();

        // Act
        options.ValidateQueries = true;

        // Assert
        options.ValidateQueries.Should().BeTrue();
    }

    [Fact]
    public void ValidateQueries_CanBeSetToFalse()
    {
        // Arrange
        var options = new CqrsValidationOptions { ValidateQueries = true };

        // Act
        options.ValidateQueries = false;

        // Assert
        options.ValidateQueries.Should().BeFalse();
    }

    [Fact]
    public void ValidateQueries_DefaultValueIsFalse()
    {
        // Arrange & Act
        var options = new CqrsValidationOptions();

        // Assert
        options.ValidateQueries.Should().BeFalse();
    }

    #endregion

    #region ValidateCommands Property Tests

    [Fact]
    public void ValidateCommands_CanBeSetToTrue()
    {
        // Arrange
        var options = new CqrsValidationOptions();

        // Act
        options.ValidateCommands = true;

        // Assert
        options.ValidateCommands.Should().BeTrue();
    }

    [Fact]
    public void ValidateCommands_CanBeSetToFalse()
    {
        // Arrange
        var options = new CqrsValidationOptions { ValidateCommands = true };

        // Act
        options.ValidateCommands = false;

        // Assert
        options.ValidateCommands.Should().BeFalse();
    }

    [Fact]
    public void ValidateCommands_DefaultValueIsFalse()
    {
        // Arrange & Act
        var options = new CqrsValidationOptions();

        // Assert
        options.ValidateCommands.Should().BeFalse();
    }

    #endregion

    #region Combined Property Tests

    [Fact]
    public void Options_CanHaveBothPropertiesSetToTrue()
    {
        // Arrange
        var options = new CqrsValidationOptions();

        // Act
        options.ValidateQueries = true;
        options.ValidateCommands = true;

        // Assert
        options.ValidateQueries.Should().BeTrue();
        options.ValidateCommands.Should().BeTrue();
    }

    [Fact]
    public void Options_CanHaveBothPropertiesSetToFalse()
    {
        // Arrange
        var options = new CqrsValidationOptions
        {
            ValidateQueries = true,
            ValidateCommands = true
        };

        // Act
        options.ValidateQueries = false;
        options.ValidateCommands = false;

        // Assert
        options.ValidateQueries.Should().BeFalse();
        options.ValidateCommands.Should().BeFalse();
    }

    [Fact]
    public void Options_CanHaveMixedPropertyValues()
    {
        // Arrange & Act
        var options = new CqrsValidationOptions
        {
            ValidateQueries = true,
            ValidateCommands = false
        };

        // Assert
        options.ValidateQueries.Should().BeTrue();
        options.ValidateCommands.Should().BeFalse();
    }

    #endregion

    #region Object Initializer Tests

    [Fact]
    public void Options_CanBeInitializedWithObjectInitializer()
    {
        // Arrange & Act
        var options = new CqrsValidationOptions
        {
            ValidateQueries = true,
            ValidateCommands = true
        };

        // Assert
        options.ValidateQueries.Should().BeTrue();
        options.ValidateCommands.Should().BeTrue();
    }

    #endregion
}
