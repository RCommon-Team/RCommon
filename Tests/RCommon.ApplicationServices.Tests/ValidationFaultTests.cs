using FluentAssertions;
using RCommon.ApplicationServices.Validation;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class ValidationFaultTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptyFault()
    {
        // Arrange & Act
        var fault = new ValidationFault();

        // Assert
        fault.PropertyName.Should().BeNull();
        fault.ErrorMessage.Should().BeNull();
        fault.AttemptedValue.Should().BeNull();
        fault.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void Constructor_WithPropertyNameAndMessage_SetsProperties()
    {
        // Arrange
        var propertyName = "TestProperty";
        var errorMessage = "Test error message";

        // Act
        var fault = new ValidationFault(propertyName, errorMessage);

        // Assert
        fault.PropertyName.Should().Be(propertyName);
        fault.ErrorMessage.Should().Be(errorMessage);
        fault.AttemptedValue.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var propertyName = "TestProperty";
        var errorMessage = "Test error message";
        var attemptedValue = "Invalid Value";

        // Act
        var fault = new ValidationFault(propertyName, errorMessage, attemptedValue);

        // Assert
        fault.PropertyName.Should().Be(propertyName);
        fault.ErrorMessage.Should().Be(errorMessage);
        fault.AttemptedValue.Should().Be(attemptedValue);
    }

    [Fact]
    public void PropertyName_CanBeSetAndGet()
    {
        // Arrange
        var fault = new ValidationFault();

        // Act
        fault.PropertyName = "MyProperty";

        // Assert
        fault.PropertyName.Should().Be("MyProperty");
    }

    [Fact]
    public void ErrorMessage_CanBeSetAndGet()
    {
        // Arrange
        var fault = new ValidationFault();

        // Act
        fault.ErrorMessage = "My error message";

        // Assert
        fault.ErrorMessage.Should().Be("My error message");
    }

    [Fact]
    public void AttemptedValue_CanBeSetAndGet()
    {
        // Arrange
        var fault = new ValidationFault();
        var value = new { Name = "Test" };

        // Act
        fault.AttemptedValue = value;

        // Assert
        fault.AttemptedValue.Should().Be(value);
    }

    [Fact]
    public void CustomState_CanBeSetAndGet()
    {
        // Arrange
        var fault = new ValidationFault();
        var customState = new { Extra = "Info" };

        // Act
        fault.CustomState = customState;

        // Assert
        fault.CustomState.Should().Be(customState);
    }

    [Theory]
    [InlineData(Severity.Error)]
    [InlineData(Severity.Warning)]
    [InlineData(Severity.Info)]
    public void Severity_CanBeSetToAllValues(Severity severity)
    {
        // Arrange
        var fault = new ValidationFault();

        // Act
        fault.Severity = severity;

        // Assert
        fault.Severity.Should().Be(severity);
    }

    [Fact]
    public void Severity_DefaultsToError()
    {
        // Arrange & Act
        var fault = new ValidationFault();

        // Assert
        fault.Severity.Should().Be(Severity.Error);
    }

    [Fact]
    public void ErrorCode_CanBeSetAndGet()
    {
        // Arrange
        var fault = new ValidationFault();

        // Act
        fault.ErrorCode = "ERR001";

        // Assert
        fault.ErrorCode.Should().Be("ERR001");
    }

    [Fact]
    public void FormattedMessagePlaceholderValues_CanBeSetAndGet()
    {
        // Arrange
        var fault = new ValidationFault();
        var placeholders = new Dictionary<string, object>
        {
            { "MinLength", 5 },
            { "MaxLength", 100 }
        };

        // Act
        fault.FormattedMessagePlaceholderValues = placeholders;

        // Assert
        fault.FormattedMessagePlaceholderValues.Should().BeEquivalentTo(placeholders);
    }

    [Fact]
    public void ToString_ReturnsErrorMessage()
    {
        // Arrange
        var errorMessage = "This is the error message";
        var fault = new ValidationFault("Property", errorMessage);

        // Act
        var result = fault.ToString();

        // Assert
        result.Should().Be(errorMessage);
    }

    [Fact]
    public void ToString_WhenErrorMessageIsNull_ReturnsNull()
    {
        // Arrange
        var fault = new ValidationFault();

        // Act
        var result = fault.ToString();

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("Name", "Name is required", "")]
    [InlineData("Email", "Invalid email format", "invalid-email")]
    [InlineData("Age", "Age must be positive", -5)]
    public void Constructor_WithDifferentValues_SetsCorrectly(
        string propertyName, string errorMessage, object attemptedValue)
    {
        // Arrange & Act
        var fault = new ValidationFault(propertyName, errorMessage, attemptedValue);

        // Assert
        fault.PropertyName.Should().Be(propertyName);
        fault.ErrorMessage.Should().Be(errorMessage);
        fault.AttemptedValue.Should().Be(attemptedValue);
    }

    [Fact]
    public void AllProperties_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var fault = new ValidationFault("Original", "Original message", "original value");

        // Act
        fault.PropertyName = "Modified";
        fault.ErrorMessage = "Modified message";
        fault.AttemptedValue = "modified value";
        fault.Severity = Severity.Warning;
        fault.ErrorCode = "MOD001";
        fault.CustomState = "custom";
        fault.FormattedMessagePlaceholderValues = new Dictionary<string, object> { { "key", "value" } };

        // Assert
        fault.PropertyName.Should().Be("Modified");
        fault.ErrorMessage.Should().Be("Modified message");
        fault.AttemptedValue.Should().Be("modified value");
        fault.Severity.Should().Be(Severity.Warning);
        fault.ErrorCode.Should().Be("MOD001");
        fault.CustomState.Should().Be("custom");
        fault.FormattedMessagePlaceholderValues.Should().ContainKey("key");
    }
}
