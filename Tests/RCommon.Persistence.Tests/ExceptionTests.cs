using FluentAssertions;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Sql;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class DataStoreNotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_CreatesExceptionWithMessage()
    {
        // Arrange
        var message = "Test data store not found";

        // Act
        var exception = new DataStoreNotFoundException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new DataStoreNotFoundException("test");

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("DataStore with name of TestStore not found")]
    [InlineData("Custom error message")]
    public void Constructor_WithVariousMessages_SetsMessageCorrectly(string message)
    {
        // Arrange & Act
        var exception = new DataStoreNotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception";

        // Act
        Action action = () => throw new DataStoreNotFoundException(message);

        // Assert
        action.Should().Throw<DataStoreNotFoundException>()
            .WithMessage(message);
    }
}

public class UnsupportedDataStoreExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_CreatesExceptionWithMessage()
    {
        // Arrange
        var message = "Unsupported data store";

        // Act
        var exception = new UnsupportedDataStoreException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new UnsupportedDataStoreException("test");

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Concrete type must implement base type.")]
    [InlineData("You cannot register a data store with the same name")]
    public void Constructor_WithVariousMessages_SetsMessageCorrectly(string message)
    {
        // Arrange & Act
        var exception = new UnsupportedDataStoreException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception";

        // Act
        Action action = () => throw new UnsupportedDataStoreException(message);

        // Assert
        action.Should().Throw<UnsupportedDataStoreException>()
            .WithMessage(message);
    }
}

public class PersistenceExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Persistence error occurred";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new PersistenceException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Exception_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new PersistenceException("test", new Exception());

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Fact]
    public void Constructor_WithNullInnerException_SetsInnerExceptionToNull()
    {
        // Arrange & Act
        var exception = new PersistenceException("test", null!);

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception";
        var inner = new Exception("Inner");

        // Act
        Action action = () => throw new PersistenceException(message, inner);

        // Assert
        action.Should().Throw<PersistenceException>()
            .WithMessage(message)
            .WithInnerException<Exception>();
    }
}

public class RepositoryExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndInnerException_CreatesException()
    {
        // Arrange
        var message = "Repository error occurred";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new RepositoryException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Exception_InheritsFromApplicationException()
    {
        // Arrange & Act
        var exception = new RepositoryException("test", new Exception());

        // Assert
        exception.Should().BeAssignableTo<ApplicationException>();
    }

    [Fact]
    public void Constructor_WithNullInnerException_SetsInnerExceptionToNull()
    {
        // Arrange & Act
        var exception = new RepositoryException("test", null!);

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception";
        var inner = new Exception("Inner");

        // Act
        Action action = () => throw new RepositoryException(message, inner);

        // Assert
        action.Should().Throw<RepositoryException>()
            .WithMessage(message)
            .WithInnerException<Exception>();
    }
}

public class UnitOfWorkExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_CreatesExceptionWithMessage()
    {
        // Arrange
        var message = "Unit of work error";

        // Act
        var exception = new UnitOfWorkException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new UnitOfWorkException("test");

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Cannot commit a disposed UnitOfWorkScope instance")]
    [InlineData("This unit of work scope has been marked completed")]
    public void Constructor_WithVariousMessages_SetsMessageCorrectly(string message)
    {
        // Arrange & Act
        var exception = new UnitOfWorkException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception";

        // Act
        Action action = () => throw new UnitOfWorkException(message);

        // Assert
        action.Should().Throw<UnitOfWorkException>()
            .WithMessage(message);
    }
}

public class RDbConnectionExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_CreatesExceptionWithMessage()
    {
        // Arrange
        var message = "Database connection error";

        // Act
        var exception = new RDbConnectionException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new RDbConnectionException("test");

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("No options configured for this RDbConnection")]
    [InlineData("You must configure a connection string for this RDbConnection")]
    public void Constructor_WithVariousMessages_SetsMessageCorrectly(string message)
    {
        // Arrange & Act
        var exception = new RDbConnectionException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Exception_CanBeThrown()
    {
        // Arrange
        var message = "Test exception";

        // Act
        Action action = () => throw new RDbConnectionException(message);

        // Assert
        action.Should().Throw<RDbConnectionException>()
            .WithMessage(message);
    }
}
