using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class GeneralExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_CreatesException()
    {
        // Arrange & Act
        var exception = new GeneralException();

        // Assert
        exception.Should().NotBeNull();
        exception.Severity.Should().Be(SeverityOptions.High); // Default severity
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new GeneralException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.DebugMessage.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithSeverityAndMessage_SetsBothProperties()
    {
        // Arrange
        var message = "Critical error";
        var severity = SeverityOptions.Critical;

        // Act
        var exception = new GeneralException(severity, message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Severity.Should().Be(SeverityOptions.Critical);
    }

    [Fact]
    public void Constructor_WithMessageAndParams_FormatsMessage()
    {
        // Arrange
        var message = "Error in {0} at line {1}";
        var param1 = "MyClass";
        var param2 = 42;

        // Act
        var exception = new GeneralException(message, param1, param2);

        // Assert
        exception.Message.Should().Be("Error in MyClass at line 42");
        exception.DebugMessage.Should().Be("Error in MyClass at line 42");
    }

    [Fact]
    public void Constructor_WithSeverityMessageAndParams_FormatsMessageAndSetsSeverity()
    {
        // Arrange
        var message = "Warning: {0}";
        var param = "Low disk space";

        // Act
        var exception = new GeneralException(SeverityOptions.Medium, message, param);

        // Assert
        exception.Message.Should().Be("Warning: Low disk space");
        exception.Severity.Should().Be(SeverityOptions.Medium);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = "Outer error";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new GeneralException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithMessageInnerExceptionAndParams_FormatsMessage()
    {
        // Arrange
        var message = "Error processing {0}";
        var innerException = new Exception("Inner");
        var param = "data.json";

        // Act
        var exception = new GeneralException(message, innerException, param);

        // Assert
        exception.Message.Should().Be("Error processing data.json");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region Severity Property Tests

    [Fact]
    public void Severity_CanBeSetAndRetrieved()
    {
        // Arrange
        var exception = new GeneralException();

        // Act
        exception.Severity = SeverityOptions.Low;

        // Assert
        exception.Severity.Should().Be(SeverityOptions.Low);
    }

    [Theory]
    [InlineData(SeverityOptions.Low)]
    [InlineData(SeverityOptions.Medium)]
    [InlineData(SeverityOptions.High)]
    [InlineData(SeverityOptions.Critical)]
    public void Severity_AcceptsAllSeverityOptions(SeverityOptions severity)
    {
        // Arrange
        var exception = new GeneralException();

        // Act
        exception.Severity = severity;

        // Assert
        exception.Severity.Should().Be(severity);
    }

    #endregion

    #region Message Property Tests

    [Fact]
    public void Message_WithNoParams_ReturnsOriginalMessage()
    {
        // Arrange
        var message = "Simple error message";
        var exception = new GeneralException(message);

        // Act
        var result = exception.Message;

        // Assert
        result.Should().Be(message);
    }

    [Fact]
    public void Message_WithParams_ReturnsFormattedMessage()
    {
        // Arrange
        var exception = new GeneralException("Value: {0}, Status: {1}", 100, "Active");

        // Act
        var result = exception.Message;

        // Assert
        result.Should().Be("Value: 100, Status: Active");
    }

    #endregion

    #region DebugMessage Property Tests

    [Fact]
    public void DebugMessage_WithNoParams_ReturnsOriginalMessage()
    {
        // Arrange
        var message = "Debug info";
        var exception = new GeneralException(message);

        // Act
        var result = exception.DebugMessage;

        // Assert
        result.Should().Be(message);
    }

    [Fact]
    public void DebugMessage_WithParams_ReturnsFormattedMessage()
    {
        // Arrange
        var exception = new GeneralException("Debug: {0} - {1}", "Test", 123);

        // Act
        var result = exception.DebugMessage;

        // Assert
        result.Should().Be("Debug: Test - 123");
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void GeneralException_InheritsFromBaseApplicationException()
    {
        // Arrange & Act
        var exception = new GeneralException();

        // Assert
        exception.Should().BeAssignableTo<BaseApplicationException>();
    }

    [Fact]
    public void GeneralException_InheritsEnvironmentInfoFromBase()
    {
        // Arrange & Act
        var exception = new GeneralException("Test");

        // Assert
        exception.MachineName.Should().NotBeNullOrEmpty();
        exception.AppDomainName.Should().NotBeNullOrEmpty();
        exception.CreatedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Exception Throwing Tests

    [Fact]
    public void GeneralException_CanBeThrown()
    {
        // Arrange & Act
        Action act = () => throw new GeneralException("Test error");

        // Assert
        act.Should().Throw<GeneralException>().WithMessage("Test error");
    }

    [Fact]
    public void GeneralException_CanBeCaughtAsBaseApplicationException()
    {
        // Arrange & Act
        GeneralException? caughtException = null;
        try
        {
            throw new GeneralException("Test");
        }
        catch (BaseApplicationException ex)
        {
            caughtException = ex as GeneralException;
        }

        // Assert
        caughtException.Should().NotBeNull();
    }

    #endregion
}
