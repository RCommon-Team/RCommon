using FluentAssertions;
using RCommon.Persistence.Dapper;
using Xunit;

namespace RCommon.Dapper.Tests;

public class DapperFluentMappingsExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_CreatesInstance()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var exception = new DapperFluentMappingsException(message);

        // Assert
        exception.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var expectedMessage = "Test exception message";

        // Act
        var exception = new DapperFluentMappingsException(expectedMessage);

        // Assert
        exception.Message.Should().Be(expectedMessage);
    }

    [Fact]
    public void Exception_IsGeneralException()
    {
        // Arrange
        var exception = new DapperFluentMappingsException("Test message");

        // Act & Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Fact]
    public void Exception_IsException()
    {
        // Arrange
        var exception = new DapperFluentMappingsException("Test message");

        // Act & Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Severity_IsCritical()
    {
        // Arrange
        var exception = new DapperFluentMappingsException("Test message");

        // Act
        var severity = exception.Severity;

        // Assert
        severity.Should().Be(SeverityOptions.Critical);
    }

    [Theory]
    [InlineData("Fluent mappings configuration failed")]
    [InlineData("Unable to map entity to table")]
    [InlineData("Column mapping error")]
    public void Constructor_WithVariousMessages_SetsCorrectMessage(string message)
    {
        // Arrange & Act
        var exception = new DapperFluentMappingsException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception";

        // Act
        Action action = () => throw new DapperFluentMappingsException(message);

        // Assert
        action.Should().Throw<DapperFluentMappingsException>()
            .WithMessage(message);
    }

    [Fact]
    public void Exception_CanBeCaught()
    {
        // Arrange
        var message = "Test exception message";
        DapperFluentMappingsException? caughtException = null;

        // Act
        try
        {
            throw new DapperFluentMappingsException(message);
        }
        catch (DapperFluentMappingsException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_CanBeCaughtAsGeneralException()
    {
        // Arrange
        var message = "Test exception";
        GeneralException? caughtException = null;

        // Act
        try
        {
            throw new DapperFluentMappingsException(message);
        }
        catch (GeneralException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<DapperFluentMappingsException>();
    }

    [Fact]
    public void DebugMessage_MatchesMessage()
    {
        // Arrange
        var message = "Test debug message";
        var exception = new DapperFluentMappingsException(message);

        // Act
        var debugMessage = exception.DebugMessage;

        // Assert
        debugMessage.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_CreatesInstanceWithEmptyMessage()
    {
        // Arrange & Act
        var exception = new DapperFluentMappingsException(string.Empty);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithWhitespaceMessage_CreatesInstanceWithWhitespaceMessage()
    {
        // Arrange
        var whitespaceMessage = "   ";

        // Act
        var exception = new DapperFluentMappingsException(whitespaceMessage);

        // Assert
        exception.Message.Should().Be(whitespaceMessage);
    }

    [Fact]
    public void Exception_HasCorrectTypeName()
    {
        // Arrange
        var exception = new DapperFluentMappingsException("Test");

        // Act
        var typeName = exception.GetType().Name;

        // Assert
        typeName.Should().Be("DapperFluentMappingsException");
    }

    [Fact]
    public void Exception_InCorrectNamespace()
    {
        // Arrange
        var exception = new DapperFluentMappingsException("Test");

        // Act
        var ns = exception.GetType().Namespace;

        // Assert
        ns.Should().Be("RCommon.Persistence.Dapper");
    }
}
