using Bogus;
using FluentAssertions;
using RCommon.Entities;
using Xunit;

namespace RCommon.Entities.Tests;

/// <summary>
/// Unit tests for EntityNotFoundException class.
/// </summary>
public class EntityNotFoundExceptionTests
{
    private readonly Faker _faker;

    public EntityNotFoundExceptionTests()
    {
        _faker = new Faker();
    }

    #region Test Entities

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class AnotherTestEntity
    {
        public Guid Id { get; set; }
    }

    #endregion

    #region Default Constructor Tests

    [Fact]
    public void DefaultConstructor_CreatesException_WithNullProperties()
    {
        // Arrange & Act
        var exception = new EntityNotFoundException();

        // Assert
        exception.EntityType.Should().BeNull();
        exception.Id.Should().BeNull();
    }

    [Fact]
    public void DefaultConstructor_CreatesExceptionThatCanBeThrown()
    {
        // Arrange & Act
        Action act = () => throw new EntityNotFoundException();

        // Assert
        act.Should().Throw<EntityNotFoundException>();
    }

    #endregion

    #region Constructor With EntityType Tests

    [Fact]
    public void Constructor_WithEntityType_SetsEntityType()
    {
        // Arrange
        var entityType = typeof(TestEntity);

        // Act
        var exception = new EntityNotFoundException(entityType);

        // Assert
        exception.EntityType.Should().Be(entityType);
        exception.Id.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEntityType_GeneratesCorrectMessage()
    {
        // Arrange
        var entityType = typeof(TestEntity);

        // Act
        var exception = new EntityNotFoundException(entityType);

        // Assert
        exception.Message.Should().Contain("There is no such an entity");
        exception.Message.Should().Contain(entityType.FullName);
    }

    [Theory]
    [InlineData(typeof(TestEntity))]
    [InlineData(typeof(AnotherTestEntity))]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))]
    public void Constructor_WithVariousEntityTypes_SetsEntityTypeCorrectly(Type entityType)
    {
        // Arrange & Act
        var exception = new EntityNotFoundException(entityType);

        // Assert
        exception.EntityType.Should().Be(entityType);
    }

    #endregion

    #region Constructor With EntityType and Id Tests

    [Fact]
    public void Constructor_WithEntityTypeAndId_SetsProperties()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = _faker.Random.Int(1, 1000);

        // Act
        var exception = new EntityNotFoundException(entityType, id);

        // Assert
        exception.EntityType.Should().Be(entityType);
        exception.Id.Should().Be(id);
    }

    [Fact]
    public void Constructor_WithEntityTypeAndId_GeneratesCorrectMessage()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = _faker.Random.Int(1, 1000);

        // Act
        var exception = new EntityNotFoundException(entityType, id);

        // Assert
        exception.Message.Should().Contain("There is no such an entity");
        exception.Message.Should().Contain(entityType.FullName);
        exception.Message.Should().Contain(id.ToString());
    }

    [Fact]
    public void Constructor_WithEntityTypeAndGuidId_SetsProperties()
    {
        // Arrange
        var entityType = typeof(AnotherTestEntity);
        var id = _faker.Random.Guid();

        // Act
        var exception = new EntityNotFoundException(entityType, id);

        // Assert
        exception.EntityType.Should().Be(entityType);
        exception.Id.Should().Be(id);
    }

    [Fact]
    public void Constructor_WithEntityTypeAndStringId_SetsProperties()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = _faker.Random.AlphaNumeric(10);

        // Act
        var exception = new EntityNotFoundException(entityType, id);

        // Assert
        exception.EntityType.Should().Be(entityType);
        exception.Id.Should().Be(id);
    }

    [Fact]
    public void Constructor_WithEntityTypeAndNullId_HandlesGracefully()
    {
        // Arrange
        var entityType = typeof(TestEntity);

        // Act
        var exception = new EntityNotFoundException(entityType, null);

        // Assert
        exception.EntityType.Should().Be(entityType);
        exception.Id.Should().BeNull();
        exception.Message.Should().Contain("There is no such an entity");
    }

    #endregion

    #region Constructor With EntityType, Id, and InnerException Tests

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = _faker.Random.Int(1, 1000);
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new EntityNotFoundException(entityType, id, innerException);

        // Assert
        exception.EntityType.Should().Be(entityType);
        exception.Id.Should().Be(id);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithNullInnerException_HasNullInnerException()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = _faker.Random.Int(1, 1000);

        // Act
        var exception = new EntityNotFoundException(entityType, id, null);

        // Assert
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_PreservesInnerExceptionMessage()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = _faker.Random.Int(1, 1000);
        var innerMessage = _faker.Lorem.Sentence();
        var innerException = new Exception(innerMessage);

        // Act
        var exception = new EntityNotFoundException(entityType, id, innerException);

        // Assert
        exception.InnerException!.Message.Should().Be(innerMessage);
    }

    #endregion

    #region Constructor With Message Tests

    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        // Arrange
        var message = _faker.Lorem.Sentence();

        // Act
        var exception = new EntityNotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_SetsEmptyMessage()
    {
        // Arrange & Act
        var exception = new EntityNotFoundException(string.Empty);

        // Assert
        exception.Message.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Entity not found")]
    [InlineData("The requested resource does not exist")]
    [InlineData("No matching entity")]
    public void Constructor_WithVariousMessages_SetsMessageCorrectly(string message)
    {
        // Arrange & Act
        var exception = new EntityNotFoundException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    #endregion

    #region Constructor With Message and InnerException Tests

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsBoth()
    {
        // Arrange
        var message = _faker.Lorem.Sentence();
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new EntityNotFoundException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void Constructor_WithMessageAndNullInnerException_SetsOnlyMessage()
    {
        // Arrange
        var message = _faker.Lorem.Sentence();

        // Act
        var exception = new EntityNotFoundException(message, null);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    #endregion

    #region Inheritance Tests

    [Fact]
    public void EntityNotFoundException_InheritsFromGeneralException()
    {
        // Arrange & Act
        var exception = new EntityNotFoundException();

        // Assert
        exception.Should().BeAssignableTo<GeneralException>();
    }

    [Fact]
    public void EntityNotFoundException_InheritsFromException()
    {
        // Arrange & Act
        var exception = new EntityNotFoundException();

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    #endregion

    #region Throw and Catch Tests

    [Fact]
    public void EntityNotFoundException_CanBeThrownAndCaught()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = _faker.Random.Int(1, 1000);

        // Act
        EntityNotFoundException? caughtException = null;
        try
        {
            throw new EntityNotFoundException(entityType, id);
        }
        catch (EntityNotFoundException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException!.EntityType.Should().Be(entityType);
        caughtException.Id.Should().Be(id);
    }

    [Fact]
    public void EntityNotFoundException_CanBeCaughtAsGeneralException()
    {
        // Arrange
        var entityType = typeof(TestEntity);

        // Act
        GeneralException? caughtException = null;
        try
        {
            throw new EntityNotFoundException(entityType);
        }
        catch (GeneralException ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<EntityNotFoundException>();
    }

    [Fact]
    public void EntityNotFoundException_CanBeCaughtAsException()
    {
        // Arrange
        var message = _faker.Lorem.Sentence();

        // Act
        Exception? caughtException = null;
        try
        {
            throw new EntityNotFoundException(message);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<EntityNotFoundException>();
    }

    #endregion

    #region Message Format Tests

    [Fact]
    public void Message_WithEntityTypeOnly_ContainsEntityTypeFullName()
    {
        // Arrange
        var entityType = typeof(TestEntity);

        // Act
        var exception = new EntityNotFoundException(entityType);

        // Assert
        exception.Message.Should().Contain("RCommon.Entities.Tests.EntityNotFoundExceptionTests+TestEntity");
    }

    [Fact]
    public void Message_WithEntityTypeAndId_ContainsBothInMessage()
    {
        // Arrange
        var entityType = typeof(TestEntity);
        var id = 42;

        // Act
        var exception = new EntityNotFoundException(entityType, id);

        // Assert
        exception.Message.Should().Contain(entityType.FullName!);
        exception.Message.Should().Contain("42");
    }

    #endregion

    #region Property Mutability Tests

    [Fact]
    public void EntityType_CanBeSetAfterConstruction()
    {
        // Arrange
        var exception = new EntityNotFoundException();
        var newEntityType = typeof(AnotherTestEntity);

        // Act
        exception.EntityType = newEntityType;

        // Assert
        exception.EntityType.Should().Be(newEntityType);
    }

    [Fact]
    public void Id_CanBeSetAfterConstruction()
    {
        // Arrange
        var exception = new EntityNotFoundException();
        var newId = _faker.Random.Int(1, 1000);

        // Act
        exception.Id = newId;

        // Assert
        exception.Id.Should().Be(newId);
    }

    #endregion
}
