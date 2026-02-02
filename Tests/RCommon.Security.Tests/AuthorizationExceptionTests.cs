using FluentAssertions;
using Microsoft.Extensions.Logging;
using RCommon.Security.Authorization;
using Xunit;

namespace RCommon.Security.Tests;

public class AuthorizationExceptionTests
{
    [Fact]
    public void Constructor_Default_SetsLogLevelToWarning()
    {
        // Act
        var exception = new AuthorizationException();

        // Assert
        exception.LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "Access denied";

        // Act
        var exception = new AuthorizationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = "Authorization failed";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new AuthorizationException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void Constructor_WithMessageCodeAndInnerException_SetsAllProperties()
    {
        // Arrange
        var message = "Permission denied";
        var code = "AUTH_001";
        var innerException = new Exception("Inner");

        // Act
        var exception = new AuthorizationException(message, code, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.Code.Should().Be(code);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.LogLevel.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void LogLevel_CanBeChanged()
    {
        // Arrange
        var exception = new AuthorizationException();

        // Act
        exception.LogLevel = LogLevel.Error;

        // Assert
        exception.LogLevel.Should().Be(LogLevel.Error);
    }

    [Fact]
    public void WithData_AddsDataToException()
    {
        // Arrange
        var exception = new AuthorizationException("Test");

        // Act
        var result = exception.WithData("key", "value");

        // Assert
        result.Should().BeSameAs(exception);
        exception.Data["key"].Should().Be("value");
    }

    [Fact]
    public void WithData_CanBeChained()
    {
        // Arrange
        var exception = new AuthorizationException("Test");

        // Act
        var result = exception
            .WithData("key1", "value1")
            .WithData("key2", 123)
            .WithData("key3", true);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Data["key1"].Should().Be("value1");
        exception.Data["key2"].Should().Be(123);
        exception.Data["key3"].Should().Be(true);
    }

    [Fact]
    public void Code_IsReadOnly()
    {
        // Arrange
        var exception = new AuthorizationException("msg", "CODE_1", null);

        // Assert
        exception.Code.Should().Be("CODE_1");
        typeof(AuthorizationException).GetProperty("Code")!.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void Code_CanBeNull()
    {
        // Arrange
        var exception = new AuthorizationException("msg", null, null);

        // Assert
        exception.Code.Should().BeNull();
    }

    [Fact]
    public void Exception_DerivesFromApplicationException()
    {
        // Act
        var exception = new AuthorizationException();

        // Assert
        exception.Should().BeAssignableTo<ApplicationException>();
    }

    [Fact]
    public void Constructor_WithNullMessage_AllowsNullMessage()
    {
        // Act
        var exception = new AuthorizationException(null, "CODE", null);

        // Assert
        exception.Message.Should().NotBeNull(); // ApplicationException provides default message
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    public void LogLevel_AcceptsAllLogLevels(LogLevel level)
    {
        // Arrange
        var exception = new AuthorizationException();

        // Act
        exception.LogLevel = level;

        // Assert
        exception.LogLevel.Should().Be(level);
    }
}
