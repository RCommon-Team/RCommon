using FluentAssertions;
using Moq;
using RCommon.Caching;
using RCommon.Entities;
using RCommon.Persistence.Caching.Crud;
using RCommon.Persistence.Crud;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Caching.Tests;

/// <summary>
/// Verifies that caching decorator repositories correctly delegate soft-delete
/// calls to the inner repository without interfering with cache behavior.
/// </summary>
public class CachingSoftDeleteTests
{
    private readonly Mock<IGraphRepository<TestCachingEntity>> _mockGraphRepository;
    private readonly Mock<ISqlMapperRepository<TestCachingEntity>> _mockSqlRepository;
    private readonly Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>> _mockCacheFactory;
    private readonly Mock<ICacheService> _mockCacheService;

    public CachingSoftDeleteTests()
    {
        _mockGraphRepository = new Mock<IGraphRepository<TestCachingEntity>>();
        _mockSqlRepository = new Mock<ISqlMapperRepository<TestCachingEntity>>();
        _mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        _mockCacheService = new Mock<ICacheService>();

        _mockCacheFactory.Setup(f => f.Create(PersistenceCachingStrategy.Default)).Returns(_mockCacheService.Object);
    }

    // --- CachingGraphRepository soft delete delegation ---

    [Fact]
    public async Task CachingGraphRepository_DeleteAsync_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingGraphRepository<TestCachingEntity>(_mockGraphRepository.Object, _mockCacheFactory.Object);
        var entity = new TestCachingEntity();

        // Act
        await cachingRepo.DeleteAsync(entity, isSoftDelete: true);

        // Assert
        _mockGraphRepository.Verify(r => r.DeleteAsync(entity, true, default), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_DeleteAsync_WithHardDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingGraphRepository<TestCachingEntity>(_mockGraphRepository.Object, _mockCacheFactory.Object);
        var entity = new TestCachingEntity();

        // Act
        await cachingRepo.DeleteAsync(entity, isSoftDelete: false);

        // Assert
        _mockGraphRepository.Verify(r => r.DeleteAsync(entity, false, default), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_DeleteManyAsync_Spec_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingGraphRepository<TestCachingEntity>(_mockGraphRepository.Object, _mockCacheFactory.Object);
        var mockSpec = new Mock<ISpecification<TestCachingEntity>>();

        // Act
        await cachingRepo.DeleteManyAsync(mockSpec.Object, isSoftDelete: true);

        // Assert
        _mockGraphRepository.Verify(r => r.DeleteManyAsync(mockSpec.Object, true, default), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_DeleteManyAsync_Expression_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingGraphRepository<TestCachingEntity>(_mockGraphRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestCachingEntity, bool>> expression = e => e.Name == "Test";

        // Act
        await cachingRepo.DeleteManyAsync(expression, isSoftDelete: true);

        // Assert
        _mockGraphRepository.Verify(r => r.DeleteManyAsync(It.IsAny<Expression<Func<TestCachingEntity, bool>>>(), true, default), Times.Once);
    }

    // --- CachingLinqRepository soft delete delegation ---

    [Fact]
    public async Task CachingLinqRepository_DeleteAsync_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingLinqRepository<TestCachingEntity>(_mockGraphRepository.Object, _mockCacheFactory.Object);
        var entity = new TestCachingEntity();

        // Act
        await cachingRepo.DeleteAsync(entity, isSoftDelete: true);

        // Assert
        _mockGraphRepository.Verify(r => r.DeleteAsync(entity, true, default), Times.Once);
    }

    [Fact]
    public async Task CachingLinqRepository_DeleteManyAsync_Spec_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingLinqRepository<TestCachingEntity>(_mockGraphRepository.Object, _mockCacheFactory.Object);
        var mockSpec = new Mock<ISpecification<TestCachingEntity>>();

        // Act
        await cachingRepo.DeleteManyAsync(mockSpec.Object, isSoftDelete: true);

        // Assert
        _mockGraphRepository.Verify(r => r.DeleteManyAsync(mockSpec.Object, true, default), Times.Once);
    }

    [Fact]
    public async Task CachingLinqRepository_DeleteManyAsync_Expression_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingLinqRepository<TestCachingEntity>(_mockGraphRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestCachingEntity, bool>> expression = e => e.Name == "Test";

        // Act
        await cachingRepo.DeleteManyAsync(expression, isSoftDelete: true);

        // Assert
        _mockGraphRepository.Verify(r => r.DeleteManyAsync(It.IsAny<Expression<Func<TestCachingEntity, bool>>>(), true, default), Times.Once);
    }

    // --- CachingSqlMapperRepository soft delete delegation ---

    [Fact]
    public async Task CachingSqlMapperRepository_DeleteAsync_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingSqlMapperRepository<TestCachingEntity>(_mockSqlRepository.Object, _mockCacheFactory.Object);
        var entity = new TestCachingEntity();

        // Act
        await cachingRepo.DeleteAsync(entity, isSoftDelete: true);

        // Assert
        _mockSqlRepository.Verify(r => r.DeleteAsync(entity, true, default), Times.Once);
    }

    [Fact]
    public async Task CachingSqlMapperRepository_DeleteManyAsync_Spec_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingSqlMapperRepository<TestCachingEntity>(_mockSqlRepository.Object, _mockCacheFactory.Object);
        var mockSpec = new Mock<ISpecification<TestCachingEntity>>();

        // Act
        await cachingRepo.DeleteManyAsync(mockSpec.Object, isSoftDelete: true);

        // Assert
        _mockSqlRepository.Verify(r => r.DeleteManyAsync(mockSpec.Object, true, default), Times.Once);
    }

    [Fact]
    public async Task CachingSqlMapperRepository_DeleteManyAsync_Expression_WithSoftDelete_DelegatesToInnerRepository()
    {
        // Arrange
        var cachingRepo = new CachingSqlMapperRepository<TestCachingEntity>(_mockSqlRepository.Object, _mockCacheFactory.Object);
        Expression<Func<TestCachingEntity, bool>> expression = e => e.Name == "Test";

        // Act
        await cachingRepo.DeleteManyAsync(expression, isSoftDelete: true);

        // Assert
        _mockSqlRepository.Verify(r => r.DeleteManyAsync(It.IsAny<Expression<Func<TestCachingEntity, bool>>>(), true, default), Times.Once);
    }
}

/// <summary>
/// Test entity for caching soft-delete tests. Implements ISoftDelete so both
/// soft and hard delete paths can be tested.
/// </summary>
public class TestCachingEntity : BusinessEntity<Guid>, ISoftDelete
{
    public string? Name { get; set; }
    public bool IsDeleted { get; set; }

    public TestCachingEntity() : base()
    {
        Id = Guid.NewGuid();
    }
}
