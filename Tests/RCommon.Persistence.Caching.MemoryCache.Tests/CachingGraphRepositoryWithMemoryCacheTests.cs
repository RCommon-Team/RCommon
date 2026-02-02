using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using RCommon.Caching;
using RCommon.Collections;
using RCommon.MemoryCache;
using RCommon.Persistence.Caching.Crud;
using RCommon.Persistence.Caching.MemoryCache.Tests.TestHelpers;
using RCommon.Persistence.Crud;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Caching.MemoryCache.Tests;

public class CachingGraphRepositoryWithMemoryCacheTests
{
    private readonly Mock<IGraphRepository<TestEntity>> _mockRepository;
    private readonly Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>> _mockCacheFactory;
    private readonly InMemoryCacheService _cacheService;
    private readonly IMemoryCache _memoryCache;

    public CachingGraphRepositoryWithMemoryCacheTests()
    {
        _mockRepository = new Mock<IGraphRepository<TestEntity>>();
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
        var act = () => new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_CreatesInstanceOfICachingGraphRepository()
    {
        // Arrange & Act
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Assert
        repository.Should().BeAssignableTo<ICachingGraphRepository<TestEntity>>();
    }

    #endregion

    #region Cached FindAsync with Expression Tests

    [Fact]
    public async Task FindAsync_WithCacheKey_WhenCacheMiss_CallsRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "test-key";
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "Test" } };
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
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "cached-entities-key";
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "Test" } };
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
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey1 = "key1";
        var cacheKey2 = "key2";
        var entities1 = new List<TestEntity> { new TestEntity { Name = "Entity1" } };
        var entities2 = new List<TestEntity> { new TestEntity { Name = "Entity2" } };
        Expression<Func<TestEntity, bool>> expression1 = x => x.Name == "Entity1";
        Expression<Func<TestEntity, bool>> expression2 = x => x.Name == "Entity2";

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

    #endregion

    #region Cached FindAsync with Specification Tests

    [Fact]
    public async Task FindAsync_WithCacheKeyAndSpecification_WhenCacheMiss_CallsRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "spec-cache-key";
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "SpecTest" } };

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
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "spec-cached-key";
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "CachedSpecTest" } };

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

    #endregion

    #region Cached FindAsync with Pagination Tests

    [Fact]
    public async Task FindAsync_WithCacheKeyAndPagination_WhenCacheMiss_CallsRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "paged-cache-key";
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;
        Expression<Func<TestEntity, object>> orderBy = x => x.Name;
        var expectedList = CreateMockPaginatedList(new List<TestEntity> { new TestEntity { Name = "PagedTest" } });

        _mockRepository
            .Setup(x => x.FindAsync(expression, orderBy, true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // Act
        var result = await repository.FindAsync(cacheKey, expression, orderBy, true, 1, 10);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.FindAsync(expression, orderBy, true, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithCacheKeyAndPagination_WhenCacheHit_ReturnsCachedData()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "paged-cached-key";
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;
        Expression<Func<TestEntity, object>> orderBy = x => x.Name;
        var expectedList = CreateMockPaginatedList(new List<TestEntity> { new TestEntity { Name = "CachedPagedTest" } });

        _mockRepository
            .Setup(x => x.FindAsync(expression, orderBy, true, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // First call to populate cache
        await repository.FindAsync(cacheKey, expression, orderBy, true, 1, 10);
        _mockRepository.Invocations.Clear();

        // Act
        var result = await repository.FindAsync(cacheKey, expression, orderBy, true, 1, 10);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.FindAsync(expression, orderBy, true, 1, 10, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task FindAsync_WithCacheKeyAndPagedSpecification_WhenCacheMiss_CallsRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var cacheKey = "paged-spec-cache-key";
        var mockSpec = new Mock<IPagedSpecification<TestEntity>>();
        var expectedList = CreateMockPaginatedList(new List<TestEntity> { new TestEntity { Name = "PagedSpecTest" } });

        _mockRepository
            .Setup(x => x.FindAsync(mockSpec.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // Act
        var result = await repository.FindAsync(cacheKey, mockSpec.Object);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.FindAsync(mockSpec.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Non-Cached Repository Operations Tests

    [Fact]
    public async Task AddAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entity = new TestEntity { Name = "NewEntity" };

        // Act
        await repository.AddAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entity = new TestEntity { Name = "UpdatedEntity" };

        // Act
        await repository.UpdateAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entity = new TestEntity { Name = "DeletedEntity" };

        // Act
        await repository.DeleteAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_ByPrimaryKey_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var primaryKey = Guid.NewGuid();
        var expectedEntity = new TestEntity { Id = primaryKey, Name = "FoundEntity" };

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
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
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
    public async Task GetCountAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        _mockRepository
            .Setup(x => x.GetCountAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await repository.GetCountAsync(expression);

        // Assert
        result.Should().Be(42);
        _mockRepository.Verify(x => x.GetCountAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteManyAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestEntity, bool>> expression = x => !x.IsActive;

        _mockRepository
            .Setup(x => x.DeleteManyAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await repository.DeleteManyAsync(expression);

        // Assert
        result.Should().Be(5);
        _mockRepository.Verify(x => x.DeleteManyAsync(expression, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        var entities = new List<TestEntity>
        {
            new TestEntity { Name = "Entity1" },
            new TestEntity { Name = "Entity2" }
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
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Act
        var act = async () => await repository.AddRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    #endregion

    #region Property Delegation Tests

    [Fact]
    public void Tracking_Get_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        _mockRepository.Setup(x => x.Tracking).Returns(true);

        // Act
        var result = repository.Tracking;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Tracking_Set_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Act
        repository.Tracking = true;

        // Assert
        _mockRepository.VerifySet(x => x.Tracking = true);
    }

    [Fact]
    public void DataStoreName_Get_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
        _mockRepository.Setup(x => x.DataStoreName).Returns("TestDataStore");

        // Act
        var result = repository.DataStoreName;

        // Assert
        result.Should().Be("TestDataStore");
    }

    [Fact]
    public void DataStoreName_Set_DelegatesToRepository()
    {
        // Arrange
        var repository = new CachingGraphRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);

        // Act
        repository.DataStoreName = "NewDataStore";

        // Assert
        _mockRepository.VerifySet(x => x.DataStoreName = "NewDataStore");
    }

    #endregion

    #region Helper Methods

    private static IPaginatedList<TestEntity> CreateMockPaginatedList(List<TestEntity> entities)
    {
        var mockList = new Mock<IPaginatedList<TestEntity>>();
        mockList.Setup(x => x.GetEnumerator()).Returns(entities.GetEnumerator());
        mockList.Setup(x => x.Count).Returns(entities.Count);
        mockList.Setup(x => x.TotalCount).Returns(entities.Count);
        mockList.Setup(x => x.PageIndex).Returns(1);
        mockList.Setup(x => x.PageSize).Returns(10);
        return mockList.Object;
    }

    #endregion
}
