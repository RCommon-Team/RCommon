using FluentAssertions;
using RCommon.ApplicationServices.Validation;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class ValidationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessageAndEmptyErrors()
    {
        // Arrange
        var message = "Validation failed";

        // Act
        var exception = new ValidationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithMessageAndErrors_SetsBothProperties()
    {
        // Arrange
        var message = "Validation failed";
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Property1", "Error 1"),
            new ValidationFault("Property2", "Error 2")
        };

        // Act
        var exception = new ValidationException(message, errors);

        // Assert
        exception.Message.Should().Be(message);
        exception.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Constructor_WithErrors_BuildsMessageFromErrors()
    {
        // Arrange
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Name", "Name is required") { Severity = Severity.Error },
            new ValidationFault("Email", "Invalid email") { Severity = Severity.Warning }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Name");
        exception.Message.Should().Contain("Name is required");
        exception.Message.Should().Contain("Email");
        exception.Message.Should().Contain("Invalid email");
        exception.Errors.Should().BeSameAs(errors);
    }

    [Fact]
    public void Constructor_WithMessageErrorsAndAppendFalse_UsesOnlyCustomMessage()
    {
        // Arrange
        var customMessage = "Custom validation error";
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Property", "Error message")
        };

        // Act
        var exception = new ValidationException(customMessage, errors, appendDefaultMessage: false);

        // Assert
        exception.Message.Should().Be(customMessage);
        exception.Errors.Should().BeSameAs(errors);
    }

    [Fact]
    public void Constructor_WithMessageErrorsAndAppendTrue_AppendsDefaultMessage()
    {
        // Arrange
        var customMessage = "Custom validation error";
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Property", "Error message") { Severity = Severity.Error }
        };

        // Act
        var exception = new ValidationException(customMessage, errors, appendDefaultMessage: true);

        // Assert
        exception.Message.Should().StartWith(customMessage);
        exception.Message.Should().Contain("Validation failed");
        exception.Message.Should().Contain("Property");
        exception.Message.Should().Contain("Error message");
    }

    [Fact]
    public void Errors_ContainsAllProvidedErrors()
    {
        // Arrange
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Prop1", "Error 1"),
            new ValidationFault("Prop2", "Error 2"),
            new ValidationFault("Prop3", "Error 3")
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().HaveCount(3);
        exception.Errors.Should().Contain(e => e.PropertyName == "Prop1");
        exception.Errors.Should().Contain(e => e.PropertyName == "Prop2");
        exception.Errors.Should().Contain(e => e.PropertyName == "Prop3");
    }

    [Fact]
    public void Constructor_WithEmptyErrors_CreatesEmptyErrorsList()
    {
        // Arrange
        var errors = new List<ValidationFault>();

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Errors.Should().BeEmpty();
        exception.Message.Should().Contain("Validation failed");
    }

    [Fact]
    public void Exception_IsSerializable()
    {
        // Arrange
        var exception = new ValidationException("Test message");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
        exception.GetType().Should().BeDecoratedWith<SerializableAttribute>();
    }

    [Fact]
    public void ErrorMessage_IncludesSeverityInformation()
    {
        // Arrange
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Property", "Error") { Severity = Severity.Warning }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Contain("Warning");
    }

    [Theory]
    [InlineData(Severity.Error, "Error")]
    [InlineData(Severity.Warning, "Warning")]
    [InlineData(Severity.Info, "Info")]
    public void ErrorMessage_IncludesCorrectSeverityString(Severity severity, string expectedSeverityString)
    {
        // Arrange
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Property", "Error message") { Severity = severity }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Contain(expectedSeverityString);
    }

    [Fact]
    public void ErrorMessage_FormattedCorrectly()
    {
        // Arrange
        var errors = new List<ValidationFault>
        {
            new ValidationFault("Name", "Name is required") { Severity = Severity.Error }
        };

        // Act
        var exception = new ValidationException(errors);

        // Assert
        exception.Message.Should().Contain("-- Name: Name is required Severity: Error");
    }

    [Fact]
    public void Constructor_InheritsFromException()
    {
        // Arrange & Act
        var exception = new ValidationException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}
