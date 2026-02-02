using FluentAssertions;
using Moq;
using RCommon;
using RCommon.Caching;
using RCommon.Collections;
using RCommon.Persistence.Caching;
using RCommon.Persistence.Caching.Crud;
using RCommon.Persistence.Crud;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Caching.Tests.Crud;

public class CachingLinqRepositoryTests
{
    private readonly Mock<IGraphRepository<TestEntity>> _mockRepository;
    private readonly Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>> _mockCacheFactory;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly CachingLinqRepository<TestEntity> _sut;

    public CachingLinqRepositoryTests()
    {
        _mockRepository = new Mock<IGraphRepository<TestEntity>>();
        _mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        _mockCacheService = new Mock<ICacheService>();

        _mockCacheFactory
            .Setup(x => x.Create(PersistenceCachingStrategy.Default))
            .Returns(_mockCacheService.Object);

        _sut = new CachingLinqRepository<TestEntity>(_mockRepository.Object, _mockCacheFactory.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        var mockCacheService = new Mock<ICacheService>();

        mockCacheFactory
            .Setup(x => x.Create(PersistenceCachingStrategy.Default))
            .Returns(mockCacheService.Object);

        // Act
        var repository = new CachingLinqRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_CallsCacheFactoryWithDefaultStrategy()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        var mockCacheService = new Mock<ICacheService>();

        mockCacheFactory
            .Setup(x => x.Create(PersistenceCachingStrategy.Default))
            .Returns(mockCacheService.Object);

        // Act
        var repository = new CachingLinqRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Assert
        mockCacheFactory.Verify(x => x.Create(PersistenceCachingStrategy.Default), Times.Once);
    }

    #endregion

    #region Property Delegation Tests

    [Fact]
    public void ElementType_DelegatesToRepository()
    {
        // Arrange
        var expectedType = typeof(TestEntity);
        _mockRepository.Setup(x => x.ElementType).Returns(expectedType);

        // Act
        var result = _sut.ElementType;

        // Assert
        result.Should().Be(expectedType);
    }

    [Fact]
    public void Expression_DelegatesToRepository()
    {
        // Arrange
        var expectedExpression = Expression.Constant(new List<TestEntity>().AsQueryable());
        _mockRepository.Setup(x => x.Expression).Returns(expectedExpression);

        // Act
        var result = _sut.Expression;

        // Assert
        result.Should().Be(expectedExpression);
    }

    [Fact]
    public void Provider_DelegatesToRepository()
    {
        // Arrange
        var mockProvider = new Mock<IQueryProvider>();
        _mockRepository.Setup(x => x.Provider).Returns(mockProvider.Object);

        // Act
        var result = _sut.Provider;

        // Assert
        result.Should().Be(mockProvider.Object);
    }

    [Fact]
    public void DataStoreName_Get_DelegatesToRepository()
    {
        // Arrange
        var expectedName = "TestDataStore";
        _mockRepository.Setup(x => x.DataStoreName).Returns(expectedName);

        // Act
        var result = _sut.DataStoreName;

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void DataStoreName_Set_DelegatesToRepository()
    {
        // Arrange
        var storeName = "NewDataStore";

        // Act
        _sut.DataStoreName = storeName;

        // Assert
        _mockRepository.VerifySet(x => x.DataStoreName = storeName, Times.Once);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_DelegatesToRepository()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };
        _mockRepository.Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.AddAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_PassesCancellationToken()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };
        using var cts = new CancellationTokenSource();
        CancellationToken capturedToken = default;

        _mockRepository
            .Setup(x => x.AddAsync(entity, It.IsAny<CancellationToken>()))
            .Callback<TestEntity, CancellationToken>((e, ct) => capturedToken = ct)
            .Returns(Task.CompletedTask);

        // Act
        await _sut.AddAsync(entity, cts.Token);

        // Assert
        capturedToken.Should().Be(cts.Token);
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_DelegatesToRepository()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new TestEntity { Id = Guid.NewGuid() },
            new TestEntity { Id = Guid.NewGuid() }
        };
        _mockRepository.Setup(x => x.AddRangeAsync(entities, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.AddRangeAsync(entities);

        // Assert
        _mockRepository.Verify(x => x.AddRangeAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<TestEntity>? entities = null;

        // Act
        Func<Task> act = async () => await _sut.AddRangeAsync(entities!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    #endregion

    #region AnyAsync Tests

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnyAsync_WithExpression_DelegatesToRepository(bool expectedResult)
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Id != Guid.Empty;
        _mockRepository.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.AnyAsync(expression);

        // Assert
        result.Should().Be(expectedResult);
        _mockRepository.Verify(x => x.AnyAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnyAsync_WithSpecification_DelegatesToRepository(bool expectedResult)
    {
        // Arrange
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        _mockRepository.Setup(x => x.AnyAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.AnyAsync(mockSpecification.Object);

        // Assert
        result.Should().Be(expectedResult);
        _mockRepository.Verify(x => x.AnyAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };
        _mockRepository.Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteManyAsync Tests

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task DeleteManyAsync_WithSpecification_DelegatesToRepository(int expectedCount)
    {
        // Arrange
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        _mockRepository.Setup(x => x.DeleteManyAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _sut.DeleteManyAsync(mockSpecification.Object);

        // Assert
        result.Should().Be(expectedCount);
        _mockRepository.Verify(x => x.DeleteManyAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(100)]
    public async Task DeleteManyAsync_WithExpression_DelegatesToRepository(int expectedCount)
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Id != Guid.Empty;
        _mockRepository.Setup(x => x.DeleteManyAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _sut.DeleteManyAsync(expression);

        // Assert
        result.Should().Be(expectedCount);
        _mockRepository.Verify(x => x.DeleteManyAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region FindAsync (Non-Cached) Tests

    [Fact]
    public async Task FindAsync_WithPrimaryKey_DelegatesToRepository()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expectedEntity = new TestEntity { Id = id };
        _mockRepository.Setup(x => x.FindAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        // Act
        var result = await _sut.FindAsync(id);

        // Assert
        result.Should().Be(expectedEntity);
        _mockRepository.Verify(x => x.FindAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Id != Guid.Empty;
        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        _mockRepository.Setup(x => x.FindAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await _sut.FindAsync(expression);

        // Assert
        result.Should().BeSameAs(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        _mockRepository.Setup(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        // Act
        var result = await _sut.FindAsync(mockSpecification.Object);

        // Assert
        result.Should().BeSameAs(expectedEntities);
        _mockRepository.Verify(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithPagination_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => true;
        Expression<Func<TestEntity, object>> orderBy = e => e.Id;
        var mockPaginatedList = new Mock<IPaginatedList<TestEntity>>();
        _mockRepository.Setup(x => x.FindAsync(
                It.IsAny<Expression<Func<TestEntity, bool>>>(),
                It.IsAny<Expression<Func<TestEntity, object>>>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPaginatedList.Object);

        // Act
        var result = await _sut.FindAsync(expression, orderBy, true, 1, 10);

        // Assert
        result.Should().BeSameAs(mockPaginatedList.Object);
        _mockRepository.Verify(x => x.FindAsync(
            It.IsAny<Expression<Func<TestEntity, bool>>>(),
            It.IsAny<Expression<Func<TestEntity, object>>>(),
            true, 1, 10,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithPagedSpecification_DelegatesToRepository()
    {
        // Arrange
        var mockSpecification = new Mock<IPagedSpecification<TestEntity>>();
        var mockPaginatedList = new Mock<IPaginatedList<TestEntity>>();
        _mockRepository.Setup(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockPaginatedList.Object);

        // Act
        var result = await _sut.FindAsync(mockSpecification.Object);

        // Assert
        result.Should().BeSameAs(mockPaginatedList.Object);
        _mockRepository.Verify(x => x.FindAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region FindAsync (Cached) Tests

    [Fact]
    public async Task FindAsync_WithCacheKey_AndExpression_UsesCacheService()
    {
        // Arrange
        var cacheKey = "test-cache-key";
        Expression<Func<TestEntity, bool>> expression = e => true;
        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } } as ICollection<TestEntity>;

        _mockCacheService
            .Setup(x => x.GetOrCreateAsync<Task<ICollection<TestEntity>>>(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()))
            .Returns(Task.FromResult(Task.FromResult(expectedEntities)));

        // Act
        var result = await _sut.FindAsync(cacheKey, expression);

        // Assert
        result.Should().BeSameAs(expectedEntities);
        _mockCacheService.Verify(x => x.GetOrCreateAsync<Task<ICollection<TestEntity>>>(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithCacheKey_AndSpecification_UsesCacheService()
    {
        // Arrange
        var cacheKey = "spec-cache-key";
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } } as ICollection<TestEntity>;

        _mockCacheService
            .Setup(x => x.GetOrCreateAsync<Task<ICollection<TestEntity>>>(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()))
            .Returns(Task.FromResult(Task.FromResult(expectedEntities)));

        // Act
        var result = await _sut.FindAsync(cacheKey, mockSpecification.Object);

        // Assert
        result.Should().BeSameAs(expectedEntities);
        _mockCacheService.Verify(x => x.GetOrCreateAsync<Task<ICollection<TestEntity>>>(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithCacheKey_AndPagination_UsesCacheService()
    {
        // Arrange
        var cacheKey = "paged-cache-key";
        Expression<Func<TestEntity, bool>> expression = e => true;
        Expression<Func<TestEntity, object>> orderBy = e => e.Id;
        var mockPaginatedList = new Mock<IPaginatedList<TestEntity>>();

        _mockCacheService
            .Setup(x => x.GetOrCreateAsync<Task<IPaginatedList<TestEntity>>>(cacheKey, It.IsAny<Func<Task<IPaginatedList<TestEntity>>>>()))
            .Returns(Task.FromResult(Task.FromResult(mockPaginatedList.Object)));

        // Act
        var result = await _sut.FindAsync(cacheKey, expression, orderBy, true, 1, 10);

        // Assert
        result.Should().BeSameAs(mockPaginatedList.Object);
        _mockCacheService.Verify(x => x.GetOrCreateAsync<Task<IPaginatedList<TestEntity>>>(cacheKey, It.IsAny<Func<Task<IPaginatedList<TestEntity>>>>()), Times.Once);
    }

    [Fact]
    public async Task FindAsync_WithCacheKey_AndPagedSpecification_UsesCacheService()
    {
        // Arrange
        var cacheKey = "paged-spec-cache-key";
        var mockSpecification = new Mock<IPagedSpecification<TestEntity>>();
        var mockPaginatedList = new Mock<IPaginatedList<TestEntity>>();

        _mockCacheService
            .Setup(x => x.GetOrCreateAsync<Task<IPaginatedList<TestEntity>>>(cacheKey, It.IsAny<Func<Task<IPaginatedList<TestEntity>>>>()))
            .Returns(Task.FromResult(Task.FromResult(mockPaginatedList.Object)));

        // Act
        var result = await _sut.FindAsync(cacheKey, mockSpecification.Object);

        // Assert
        result.Should().BeSameAs(mockPaginatedList.Object);
        _mockCacheService.Verify(x => x.GetOrCreateAsync<Task<IPaginatedList<TestEntity>>>(cacheKey, It.IsAny<Func<Task<IPaginatedList<TestEntity>>>>()), Times.Once);
    }

    #endregion

    #region FindQuery Tests

    [Fact]
    public void FindQuery_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedQueryable = new List<TestEntity>().AsQueryable();
        _mockRepository.Setup(x => x.FindQuery(mockSpecification.Object)).Returns(expectedQueryable);

        // Act
        var result = _sut.FindQuery(mockSpecification.Object);

        // Assert
        result.Should().BeSameAs(expectedQueryable);
        _mockRepository.Verify(x => x.FindQuery(mockSpecification.Object), Times.Once);
    }

    [Fact]
    public void FindQuery_WithExpression_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => true;
        var expectedQueryable = new List<TestEntity>().AsQueryable();
        _mockRepository.Setup(x => x.FindQuery(It.IsAny<Expression<Func<TestEntity, bool>>>())).Returns(expectedQueryable);

        // Act
        var result = _sut.FindQuery(expression);

        // Assert
        result.Should().BeSameAs(expectedQueryable);
        _mockRepository.Verify(x => x.FindQuery(It.IsAny<Expression<Func<TestEntity, bool>>>()), Times.Once);
    }

    [Fact]
    public void FindQuery_WithExpressionAndOrderBy_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => true;
        Expression<Func<TestEntity, object>> orderBy = e => e.Id;
        var expectedQueryable = new List<TestEntity>().AsQueryable();
        _mockRepository.Setup(x => x.FindQuery(
            It.IsAny<Expression<Func<TestEntity, bool>>>(),
            It.IsAny<Expression<Func<TestEntity, object>>>(),
            It.IsAny<bool>())).Returns(expectedQueryable);

        // Act
        var result = _sut.FindQuery(expression, orderBy, true);

        // Assert
        result.Should().BeSameAs(expectedQueryable);
        _mockRepository.Verify(x => x.FindQuery(
            It.IsAny<Expression<Func<TestEntity, bool>>>(),
            It.IsAny<Expression<Func<TestEntity, object>>>(),
            true), Times.Once);
    }

    [Fact]
    public void FindQuery_WithPagination_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => true;
        Expression<Func<TestEntity, object>> orderBy = e => e.Id;
        var expectedQueryable = new List<TestEntity>().AsQueryable();
        _mockRepository.Setup(x => x.FindQuery(
            It.IsAny<Expression<Func<TestEntity, bool>>>(),
            It.IsAny<Expression<Func<TestEntity, object>>>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>())).Returns(expectedQueryable);

        // Act
        var result = _sut.FindQuery(expression, orderBy, true, 1, 10);

        // Assert
        result.Should().BeSameAs(expectedQueryable);
        _mockRepository.Verify(x => x.FindQuery(
            It.IsAny<Expression<Func<TestEntity, bool>>>(),
            It.IsAny<Expression<Func<TestEntity, object>>>(),
            true, 1, 10), Times.Once);
    }

    [Fact]
    public void FindQuery_WithPagedSpecification_DelegatesToRepository()
    {
        // Arrange
        var mockSpecification = new Mock<IPagedSpecification<TestEntity>>();
        var expectedQueryable = new List<TestEntity>().AsQueryable();
        _mockRepository.Setup(x => x.FindQuery(mockSpecification.Object)).Returns(expectedQueryable);

        // Act
        var result = _sut.FindQuery(mockSpecification.Object);

        // Assert
        result.Should().BeSameAs(expectedQueryable);
        _mockRepository.Verify(x => x.FindQuery(mockSpecification.Object), Times.Once);
    }

    #endregion

    #region FindSingleOrDefaultAsync Tests

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithExpression_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => e.Id != Guid.Empty;
        var expectedEntity = new TestEntity { Id = Guid.NewGuid() };
        _mockRepository.Setup(x => x.FindSingleOrDefaultAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        // Act
        var result = await _sut.FindSingleOrDefaultAsync(expression);

        // Assert
        result.Should().Be(expectedEntity);
        _mockRepository.Verify(x => x.FindSingleOrDefaultAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithSpecification_DelegatesToRepository()
    {
        // Arrange
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        var expectedEntity = new TestEntity { Id = Guid.NewGuid() };
        _mockRepository.Setup(x => x.FindSingleOrDefaultAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntity);

        // Act
        var result = await _sut.FindSingleOrDefaultAsync(mockSpecification.Object);

        // Assert
        result.Should().Be(expectedEntity);
        _mockRepository.Verify(x => x.FindSingleOrDefaultAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetCountAsync Tests

    [Theory]
    [InlineData(0L)]
    [InlineData(50L)]
    [InlineData(1000L)]
    public async Task GetCountAsync_WithSpecification_DelegatesToRepository(long expectedCount)
    {
        // Arrange
        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        _mockRepository.Setup(x => x.GetCountAsync(mockSpecification.Object, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _sut.GetCountAsync(mockSpecification.Object);

        // Assert
        result.Should().Be(expectedCount);
        _mockRepository.Verify(x => x.GetCountAsync(mockSpecification.Object, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(50L)]
    [InlineData(1000L)]
    public async Task GetCountAsync_WithExpression_DelegatesToRepository(long expectedCount)
    {
        // Arrange
        Expression<Func<TestEntity, bool>> expression = e => true;
        _mockRepository.Setup(x => x.GetCountAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _sut.GetCountAsync(expression);

        // Assert
        result.Should().Be(expectedCount);
        _mockRepository.Verify(x => x.GetCountAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetEnumerator Tests

    [Fact]
    public void GetEnumerator_DelegatesToRepository()
    {
        // Arrange
        var entities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        _mockRepository.Setup(x => x.GetEnumerator()).Returns(entities.GetEnumerator());

        // Act
        var result = _sut.GetEnumerator();

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.GetEnumerator(), Times.Once);
    }

    [Fact]
    public void IEnumerable_GetEnumerator_DelegatesToRepository()
    {
        // Arrange
        var entities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        _mockRepository.Setup(x => x.GetEnumerator()).Returns(entities.GetEnumerator());

        // Act
        var result = ((System.Collections.IEnumerable)_sut).GetEnumerator();

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.GetEnumerator(), Times.Once);
    }

    #endregion

    #region Include Tests

    [Fact]
    public void Include_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<TestEntity, object>> path = e => e.Id;
        var mockEagerLoadable = new Mock<IEagerLoadableQueryable<TestEntity>>();
        _mockRepository.Setup(x => x.Include(It.IsAny<Expression<Func<TestEntity, object>>>())).Returns(mockEagerLoadable.Object);

        // Act
        var result = _sut.Include(path);

        // Assert
        result.Should().BeSameAs(mockEagerLoadable.Object);
        _mockRepository.Verify(x => x.Include(It.IsAny<Expression<Func<TestEntity, object>>>()), Times.Once);
    }

    #endregion

    #region ThenInclude Tests

    [Fact]
    public void ThenInclude_DelegatesToRepository()
    {
        // Arrange
        Expression<Func<object, string>> path = o => string.Empty;
        var mockEagerLoadable = new Mock<IEagerLoadableQueryable<TestEntity>>();
        _mockRepository.Setup(x => x.ThenInclude<object, string>(It.IsAny<Expression<Func<object, string>>>())).Returns(mockEagerLoadable.Object);

        // Act
        var result = _sut.ThenInclude<object, string>(path);

        // Assert
        result.Should().BeSameAs(mockEagerLoadable.Object);
        _mockRepository.Verify(x => x.ThenInclude<object, string>(It.IsAny<Expression<Func<object, string>>>()), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_DelegatesToRepository()
    {
        // Arrange
        var entity = new TestEntity { Id = Guid.NewGuid() };
        _mockRepository.Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.UpdateAsync(entity);

        // Assert
        _mockRepository.Verify(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void CachingLinqRepository_ImplementsICachingLinqRepository()
    {
        // Act & Assert
        _sut.Should().BeAssignableTo<ICachingLinqRepository<TestEntity>>();
    }

    [Fact]
    public void CachingLinqRepository_ImplementsILinqRepository()
    {
        // Act & Assert
        _sut.Should().BeAssignableTo<ILinqRepository<TestEntity>>();
    }

    #endregion
}
