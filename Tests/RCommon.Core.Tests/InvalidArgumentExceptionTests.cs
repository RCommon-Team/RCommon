using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class InvalidArgumentExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_CreateExceptionWithLowSeverity()
    {
        // Arrange & Act
        var exception = new InvalidArgumentException();

        // Assert
        exception.Should().NotBeNull();
        exception.Severity.Should().Be(SeverityOptions.Low);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessageAndLowSeverity()
    {
        // Arrange
        var message = "Invalid argument provided";

        // Act
        var exception = new InvalidArgumentException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.Severity.Should().Be(SeverityOptions.Low);
    }

    [Fact]
    public void Constructor_WithMessageAndParams_FormatsMessageAndSetsLowSeverity()
    {
        // Arrange
        var message = "Parameter {0} has invalid value: {1}";
        var paramName = "userId";
        var value = -1;

        // Act
        var exception = new InvalidArgumentException(message, paramName, value);

        // Assert
        exception.Message.Should().Be("Parameter userId has invalid value: -1");
        exception.Severity.Should().Be(SeverityOptions.Low);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBothAndLowSeverity()
    {
        // Arrange
        var message = "Outer exception";
        var innerException = new ArgumentException("Inner exception");

        // Act
        var exception = new InvalidArgumentException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Severity.Should().Be(SeverityOptions.Low);
    }

    #endregion

    #region Severity Tests

    [Fact]
    public void Severity_IsAlwaysLowByDefault()
    {
        // Arrange & Act
        var exception1 = new InvalidArgumentException();
        var exception2 = new InvalidArgumentException("message");
        var exception3 = new InvalidArgumentException("message", new Exception());

        // Assert
        exception1.Severity.Should().Be(SeverityOptions.Low);
        exception2.Severity.Should().Be(SeverityOptions.Low);
        exception3.Severity.Should().Be(SeverityOptions.Low);
    }

    [Fact]
    public void Severity_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var exception = new InvalidArgumentException();

        // Act
        exception.Severity = SeverityOptions.High;

        // Assert
        exception.Severity.Should().Be(SeverityOptions.High);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void InvalidArgumentException_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new InvalidArgumentException();

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Fact]
    public void InvalidArgumentException_InheritsFromBaseApplicationException()
    {
        // Arrange & Act
        var exception = new InvalidArgumentException();

        // Assert
        exception.Should().BeAssignableTo<BaseApplicationException>();
    }

    [Fact]
    public void InvalidArgumentException_InheritsEnvironmentInfo()
    {
        // Arrange & Act
        var exception = new InvalidArgumentException("Test");

        // Assert
        exception.MachineName.Should().NotBeNullOrEmpty();
        exception.AppDomainName.Should().NotBeNullOrEmpty();
        exception.CreatedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Exception Throwing Tests

    [Fact]
    public void InvalidArgumentException_CanBeThrown()
    {
        // Arrange & Act
        Action act = () => throw new InvalidArgumentException("Invalid parameter");

        // Assert
        act.Should().Throw<InvalidArgumentException>().WithMessage("Invalid parameter");
    }

    [Fact]
    public void InvalidArgumentException_CanBeCaughtAsGeneralException()
    {
        // Arrange & Act
        InvalidArgumentException? caughtException = null;
        try
        {
            throw new InvalidArgumentException("Test");
        }
        catch (GeneralException ex)
        {
            caughtException = ex as InvalidArgumentException;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.Severity.Should().Be(SeverityOptions.Low);
    }

    #endregion

    #region DebugMessage Tests

    [Fact]
    public void DebugMessage_WithParams_FormatsCorrectly()
    {
        // Arrange
        var exception = new InvalidArgumentException("Argument '{0}' cannot be {1}", "count", "negative");

        // Act
        var debugMessage = exception.DebugMessage;

        // Assert
        debugMessage.Should().Be("Argument 'count' cannot be negative");
    }

    #endregion

    #region AdditionalInformation Tests

    [Fact]
    public void AdditionalInformation_CanBeUsed()
    {
        // Arrange
        var exception = new InvalidArgumentException("Invalid");

        // Act
        exception.AdditionalInformation.Add("ParameterName", "userId");
        exception.AdditionalInformation.Add("ProvidedValue", "-1");

        // Assert
        exception.AdditionalInformation["ParameterName"].Should().Be("userId");
        exception.AdditionalInformation["ProvidedValue"].Should().Be("-1");
    }

    #endregion
}
