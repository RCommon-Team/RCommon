using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class BaseApplicationExceptionTests
{
    #region Constructor Tests

    [Fact]
    public void DefaultConstructor_CreatesExceptionWithEnvironmentInfo()
    {
        // Arrange & Act
        var exception = new BaseApplicationException();

        // Assert
        exception.Should().NotBeNull();
        exception.MachineName.Should().NotBeNullOrEmpty();
        exception.AppDomainName.Should().NotBeNullOrEmpty();
        exception.CreatedDateTime.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var exception = new BaseApplicationException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBothProperties()
    {
        // Arrange
        var message = "Outer exception";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new BaseApplicationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    #endregion

    #region Property Tests

    [Fact]
    public void MachineName_ReturnsCurrentMachineName()
    {
        // Arrange
        var exception = new BaseApplicationException();

        // Act
        var machineName = exception.MachineName;

        // Assert
        machineName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreatedDateTime_ReturnsTimeOfCreation()
    {
        // Arrange
        var beforeCreation = DateTime.Now;

        // Act
        var exception = new BaseApplicationException();
        var afterCreation = DateTime.Now;

        // Assert
        exception.CreatedDateTime.Should().BeOnOrAfter(beforeCreation);
        exception.CreatedDateTime.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void AppDomainName_ReturnsCurrentAppDomainName()
    {
        // Arrange
        var exception = new BaseApplicationException();

        // Act
        var appDomainName = exception.AppDomainName;

        // Assert
        appDomainName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ThreadIdentityName_HasValue()
    {
        // Arrange
        var exception = new BaseApplicationException();

        // Act
        var threadIdentity = exception.ThreadIdentityName;

        // Assert - ThreadIdentityName may be null if no thread principal is set
        // The property is expected to exist but may be null or empty
        // Just verify it doesn't throw when accessed
        threadIdentity.Should().BeOneOf(null, string.Empty, "Permission Denied");
    }

    [Fact]
    public void WindowsIdentityName_HasValue()
    {
        // Arrange
        var exception = new BaseApplicationException();

        // Act
        var windowsIdentity = exception.WindowsIdentityName;

        // Assert - WindowsIdentityName may be null if no windows identity
        // Just verify it doesn't throw when accessed
        windowsIdentity.Should().BeOneOf(null, string.Empty, "Permission Denied");
    }

    [Fact]
    public void AdditionalInformation_IsInitializedAsEmptyCollection()
    {
        // Arrange
        var exception = new BaseApplicationException();

        // Act
        var additionalInfo = exception.AdditionalInformation;

        // Assert
        additionalInfo.Should().NotBeNull();
        additionalInfo.Count.Should().Be(0);
    }

    [Fact]
    public void AdditionalInformation_CanAddKeyValuePairs()
    {
        // Arrange
        var exception = new BaseApplicationException();

        // Act
        exception.AdditionalInformation.Add("key1", "value1");
        exception.AdditionalInformation.Add("key2", "value2");

        // Assert
        exception.AdditionalInformation["key1"].Should().Be("value1");
        exception.AdditionalInformation["key2"].Should().Be("value2");
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void BaseApplicationException_InheritsFromApplicationException()
    {
        // Arrange & Act
        var exception = new BaseApplicationException();

        // Assert
        exception.Should().BeAssignableTo<ApplicationException>();
    }

    [Fact]
    public void BaseApplicationException_InheritsFromException()
    {
        // Arrange & Act
        var exception = new BaseApplicationException();

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region Exception Throwing Tests

    [Fact]
    public void BaseApplicationException_CanBeThrown()
    {
        // Arrange & Act
        Action act = () => throw new BaseApplicationException("Test");

        // Assert
        act.Should().Throw<BaseApplicationException>().WithMessage("Test");
    }

    [Fact]
    public void BaseApplicationException_CanBeCaughtAsApplicationException()
    {
        // Arrange & Act
        BaseApplicationException? caughtException = null;
        try
        {
            throw new BaseApplicationException("Test");
        }
        catch (ApplicationException ex)
        {
            caughtException = ex as BaseApplicationException;
        }

        // Assert
        caughtException.Should().NotBeNull();
    }

    #endregion
}
