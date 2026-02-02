using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Crud;
using Xunit;

namespace RCommon.EfCore.Tests;

public class EFCoreRepositoryTests : IDisposable
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly DefaultDataStoreOptions _defaultOptions;
    private readonly TestDbContext _dbContext;
    private readonly string _dataStoreName = "TestDataStore";

    public EFCoreRepositoryTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _defaultOptions = new DefaultDataStoreOptions { DefaultDataStoreName = _dataStoreName };

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TestDbContext(options);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDbContext>(_dataStoreName))
            .Returns(_dbContext);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    private EFCoreRepository<TestEntity> CreateRepository()
    {
        return new EFCoreRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);
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
        // Arrange, Act & Assert
        var action = () => new EFCoreRepository<TestEntity>(
            null!,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("dataStoreFactory");
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new EFCoreRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            null!,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullEventTracker_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new EFCoreRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            null!,
            _mockDefaultDataStoreOptions.Object);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventTracker");
    }

    [Fact]
    public void Constructor_WithNullDefaultDataStoreOptions_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new EFCoreRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("defaultDataStoreOptions");
    }

    [Fact]
    public void Tracking_DefaultsToTrue()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var tracking = repository.Tracking;

        // Assert
        tracking.Should().BeTrue();
    }

    [Fact]
    public void Tracking_CanBeSetToFalse()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        repository.Tracking = false;

        // Assert
        repository.Tracking.Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_AddsEntityToDatabase()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "Test Entity" };

        // Act
        await repository.AddAsync(entity);

        // Assert
        _dbContext.TestEntities.Should().ContainSingle();
        _mockEventTracker.Verify(x => x.AddEntity(entity), Times.Once);
    }

    [Fact]
    public async Task AddAsync_CallsEventTrackerAddEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "Test Entity" };

        // Act
        await repository.AddAsync(entity);

        // Assert
        _mockEventTracker.Verify(x => x.AddEntity(entity), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_WithValidEntities_AddsAllEntitiesToDatabase()
    {
        // Arrange
        var repository = CreateRepository();
        var entities = new[]
        {
            new TestEntity { Name = "Test 1" },
            new TestEntity { Name = "Test 2" },
            new TestEntity { Name = "Test 3" }
        };

        // Act
        await repository.AddRangeAsync(entities);

        // Assert
        _dbContext.TestEntities.Should().HaveCount(3);
    }

    [Fact]
    public async Task AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var action = async () => await repository.AddRangeAsync(null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task AddRangeAsync_CallsEventTrackerForEachEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entities = new[]
        {
            new TestEntity { Name = "Test 1" },
            new TestEntity { Name = "Test 2" }
        };

        // Act
        await repository.AddRangeAsync(entities);

        // Assert
        _mockEventTracker.Verify(x => x.AddEntity(It.IsAny<TestEntity>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_RemovesEntityFromDatabase()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "Test Entity" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(entity);

        // Assert
        _dbContext.TestEntities.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_CallsEventTrackerAddEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "Test Entity" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        await repository.DeleteAsync(entity);

        // Assert
        _mockEventTracker.Verify(x => x.AddEntity(entity), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_UpdatesEntityInDatabase()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "Original Name" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        entity.Name = "Updated Name";
        await repository.UpdateAsync(entity);

        // Assert
        var updatedEntity = await _dbContext.TestEntities.FirstOrDefaultAsync(e => e.Id == entity.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_CallsEventTrackerAddEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "Test Entity" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        entity.Name = "Updated Name";
        await repository.UpdateAsync(entity);

        // Assert
        _mockEventTracker.Verify(x => x.AddEntity(entity), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithPrimaryKey_ReturnsEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "Test Entity" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.FindAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(entity.Id);
    }

    [Fact]
    public async Task FindAsync_WithNonExistentPrimaryKey_ReturnsNull()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.FindAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_WithExpression_ReturnsMatchingEntities()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.AddRange(
            new TestEntity { Name = "Test 1" },
            new TestEntity { Name = "Test 2" },
            new TestEntity { Name = "Other" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.FindAsync(e => e.Name!.StartsWith("Test"));

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAsync_WithNoMatchingExpression_ReturnsEmptyCollection()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.Add(new TestEntity { Name = "Test" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.FindAsync(e => e.Name == "NonExistent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithMatchingExpression_ReturnsSingleEntity()
    {
        // Arrange
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "UniqueTest" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.FindSingleOrDefaultAsync(e => e.Name == "UniqueTest");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("UniqueTest");
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = await repository.FindSingleOrDefaultAsync(e => e.Name == "NonExistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCountAsync_WithExpression_ReturnsCorrectCount()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.AddRange(
            new TestEntity { Name = "Test 1", IsActive = true },
            new TestEntity { Name = "Test 2", IsActive = true },
            new TestEntity { Name = "Test 3", IsActive = false }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetCountAsync(e => e.IsActive);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetCountAsync_WithNoMatches_ReturnsZero()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.Add(new TestEntity { Name = "Test", IsActive = true });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetCountAsync(e => !e.IsActive);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task AnyAsync_WithMatchingEntities_ReturnsTrue()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.Add(new TestEntity { Name = "Test" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.AnyAsync(e => e.Name == "Test");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithNoMatchingEntities_ReturnsFalse()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.Add(new TestEntity { Name = "Test" });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await repository.AnyAsync(e => e.Name == "NonExistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void FindQuery_WithExpression_ReturnsIQueryable()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.AddRange(
            new TestEntity { Name = "Test 1" },
            new TestEntity { Name = "Test 2" }
        );
        _dbContext.SaveChanges();

        // Act
        var result = repository.FindQuery(e => e.Name!.StartsWith("Test"));

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
        result.Count().Should().Be(2);
    }

    [Fact]
    public void FindQuery_WithExpressionAndOrdering_ReturnsOrderedQueryable()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.AddRange(
            new TestEntity { Name = "B" },
            new TestEntity { Name = "A" },
            new TestEntity { Name = "C" }
        );
        _dbContext.SaveChanges();

        // Act
        var result = repository.FindQuery(e => true, e => e.Name!, true);

        // Assert
        result.Should().NotBeNull();
        result.First().Name.Should().Be("A");
    }

    [Fact]
    public void FindQuery_WithDescendingOrder_ReturnsDescendingOrderedQueryable()
    {
        // Arrange
        var repository = CreateRepository();
        _dbContext.TestEntities.AddRange(
            new TestEntity { Name = "B" },
            new TestEntity { Name = "A" },
            new TestEntity { Name = "C" }
        );
        _dbContext.SaveChanges();

        // Act
        var result = repository.FindQuery(e => true, e => e.Name!, false);

        // Assert
        result.Should().NotBeNull();
        result.First().Name.Should().Be("C");
    }

    [Fact]
    public void FindQuery_WithPaging_ReturnsPagedResults()
    {
        // Arrange
        var repository = CreateRepository();
        for (int i = 0; i < 10; i++)
        {
            _dbContext.TestEntities.Add(new TestEntity { Name = $"Test {i}" });
        }
        _dbContext.SaveChanges();

        // Act
        var result = repository.FindQuery(e => true, e => e.Name!, true, 2, 3);

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public void Include_ReturnsIEagerLoadableQueryable()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = repository.Include(e => e.Name!);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEagerLoadableQueryable<TestEntity>>();
    }

    [Fact]
    public void Include_ReturnsSameRepositoryForChaining()
    {
        // Arrange
        var repository = CreateRepository();

        // Act
        var result = repository.Include(e => e.Name!);

        // Assert
        result.Should().BeSameAs(repository);
    }

    [Fact]
    public void DataStoreName_CanBeSetAndRetrieved()
    {
        // Arrange
        var repository = CreateRepository();
        var newDataStoreName = "NewDataStore";

        // Act
        repository.DataStoreName = newDataStoreName;

        // Assert
        repository.DataStoreName.Should().Be(newDataStoreName);
    }

    [Fact]
    public void Repository_ImplementsILinqRepository()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<ILinqRepository<TestEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIGraphRepository()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<IGraphRepository<TestEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIReadOnlyRepository()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<IReadOnlyRepository<TestEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIWriteOnlyRepository()
    {
        // Arrange & Act
        var repository = CreateRepository();

        // Assert
        repository.Should().BeAssignableTo<IWriteOnlyRepository<TestEntity>>();
    }

    [Fact(Skip = "InMemory provider does not support ExecuteDeleteAsync - requires a relational database")]
    public async Task DeleteManyAsync_WithExpression_DeletesMatchingEntities()
    {
        // Note: ExecuteDeleteAsync is not supported by InMemory provider
        // This test requires a relational database (SQL Server, PostgreSQL, etc.)
        await Task.CompletedTask;
    }

    [Fact(Skip = "InMemory provider does not support ExecuteDeleteAsync - requires a relational database")]
    public async Task DeleteManyAsync_WithNoMatches_ReturnsZero()
    {
        // Note: ExecuteDeleteAsync is not supported by InMemory provider
        // This test requires a relational database (SQL Server, PostgreSQL, etc.)
        await Task.CompletedTask;
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
}
