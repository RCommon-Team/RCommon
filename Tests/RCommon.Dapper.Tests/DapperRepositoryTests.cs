using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Dapper.Crud;
using RCommon.Persistence.Sql;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Dapper.Tests;

public class DapperRepositoryTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public DapperRepositoryTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _defaultOptions = new DefaultDataStoreOptions();

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullDataStoreFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new DapperRepository<TestDapperEntity>(
            null!,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("dataStoreFactory");
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new DapperRepository<TestDapperEntity>(
            _mockDataStoreFactory.Object,
            null!,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullEventTracker_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new DapperRepository<TestDapperEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            null!,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventTracker");
    }

    [Fact]
    public void Constructor_WithNullDefaultDataStoreOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new DapperRepository<TestDapperEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("defaultDataStoreOptions");
    }

    [Fact]
    public void Constructor_CreatesLogger()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        _mockLoggerFactory.Verify(x => x.CreateLogger(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithDefaultDataStoreName_SetsDataStoreName()
    {
        // Arrange
        var expectedName = "TestDapperDataStore";
        _defaultOptions.DefaultDataStoreName = expectedName;

        // Act
        var repository = CreateRepository();

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void Repository_ImplementsISqlMapperRepository()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<ISqlMapperRepository<TestDapperEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIWriteOnlyRepository()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<IWriteOnlyRepository<TestDapperEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIReadOnlyRepository()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<IReadOnlyRepository<TestDapperEntity>>();
    }

    [Fact]
    public void DataStoreName_CanBeSetAndGet()
    {
        // Arrange
        var repository = CreateRepository();
        var expectedName = "CustomDataStore";

        // Act
        repository.DataStoreName = expectedName;

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void TableName_CanBeSetAndGet()
    {
        // Arrange
        var repository = CreateRepository();
        var expectedTableName = "TestEntities";

        // Act
        repository.TableName = expectedTableName;

        // Assert
        repository.TableName.Should().Be(expectedTableName);
    }

    [Fact]
    public void EventTracker_ReturnsInjectedEventTracker()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var eventTracker = repository.EventTracker;

        // Assert
        eventTracker.Should().Be(_mockEventTracker.Object);
    }

    [Fact]
    public void Logger_CanBeSetAndGet()
    {
        // Arrange
        var repository = CreateRepository();
        var newLogger = new Mock<ILogger>();

        // Act
        repository.Logger = newLogger.Object;

        // Assert
        repository.Logger.Should().Be(newLogger.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Users")]
    [InlineData("dbo.Products")]
    [InlineData("[schema].[table]")]
    public void TableName_CanBeSetToVariousValues(string tableName)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.TableName = tableName;

        // Assert
        repository.TableName.Should().Be(tableName);
    }

    [Theory]
    [InlineData("PrimaryDataStore")]
    [InlineData("SecondaryDataStore")]
    [InlineData("Archive.Database")]
    public void DataStoreName_CanBeSetToVariousValues(string dataStoreName)
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.DataStoreName = dataStoreName;

        // Assert
        repository.DataStoreName.Should().Be(dataStoreName);
    }

    [Fact]
    public void Dispose_CanBeCalledSafely()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var action = () => repository.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var action = () =>
        {
            repository.Dispose();
            repository.Dispose();
        };

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public async Task AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var action = () => repository.AddRangeAsync(null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    [Fact]
    public void Repository_InheritsFromSqlRepositoryBase()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<SqlRepositoryBase<TestDapperEntity>>();
    }

    [Fact]
    public void Repository_HasCorrectTypeName()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var typeName = repository.GetType().Name;

        // Assert
        typeName.Should().Contain("DapperRepository");
    }

    [Fact]
    public void Repository_IsGeneric()
    {
        // Arrange & Act
        var repositoryType = typeof(DapperRepository<>);

        // Assert
        repositoryType.IsGenericTypeDefinition.Should().BeTrue();
    }

    [Fact]
    public void Repository_GenericConstraint_RequiresIBusinessEntity()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<>);
        var genericConstraints = repositoryType.GetGenericArguments()[0].GetGenericParameterConstraints();

        // Act & Assert
        genericConstraints.Should().Contain(t => t == typeof(IBusinessEntity));
    }

    [Fact]
    public void Repository_GenericConstraint_RequiresClass()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<>);
        var genericParameter = repositoryType.GetGenericArguments()[0];

        // Act
        var hasClassConstraint = (genericParameter.GenericParameterAttributes &
            System.Reflection.GenericParameterAttributes.ReferenceTypeConstraint) != 0;

        // Assert
        hasClassConstraint.Should().BeTrue();
    }

    [Fact]
    public void Repository_IsPublicClass()
    {
        // Arrange & Act
        var repositoryType = typeof(DapperRepository<>);

        // Assert
        repositoryType.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Repository_IsNotAbstract()
    {
        // Arrange & Act
        var repositoryType = typeof(DapperRepository<>);

        // Assert
        repositoryType.IsAbstract.Should().BeFalse();
    }

    [Fact]
    public void Repository_IsNotSealed()
    {
        // Arrange & Act
        var repositoryType = typeof(DapperRepository<>);

        // Assert
        repositoryType.IsSealed.Should().BeFalse();
    }

    [Fact]
    public void Repository_HasPublicConstructor()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var constructors = repositoryType.GetConstructors();

        // Assert
        constructors.Should().HaveCountGreaterThan(0);
        constructors.Should().Contain(c => c.IsPublic);
    }

    [Fact]
    public void Repository_ConstructorHasCorrectParameters()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);
        var constructor = repositoryType.GetConstructors().First();

        // Act
        var parameters = constructor.GetParameters();

        // Assert
        parameters.Should().HaveCount(4);
        parameters[0].ParameterType.Should().Be(typeof(IDataStoreFactory));
        parameters[1].ParameterType.Should().Be(typeof(ILoggerFactory));
        parameters[2].ParameterType.Should().Be(typeof(IEntityEventTracker));
        parameters[3].ParameterType.Should().Be(typeof(IOptions<DefaultDataStoreOptions>));
    }

    [Fact]
    public void Repository_HasAddAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var method = repositoryType.GetMethod("AddAsync");

        // Assert
        method.Should().NotBeNull();
        method!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Repository_HasUpdateAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var method = repositoryType.GetMethod("UpdateAsync");

        // Assert
        method.Should().NotBeNull();
        method!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Repository_HasDeleteAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var methods = repositoryType.GetMethods().Where(m => m.Name == "DeleteAsync").ToArray();

        // Assert
        methods.Should().NotBeEmpty();
        methods.Should().OnlyContain(m => m.IsPublic);
    }

    [Fact]
    public void Repository_HasFindAsyncMethods()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var methods = repositoryType.GetMethods().Where(m => m.Name == "FindAsync").ToList();

        // Assert
        methods.Should().HaveCountGreaterThan(0);
        methods.Should().OnlyContain(m => m.IsPublic);
    }

    [Fact]
    public void Repository_HasGetCountAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var methods = repositoryType.GetMethods().Where(m => m.Name == "GetCountAsync").ToList();

        // Assert
        methods.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Repository_HasAnyAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var methods = repositoryType.GetMethods().Where(m => m.Name == "AnyAsync").ToList();

        // Assert
        methods.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Repository_HasAddRangeAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var method = repositoryType.GetMethod("AddRangeAsync");

        // Assert
        method.Should().NotBeNull();
        method!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Repository_HasDeleteManyAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var methods = repositoryType.GetMethods().Where(m => m.Name == "DeleteManyAsync").ToList();

        // Assert
        methods.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Repository_HasFindSingleOrDefaultAsyncMethod()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var methods = repositoryType.GetMethods().Where(m => m.Name == "FindSingleOrDefaultAsync").ToList();

        // Assert
        methods.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public void Repository_InCorrectNamespace()
    {
        // Arrange
        var repositoryType = typeof(DapperRepository<TestDapperEntity>);

        // Act
        var ns = repositoryType.Namespace;

        // Assert
        ns.Should().Be("RCommon.Persistence.Dapper.Crud");
    }

    private DapperRepository<TestDapperEntity> CreateRepository()
    {
        return new DapperRepository<TestDapperEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);
    }
}

/// <summary>
/// Test entity for Dapper repository tests.
/// </summary>
public class TestDapperEntity : BusinessEntity<Guid>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }

    public TestDapperEntity() : base()
    {
        Id = Guid.NewGuid();
        CreatedDate = DateTime.UtcNow;
        IsActive = true;
    }

    public TestDapperEntity(Guid id) : base(id)
    {
        CreatedDate = DateTime.UtcNow;
        IsActive = true;
    }

    public TestDapperEntity(Guid id, string name) : base(id)
    {
        Name = name;
        CreatedDate = DateTime.UtcNow;
        IsActive = true;
    }
}
