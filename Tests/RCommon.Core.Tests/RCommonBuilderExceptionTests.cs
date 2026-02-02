using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class RCommonBuilderExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Builder configuration error";

        // Act
        var exception = new RCommonBuilderException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_SetsSeverityToCritical()
    {
        // Arrange & Act
        var exception = new RCommonBuilderException("Test");

        // Assert
        exception.Severity.Should().Be(SeverityOptions.Critical);
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void RCommonBuilderException_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new RCommonBuilderException("Test");

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Fact]
    public void RCommonBuilderException_InheritsFromBaseApplicationException()
    {
        // Arrange & Act
        var exception = new RCommonBuilderException("Test");

        // Assert
        exception.Should().BeAssignableTo<BaseApplicationException>();
    }

    [Fact]
    public void RCommonBuilderException_InheritsEnvironmentInfo()
    {
        // Arrange & Act
        var exception = new RCommonBuilderException("Test");

        // Assert
        exception.MachineName.Should().NotBeNullOrEmpty();
        exception.AppDomainName.Should().NotBeNullOrEmpty();
        exception.CreatedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Exception Throwing Tests

    [Fact]
    public void RCommonBuilderException_CanBeThrown()
    {
        // Arrange & Act
        Action act = () => throw new RCommonBuilderException("Configuration failed");

        // Assert
        act.Should().Throw<RCommonBuilderException>().WithMessage("Configuration failed");
    }

    [Fact]
    public void RCommonBuilderException_CanBeCaughtAsGeneralException()
    {
        // Arrange & Act
        RCommonBuilderException? caughtException = null;
        try
        {
            throw new RCommonBuilderException("Test");
        }
        catch (GeneralException ex)
        {
            caughtException = ex as RCommonBuilderException;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.Severity.Should().Be(SeverityOptions.Critical);
    }

    #endregion

    #region DebugMessage Tests

    [Fact]
    public void DebugMessage_ReturnsMessage()
    {
        // Arrange
        var message = "Builder error occurred";
        var exception = new RCommonBuilderException(message);

        // Act
        var debugMessage = exception.DebugMessage;

        // Assert
        debugMessage.Should().Be(message);
    }

    #endregion

    #region Critical Severity Tests

    [Fact]
    public void Severity_IsCriticalAndCannotBeModifiedViaConstructor()
    {
        // Arrange & Act
        var exception = new RCommonBuilderException("Test");

        // Assert
        exception.Severity.Should().Be(SeverityOptions.Critical);
    }

    [Fact]
    public void Severity_CanBeModifiedAfterConstruction_ButDefaultsAreCritical()
    {
        // Arrange
        var exception = new RCommonBuilderException("Test");

        // Act & Assert - verify default is critical
        exception.Severity.Should().Be(SeverityOptions.Critical);

        // Can modify if needed
        exception.Severity = SeverityOptions.High;
        exception.Severity.Should().Be(SeverityOptions.High);
    }

    #endregion

    #region AdditionalInformation Tests

    [Fact]
    public void AdditionalInformation_CanBeUsed()
    {
        // Arrange
        var exception = new RCommonBuilderException("Configuration error");

        // Act
        exception.AdditionalInformation.Add("ServiceType", "IGuidGenerator");
        exception.AdditionalInformation.Add("Reason", "Already configured");

        // Assert
        exception.AdditionalInformation["ServiceType"].Should().Be("IGuidGenerator");
        exception.AdditionalInformation["Reason"].Should().Be("Already configured");
    }

    #endregion
}
