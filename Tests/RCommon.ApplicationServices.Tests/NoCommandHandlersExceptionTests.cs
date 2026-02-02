using FluentAssertions;
using RCommon.ApplicationServices.Commands;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class NoCommandHandlersExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = "No command handlers registered for the command 'TestCommand'";

        // Act
        var exception = new NoCommandHandlersException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_SetsEmptyMessage()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var exception = new NoCommandHandlersException(message);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void Exception_InheritsFromException()
    {
        // Arrange & Act
        var exception = new NoCommandHandlersException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        Action act = () => throw new NoCommandHandlersException(message);

        // Assert
        act.Should().Throw<NoCommandHandlersException>()
            .WithMessage(message);
    }

    [Fact]
    public void Exception_CanBeCaught()
    {
        // Arrange
        var message = "Test exception message";
        Exception? caughtException = null;

        // Act
        try
        {
            throw new NoCommandHandlersException(message);
        }
        catch (NoCommandHandlersException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<NoCommandHandlersException>();
        caughtException!.Message.Should().Be(message);
    }

    [Theory]
    [InlineData("No handlers for CreateUserCommand")]
    [InlineData("No handlers for UpdateProductCommand")]
    [InlineData("No handlers for DeleteOrderCommand")]
    public void Constructor_WithDifferentMessages_SetsCorrectMessage(string message)
    {
        // Arrange & Act
        var exception = new NoCommandHandlersException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_HasNullInnerException()
    {
        // Arrange & Act
        var exception = new NoCommandHandlersException("Test");

        // Assert
        exception.InnerException.Should().BeNull();
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
            throw new NoCommandHandlersException(message);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<NoCommandHandlersException>();
    }

    [Fact]
    public void Exception_MessageContainsCommandTypeName()
    {
        // Arrange
        var commandTypeName = "MySpecificCommand";
        var message = $"No command handlers registered for the command '{commandTypeName}'";

        // Act
        var exception = new NoCommandHandlersException(message);

        // Assert
        exception.Message.Should().Contain(commandTypeName);
    }
}
