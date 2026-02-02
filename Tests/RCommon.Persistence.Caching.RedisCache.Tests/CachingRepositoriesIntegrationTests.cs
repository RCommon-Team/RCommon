using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon;
using RCommon.Caching;
using RCommon.Collections;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.Json;
using RCommon.Models.Events;
using RCommon.Persistence.Caching;
using RCommon.Persistence.Caching.Crud;
using RCommon.Persistence.Caching.RedisCache;
using RCommon.Persistence.Crud;
using RCommon.RedisCache;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Caching.RedisCache.Tests;

/// <summary>
/// Integration tests to verify caching repositories work correctly with Redis cache service
/// when configured through the RCommon.Persistence.Caching.RedisCache extension method.
/// </summary>
public class CachingRepositoriesIntegrationTests
{
    #region CachingGraphRepository Integration Tests

    [Fact]
    public async Task CachingGraphRepository_FindAsync_WithCacheKey_UsesCacheService()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheService = new Mock<ICacheService>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(mockCacheService.Object);

        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        var cacheKey = "test-cache-key";

        mockCacheService
            .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()))
            .ReturnsAsync(Task.FromResult<ICollection<TestEntity>>(expectedEntities));

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        var result = await cachingRepo.FindAsync(cacheKey, x => true);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        mockCacheService.Verify(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_FindAsync_WithoutCacheKey_DelegatesDirectlyToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        var mockCacheService = new Mock<ICacheService>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(mockCacheService.Object);

        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        Expression<Func<TestEntity, bool>> expression = x => true;

        mockRepository
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        var result = await cachingRepo.FindAsync(expression);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        mockRepository.Verify(x => x.FindAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCacheService.Verify(x => x.GetOrCreateAsync(It.IsAny<object>(), It.IsAny<Func<Task<ICollection<TestEntity>>>>()), Times.Never);
    }

    [Fact]
    public async Task CachingGraphRepository_AddAsync_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var entity = new TestEntity { Id = Guid.NewGuid() };

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        await cachingRepo.AddAsync(entity);

        // Assert
        mockRepository.Verify(x => x.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_DeleteAsync_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var entity = new TestEntity { Id = Guid.NewGuid() };

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        await cachingRepo.DeleteAsync(entity);

        // Assert
        mockRepository.Verify(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_UpdateAsync_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var entity = new TestEntity { Id = Guid.NewGuid() };

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        await cachingRepo.UpdateAsync(entity);

        // Assert
        mockRepository.Verify(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_AddRangeAsync_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var entities = new List<TestEntity>
        {
            new TestEntity { Id = Guid.NewGuid() },
            new TestEntity { Id = Guid.NewGuid() }
        };

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        await cachingRepo.AddRangeAsync(entities);

        // Assert
        mockRepository.Verify(x => x.AddRangeAsync(entities, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CachingGraphRepository_AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        Func<Task> act = async () => await cachingRepo.AddRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    [Fact]
    public void CachingGraphRepository_Tracking_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        mockRepository.SetupProperty(x => x.Tracking, false);

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        cachingRepo.Tracking = true;

        // Assert
        mockRepository.Object.Tracking.Should().BeTrue();
        cachingRepo.Tracking.Should().BeTrue();
    }

    [Fact]
    public void CachingGraphRepository_DataStoreName_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        mockRepository.SetupProperty(x => x.DataStoreName, "");

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        cachingRepo.DataStoreName = "TestDataStore";

        // Assert
        mockRepository.Object.DataStoreName.Should().Be("TestDataStore");
        cachingRepo.DataStoreName.Should().Be("TestDataStore");
    }

    #endregion

    #region CachingLinqRepository Integration Tests

    [Fact]
    public async Task CachingLinqRepository_FindAsync_WithCacheKey_UsesCacheService()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheService = new Mock<ICacheService>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(mockCacheService.Object);

        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        var cacheKey = "linq-test-cache-key";

        mockCacheService
            .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()))
            .ReturnsAsync(Task.FromResult<ICollection<TestEntity>>(expectedEntities));

        var cachingRepo = new CachingLinqRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        var result = await cachingRepo.FindAsync(cacheKey, x => true);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        mockCacheService.Verify(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()), Times.Once);
    }

    [Fact]
    public async Task CachingLinqRepository_AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var cachingRepo = new CachingLinqRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        Func<Task> act = async () => await cachingRepo.AddRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    #endregion

    #region CachingSqlMapperRepository Integration Tests

    [Fact]
    public async Task CachingSqlMapperRepository_FindAsync_WithCacheKey_UsesCacheService()
    {
        // Arrange
        var mockRepository = new Mock<ISqlMapperRepository<TestEntity>>();
        var mockCacheService = new Mock<ICacheService>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(mockCacheService.Object);

        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid() } };
        var cacheKey = "sql-test-cache-key";

        mockCacheService
            .Setup(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()))
            .ReturnsAsync(Task.FromResult<ICollection<TestEntity>>(expectedEntities));

        var cachingRepo = new CachingSqlMapperRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        var result = await cachingRepo.FindAsync(cacheKey, x => true);

        // Assert
        result.Should().BeEquivalentTo(expectedEntities);
        mockCacheService.Verify(x => x.GetOrCreateAsync(cacheKey, It.IsAny<Func<Task<ICollection<TestEntity>>>>()), Times.Once);
    }

    [Fact]
    public async Task CachingSqlMapperRepository_AddAsync_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<ISqlMapperRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var entity = new TestEntity { Id = Guid.NewGuid() };

        var cachingRepo = new CachingSqlMapperRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        await cachingRepo.AddAsync(entity);

        // Assert
        mockRepository.Verify(x => x.AddAsync(entity, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CachingSqlMapperRepository_AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<ISqlMapperRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        var cachingRepo = new CachingSqlMapperRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        Func<Task> act = async () => await cachingRepo.AddRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    [Fact]
    public void CachingSqlMapperRepository_TableName_DelegatesToRepository()
    {
        // Arrange
        var mockRepository = new Mock<ISqlMapperRepository<TestEntity>>();
        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(Mock.Of<ICacheService>());

        mockRepository.SetupProperty(x => x.TableName, "");

        var cachingRepo = new CachingSqlMapperRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);

        // Act
        cachingRepo.TableName = "TestTable";

        // Assert
        mockRepository.Object.TableName.Should().Be("TestTable");
        cachingRepo.TableName.Should().Be("TestTable");
    }

    #endregion

    #region Full Integration Tests with Redis Cache Service

    [Fact]
    public async Task CachingGraphRepository_WithRedisCacheService_CachesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var distributedCache = CreateMemoryDistributedCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();

        var expectedEntities = new List<TestEntity> { new TestEntity { Id = Guid.NewGuid(), Name = "Test" } };
        var serializedData = "[{\"Id\":\"test\",\"Name\":\"Test\"}]";

        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns(serializedData);
        mockJsonSerializer
            .Setup(x => x.Deserialize<Task<ICollection<TestEntity>>>(serializedData, null))
            .Returns(Task.FromResult<ICollection<TestEntity>>(expectedEntities));

        var redisCacheService = new RedisCacheService(distributedCache, mockJsonSerializer.Object);

        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        mockRepository
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<TestEntity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        var mockCacheFactory = new Mock<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        mockCacheFactory.Setup(x => x.Create(PersistenceCachingStrategy.Default)).Returns(redisCacheService);

        var cachingRepo = new CachingGraphRepository<TestEntity>(mockRepository.Object, mockCacheFactory.Object);
        var cacheKey = "redis-integration-test-key";

        // Act - First call should populate cache
        var result1 = await cachingRepo.FindAsync(cacheKey, x => true);

        // Assert - Cache should have been populated
        var cachedValue = await distributedCache.GetStringAsync(cacheKey);
        cachedValue.Should().NotBeNull();
    }

    #endregion

    #region Service Collection Integration Tests

    [Fact]
    public void ServiceCollection_CanResolveAllCachingRepositories_WithDependencies()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Add required dependencies
        services.AddSingleton(Mock.Of<IDistributedCache>());
        services.AddSingleton(Mock.Of<IJsonSerializer>());
        services.AddTransient<IGraphRepository<TestEntity>>(sp => Mock.Of<IGraphRepository<TestEntity>>());
        services.AddTransient<ISqlMapperRepository<TestEntity>>(sp => Mock.Of<ISqlMapperRepository<TestEntity>>());

        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        var provider = services.BuildServiceProvider();

        // Act & Assert
        var graphRepo = provider.GetService<ICachingGraphRepository<TestEntity>>();
        var linqRepo = provider.GetService<ICachingLinqRepository<TestEntity>>();
        var sqlMapperRepo = provider.GetService<ICachingSqlMapperRepository<TestEntity>>();

        graphRepo.Should().NotBeNull();
        linqRepo.Should().NotBeNull();
        sqlMapperRepo.Should().NotBeNull();
    }

    [Fact]
    public void CacheServiceFactory_ReturnsRedisCacheService_ForAllStrategies()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        services.AddSingleton(Mock.Of<IDistributedCache>());
        services.AddSingleton(Mock.Of<IJsonSerializer>());

        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<PersistenceCachingStrategy, ICacheService>>();

        // Act - Test Default strategy
        var cacheService = factory(PersistenceCachingStrategy.Default);

        // Assert
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<RedisCacheService>();
    }

    #endregion

    #region Helper Methods

    private static MemoryDistributedCache CreateMemoryDistributedCache()
    {
        var options = Options.Create(new MemoryDistributedCacheOptions());
        return new MemoryDistributedCache(options);
    }

    #endregion

    #region Test Entity

    public class TestEntity : IBusinessEntity<Guid>
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public bool AllowEventTracking { get; set; } = true;

        public IReadOnlyCollection<ISerializableEvent> LocalEvents => _localEvents.AsReadOnly();
        private readonly List<ISerializableEvent> _localEvents = new();

        public void AddLocalEvent(ISerializableEvent eventItem) => _localEvents.Add(eventItem);
        public void ClearLocalEvents() => _localEvents.Clear();
        public void RemoveLocalEvent(ISerializableEvent eventItem) => _localEvents.Remove(eventItem);

        public bool EntityEquals(IBusinessEntity other)
        {
            if (other is TestEntity otherEntity)
                return Id == otherEntity.Id;
            return false;
        }

        public object[] GetKeys() => new object[] { Id };
    }

    #endregion
}
