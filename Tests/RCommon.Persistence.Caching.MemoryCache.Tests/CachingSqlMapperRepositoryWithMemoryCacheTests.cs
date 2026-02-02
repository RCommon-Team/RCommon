using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RCommon.Caching;
using RCommon.MemoryCache;
using RCommon.Persistence.Caching.Crud;
using RCommon.Persistence.Caching.MemoryCache.Tests.TestHelpers;
using RCommon.Persistence.Crud;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Caching.MemoryCache.Tests;

public class CachingSqlMapperRepositoryWithMemoryCacheTests
{
    private readonly Mock<ISqlMapperRepository<TestEntity>> _mockRepository;
    private readonly Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>> _mockCacheFactory;
    private readonly InMemoryCacheService _cacheService;
    private readonly IMemoryCache _memoryCache;

    public CachingSqlMapperRepositoryWithMemoryCacheTests()
    {
        _mockRepository = new Mock<ISqlMapperRepository<TestEntity>>();
        _mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        _memoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new MemoryCacheOptions());
        _cacheService = new InMemoryCacheService(_memoryCache);

        _mockCacheFactory
            .Setup(x => x.Create(PersistenceCachingStrategy.Default))
            .Returns(_cacheService);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_CreatesInstanceOfICachingSqlMapperRepository()
    {
        // Arrange & Act
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Assert
        repository.Should().BeAssignableTo<ICachingSqlMapperRepository<TestEntity>>();
    }

    #endregion

    #region Cached FindAsync with Expression Tests

    [Fact]
    public async Task FindAsync_WithCacheKey_WhenCacheMiss_CallsRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "sql-test-key";
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "SqlTest" } };
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        _mockRepository
            .Setup(x => x.FindAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await repository.FindAsync(cacheKey, expression);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithCacheKey_WhenCacheHit_DoesNotCallRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "sql-cached-entities-key";
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "CachedSqlTest" } };
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        _mockRepository
            .Setup(x => x.FindAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // First call to populate cache
        await repository.FindAsync(cacheKey, expression);
        _mockRepository.Invocations.Clear();

        // Act - Second call should hit cache
        var result = await repository.FindAsync(cacheKey, expression);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(expression, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindAsync_WithDifferentCacheKeys_CallsRepositoryForEach()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey1 = "sql-key1";
        var cacheKey2 = "sql-key2";
        var entities1 = new List<TestEntity> { new TestEntity { Name = "SqlEntity1" } };
        var entities2 = new List<TestEntity> { new TestEntity { Name = "SqlEntity2" } };
        Expression<Func<TestEntity, bool>> expression1 = x => x.Name == "SqlEntity1";
        Expression<Func<TestEntity, bool>> expression2 = x => x.Name == "SqlEntity2";

        _mockRepository
            .Setup(x => x.FindAsync(expression1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities1);
        _mockRepository
            .Setup(x => x.FindAsync(expression2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities2);

        // Act
        var result1 = await repository.FindAsync(cacheKey1, expression1);
        var result2 = await repository.FindAsync(cacheKey2, expression2);

        // Assert
        result1.Should().BeEquivalentTo(entities1);
        result2.Should().BeEquivalentTo(entities2);
    }

    [Fact]
    public async Task FindAsync_WithSameCacheKey_MultipleTimes_OnlyCallsRepositoryOnce()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "sql-single-call-key";
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "SingleCallTest" } };
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        _mockRepository
            .Setup(x => x.FindAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act - Multiple calls with same cache key
        await repository.FindAsync(cacheKey, expression);
        await repository.FindAsync(cacheKey, expression);
        await repository.FindAsync(cacheKey, expression);

        // Assert
        _mockRepository.Verify(x => x.FindAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cached FindAsync with Specification Tests

    [Fact]
    public async Task FindAsync_WithCacheKeyAndSpecification_WhenCacheMiss_CallsRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "sql-spec-cache-key";
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "SqlSpecTest" } };

        _mockRepository
            .Setup(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await repository.FindAsync(cacheKey, mockSpecification.Object);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithCacheKeyAndSpecification_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "sql-spec-cached-key";
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "CachedSqlSpecTest" } };

        _mockRepository
            .Setup(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // First call to populate cache
        await repository.FindAsync(cacheKey, mockSpecification.Object);
        _mockRepository.Invocations.Clear();

        // Act
        var result = await repository.FindAsync(cacheKey, mockSpecification.Object);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindAsync_WithDifferentCacheKeysAndSameSpecification_CallsRepositoryForEach()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey1 = "sql-spec-key1";
        var cacheKey2 = "sql-spec-key2";
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "SpecEntity" } };

        _mockRepository
            .Setup(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result1 = await repository.FindAsync(cacheKey1, mockSpecification.Object);
        var result2 = await repository.FindAsync(cacheKey2, mockSpecification.Object);

        // Assert
        result1.Should().BeEquivalentTo(expectedEntities);
        result2.Should().BeEquivalentTo(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region Non-Cached Repository Operations Tests

    [Fact]
    public async Task AddAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entity = new TestEntity { Name = "SqlNewEntity" };

        // Act
        await repository.AddAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entity = new TestEntity { Name = "SqlUpdatedEntity" };

        // Act
        await repository.UpdateAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entity = new TestEntity { Name = "SqlDeletedEntity" };

        // Act
        await repository.DeleteAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ByPrimaryKey_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var primaryKey = Guid.NewGuid();
        var expectedEntity = new TestEntity { Id = primaryKey, Name = "SqlFoundEntity" };

        _mockRepository
            .Setup(x => x.FindAsync(primaryKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        // Act
        var result = await repository.FindAsync(primaryKey);

        // Assert
        result.Should().BeEquivalentTo(expectedEntity);
        _mockRepository.Verify(x => x.FindAsync(primaryKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnyAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        _mockRepository
            .Setup(x => x.AnyAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await repository.AnyAsync(expression);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.AnyAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnyAsync_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var mockSpec = new Mock<ISpecification<TestEntity>>();

        _mockRepository
            .Setup(x => x.AnyAsync(mockSpec.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await repository.AnyAsync(mockSpec.Object);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.AnyAsync(mockSpec.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCountAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        _mockRepository
            .Setup(x => x.GetCountAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        // Act
        var result = await repository.GetCountAsync(expression);

        // Assert
        result.Should().Be(50);
        _mockRepository.Verify(x => x.GetCountAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCountAsync_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var mockSpec = new Mock<ISpecification<TestEntity>>();

        _mockRepository
            .Setup(x => x.GetCountAsync(mockSpec.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(75);

        // Act
        var result = await repository.GetCountAsync(mockSpec.Object);

        // Assert
        result.Should().Be(75);
        _mockRepository.Verify(x => x.GetCountAsync(mockSpec.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteManyAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestEntity, bool>> expression = x => !x.IsActive;

        _mockRepository
            .Setup(x => x.DeleteManyAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(15);

        // Act
        var result = await repository.DeleteManyAsync(expression);

        // Assert
        result.Should().Be(15);
        _mockRepository.Verify(x => x.DeleteManyAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteManyAsync_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var mockSpec = new Mock<ISpecification<TestEntity>>();

        _mockRepository
            .Setup(x => x.DeleteManyAsync(mockSpec.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        // Act
        var result = await repository.DeleteManyAsync(mockSpec.Object);

        // Assert
        result.Should().Be(20);
        _mockRepository.Verify(x => x.DeleteManyAsync(mockSpec.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var expectedEntity = new TestEntity { Name = "SingleEntity" };
        Expression<Func<TestEntity, bool>> expression = x => x.Name == "SingleEntity";

        _mockRepository
            .Setup(x => x.FindSingleOrDefaultAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        // Act
        var result = await repository.FindSingleOrDefaultAsync(expression);

        // Assert
        result.Should().BeEquivalentTo(expectedEntity);
        _mockRepository.Verify(x => x.FindSingleOrDefaultAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var expectedEntity = new TestEntity { Name = "SpecSingleEntity" };
        var mockSpec = new Mock<ISpecification<TestEntity>>();

        _mockRepository
            .Setup(x => x.FindSingleOrDefaultAsync(mockSpec.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        // Act
        var result = await repository.FindSingleOrDefaultAsync(mockSpec.Object);

        // Assert
        result.Should().BeEquivalentTo(expectedEntity);
        _mockRepository.Verify(x => x.FindSingleOrDefaultAsync(mockSpec.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entities = new List<TestEntity>
        {
            new TestEntity { Name = "SqlEntity1" },
            new TestEntity { Name = "SqlEntity2" }
        };

        // Act
        await repository.AddRangeAsync(entities);

        // Assert
        _mockRepository.Verify(x => x.AddRangeAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Act
        var act = async () => await repository.AddRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    #endregion

    #region Property Delegation Tests

    [Fact]
    public void TableName_Get_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        _mockRepository.Setup(x => x.TableName).Returns("TestEntities");

        // Act
        var result = repository.TableName;

        // Assert
        result.Should().Be("TestEntities");
    }

    [Fact]
    public void TableName_Set_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Act
        repository.TableName = "NewTableName";

        // Assert
        _mockRepository.VerifySet(x => x.TableName = "NewTableName");
    }

    [Fact]
    public void DataStoreName_Get_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        _mockRepository.Setup(x => x.DataStoreName).Returns("SqlTestDataStore");

        // Act
        var result = repository.DataStoreName;

        // Assert
        result.Should().Be("SqlTestDataStore");
    }

    [Fact]
    public void DataStoreName_Set_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Act
        repository.DataStoreName = "SqlNewDataStore";

        // Assert
        _mockRepository.VerifySet(x => x.DataStoreName = "SqlNewDataStore");
    }

    #endregion

    #region Non-Cached FindAsync Tests (Without Cache Key)

    [Fact]
    public async Task FindAsync_WithoutCacheKey_WithExpression_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "NonCachedEntity" } };
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        _mockRepository
            .Setup(x => x.FindAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await repository.FindAsync(expression);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithoutCacheKey_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingSqlMapperRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var mockSpec = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "NonCachedSpecEntity" } };

        _mockRepository
            .Setup(x => x.FindAsync(mockSpec.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await repository.FindAsync(mockSpec.Object);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(mockSpec.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
