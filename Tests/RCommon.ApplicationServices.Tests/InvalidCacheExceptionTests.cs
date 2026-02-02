using FluentAssertions;
using RCommon.ApplicationServices;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class InvalidCacheExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Invalid cache configuration";

        // Act
        var exception = new InvalidCacheException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_SetsEmptyMessage()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var exception = new InvalidCacheException(message);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void Exception_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new InvalidCacheException("Test");

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Fact]
    public void Exception_InheritsFromException()
    {
        // Arrange & Act
        var exception = new InvalidCacheException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Cache factory not available";

        // Act
        Action act = () => throw new InvalidCacheException(message);

        // Assert
        act.Should().Throw<InvalidCacheException>()
            .WithMessage(message);
    }

    [Fact]
    public void Exception_CanBeCaught()
    {
        // Arrange
        var message = "Test cache exception message";
        Exception? caughtException = null;

        // Act
        try
        {
            throw new InvalidCacheException(message);
        }
        catch (InvalidCacheException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<InvalidCacheException>();
        caughtException!.Message.Should().Be(message);
    }

    [Theory]
    [InlineData("Cache service not configured")]
    [InlineData("ICommonFactory not available")]
    [InlineData("We could not properly inject the caching factory")]
    public void Constructor_WithDifferentMessages_SetsCorrectMessage(string message)
    {
        // Arrange & Act
        var exception = new InvalidCacheException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_HasNullInnerException()
    {
        // Arrange & Act
        var exception = new InvalidCacheException("Test");

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Exception_CanBeCaughtAsGeneralException()
    {
        // Arrange
        var message = "Test exception message";
        GeneralException? caughtException = null;

        // Act
        try
        {
            throw new InvalidCacheException(message);
        }
        catch (GeneralException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<InvalidCacheException>();
    }

    [Fact]
    public void Exception_CanBeCaughtAsBaseException()
    {
        // Arrange
        var message = "Test exception message";
        Exception? caughtException = null;

        // Act
        try
        {
            throw new InvalidCacheException(message);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<InvalidCacheException>();
    }

    [Fact]
    public void Exception_MessageContainsCacheInformation()
    {
        // Arrange
        var factoryName = "ICommonFactory<ExpressionCachingStrategy, ICacheService>";
        var message = $"We could not properly inject the caching factory: '{factoryName}' into the CommandBus";

        // Act
        var exception = new InvalidCacheException(message);

        // Assert
        exception.Message.Should().Contain(factoryName);
        exception.Message.Should().Contain("CommandBus");
    }

    [Fact]
    public void Exception_DefaultSeverityIsHigh()
    {
        // Arrange & Act
        var exception = new InvalidCacheException("Test");

        // Assert
        exception.Severity.Should().Be(SeverityOptions.High);
    }
}
