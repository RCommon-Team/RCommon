using FluentAssertions;
using RCommon.ApplicationServices;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class CqrsValidationOptionsTests
{
    [Fact]
    public void Constructor_Default_SetsValidateQueriesToFalse()
    {
        // Arrange & Act
        var options = new CqrsValidationOptions();

        // Assert
        options.ValidateQueries.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Default_SetsValidateCommandsToFalse()
    {
        // Arrange & Act
        var options = new CqrsValidationOptions();

        // Assert
        options.ValidateCommands.Should().BeFalse();
    }

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

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void AllCombinations_CanBeSet(bool validateQueries, bool validateCommands)
    {
        // Arrange
        var options = new CqrsValidationOptions();

        // Act
        options.ValidateQueries = validateQueries;
        options.ValidateCommands = validateCommands;

        // Assert
        options.ValidateQueries.Should().Be(validateQueries);
        options.ValidateCommands.Should().Be(validateCommands);
    }

    [Fact]
    public void Options_AreIndependent()
    {
        // Arrange
        var options = new CqrsValidationOptions();

        // Act
        options.ValidateQueries = true;
        options.ValidateCommands = false;

        // Assert
        options.ValidateQueries.Should().BeTrue();
        options.ValidateCommands.Should().BeFalse();
    }

    [Fact]
    public void ObjectInitializer_SetsProperties()
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
}
