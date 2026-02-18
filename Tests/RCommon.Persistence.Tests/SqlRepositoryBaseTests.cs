using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Collections;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Security.Claims;
using RCommon.Persistence.Sql;
using System.Data.Common;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class SqlRepositoryBaseTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public SqlRepositoryBaseTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        _defaultOptions = new DefaultDataStoreOptions();

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullDataStoreFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestSqlRepository(
            null!,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("dataStoreFactory");
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestSqlRepository(
            _mockDataStoreFactory.Object,
            null!,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullEventTracker_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            null!,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventTracker");
    }

    [Fact]
    public void Constructor_WithNullDefaultDataStoreOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            null!,
            _mockTenantIdAccessor.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("defaultDataStoreOptions");
    }

    [Fact]
    public void Constructor_WithDefaultDataStoreName_SetsDataStoreName()
    {
        // Arrange
        var expectedName = "SqlTestDataStore";
        _defaultOptions.DefaultDataStoreName = expectedName;

        // Act
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void DataStoreName_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        var expectedName = "CustomSqlDataStore";

        // Act
        repository.DataStoreName = expectedName;

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void TableName_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

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
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Act
        var eventTracker = repository.EventTracker;

        // Assert
        eventTracker.Should().Be(_mockEventTracker.Object);
    }

    [Fact]
    public void Logger_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        var mockLogger = new Mock<ILogger>();

        // Act
        repository.Logger = mockLogger.Object;

        // Assert
        repository.Logger.Should().Be(mockLogger.Object);
    }

    [Fact]
    public void Repository_ImplementsISqlMapperRepository()
    {
        // Arrange & Act
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Assert
        repository.Should().BeAssignableTo<ISqlMapperRepository<TestSqlEntity>>();
    }

    [Fact]
    public void Dispose_CanBeCalledSafely()
    {
        // Arrange
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Act
        var action = () => repository.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Users")]
    [InlineData("dbo.Products")]
    [InlineData("[schema].[table]")]
    public void TableName_CanBeSetToVariousValues(string tableName)
    {
        // Arrange
        var repository = new TestSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);

        // Act
        repository.TableName = tableName;

        // Assert
        repository.TableName.Should().Be(tableName);
    }
}

// Test entity for SQL repository tests
public class TestSqlEntity : BusinessEntity<Guid>
{
    public string? Name { get; set; }

    public TestSqlEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public TestSqlEntity(Guid id) : base(id)
    {
    }
}

// Concrete test implementation of SqlRepositoryBase
public class TestSqlRepository : SqlRepositoryBase<TestSqlEntity>
{
    private readonly List<TestSqlEntity> _entities = new();

    public TestSqlRepository(
        IDataStoreFactory dataStoreFactory,
        ILoggerFactory logger,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, logger, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    public override Task AddAsync(TestSqlEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<TestSqlEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(TestSqlEntity entity, CancellationToken token = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(TestSqlEntity entity, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(Expression<Func<TestSqlEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(0);

    public override Task<int> DeleteManyAsync(Expression<Func<TestSqlEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(ISpecification<TestSqlEntity> specification, CancellationToken token = default)
        => Task.FromResult(0);

    public override Task<int> DeleteManyAsync(ISpecification<TestSqlEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task UpdateAsync(TestSqlEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    public override Task<ICollection<TestSqlEntity>> FindAsync(ISpecification<TestSqlEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<TestSqlEntity>>(_entities.Where(specification.Predicate.Compile()).ToList());

    public override Task<ICollection<TestSqlEntity>> FindAsync(Expression<Func<TestSqlEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<TestSqlEntity>>(_entities.Where(expression.Compile()).ToList());

    public override Task<TestSqlEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<TestSqlEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count);

    public override Task<long> GetCountAsync(Expression<Func<TestSqlEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(expression.Compile()));

    public override Task<TestSqlEntity> FindSingleOrDefaultAsync(Expression<Func<TestSqlEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(expression.Compile())!);

    public override Task<TestSqlEntity> FindSingleOrDefaultAsync(ISpecification<TestSqlEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(specification.Predicate.Compile())!);

    public override Task<bool> AnyAsync(Expression<Func<TestSqlEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.Any(expression.Compile()));

    public override Task<bool> AnyAsync(ISpecification<TestSqlEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.Any(specification.Predicate.Compile()));
}
