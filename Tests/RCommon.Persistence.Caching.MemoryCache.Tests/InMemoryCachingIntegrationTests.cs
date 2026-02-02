using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

/// <summary>
/// Integration tests that verify the complete wiring of caching repositories
/// with in-memory caching through dependency injection.
/// </summary>
public class InMemoryCachingIntegrationTests
{
    #region InMemory Cache Full Integration Tests

    [Fact]
    public void InMemoryPersistenceCaching_FullIntegration_AllServicesResolve()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        // Add mock repositories for complete resolution
        var mockGraphRepo = new Mock<IGraphRepository<TestEntity>>();
        var mockSqlRepo = new Mock<ISqlMapperRepository<TestEntity>>();
        services.AddTransient(_ => mockGraphRepo.Object);
        services.AddTransient(_ => mockSqlRepo.Object);

        var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetService<ICacheService>().Should().NotBeNull().And.BeOfType<InMemoryCacheService>();
        provider.GetService<InMemoryCacheService>().Should().NotBeNull();
        provider.GetService<IOptions<CachingOptions>>().Should().NotBeNull();
        provider.GetService<Func<PersistenceCachingStrategy, ICacheService>>().Should().NotBeNull();
        provider.GetService<ICommonFactory<PersistenceCachingStrategy, ICacheService>>().Should().NotBeNull();
    }

    [Fact]
    public void InMemoryPersistenceCaching_CacheFactory_ReturnsCorrectServiceForDefaultStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<PersistenceCachingStrategy, ICacheService>>();

        // Act
        var cacheService = factory(PersistenceCachingStrategy.Default);

        // Assert
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<InMemoryCacheService>();
    }

    [Fact]
    public void InMemoryPersistenceCaching_CachingOptions_HasCorrectDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetRequiredService<IOptions<CachingOptions>>().Value;

        // Assert
        options.CachingEnabled.Should().BeTrue();
        options.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryPersistenceCaching_CacheService_WorksEndToEnd()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        var key = "integration-test-key";
        var value = new TestEntity { Name = "IntegrationTestEntity" };

        // Act - First call should invoke the factory
        var result1 = await cacheService.GetOrCreateAsync(key, () => value);

        // Act - Second call should return cached value
        var result2 = await cacheService.GetOrCreateAsync(key, () => new TestEntity { Name = "DifferentEntity" });

        // Assert
        result1.Should().BeEquivalentTo(value);
        result2.Should().BeEquivalentTo(value);
        result2.Name.Should().Be("IntegrationTestEntity"); // Should be cached value, not new value
    }

    [Fact]
    public void InMemoryPersistenceCaching_CacheService_SyncMethodWorksEndToEnd()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        var key = "sync-integration-test-key";
        var callCount = 0;

        // Act - Multiple calls with same key
        var result1 = cacheService.GetOrCreate(key, () => { callCount++; return "value"; });
        var result2 = cacheService.GetOrCreate(key, () => { callCount++; return "different-value"; });
        var result3 = cacheService.GetOrCreate(key, () => { callCount++; return "another-value"; });

        // Assert
        result1.Should().Be("value");
        result2.Should().Be("value");
        result3.Should().Be("value");
        callCount.Should().Be(1); // Factory should only be called once
    }

    #endregion

    #region Caching Repository Integration Tests

    [Fact]
    public async Task CachingGraphRepository_WithInMemoryCache_CachesQueryResults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "CachedEntity" } };
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        mockRepository
            .Setup(x => x.FindAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        services.AddTransient(_ => mockRepository.Object);

        var provider = services.BuildServiceProvider();
        var cacheFactory = provider.GetRequiredService<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();

        var cachingRepository = new CachingGraphRepository<TestEntity>(mockRepository.Object, cacheFactory);

        // Act - First call
        var result1 = await cachingRepository.FindAsync("cache-key", expression);
        mockRepository.Invocations.Clear();

        // Second call should hit cache
        var result2 = await cachingRepository.FindAsync("cache-key", expression);

        // Assert
        result1.Should().BeEquivalentTo(expectedEntities);
        result2.Should().BeEquivalentTo(expectedEntities);
        mockRepository.Verify(x => x.FindAsync(expression, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CachingLinqRepository_WithInMemoryCache_CachesQueryResults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var mockRepository = new Mock<IGraphRepository<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "LinqCachedEntity" } };
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        mockRepository
            .Setup(x => x.FindAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        services.AddTransient(_ => mockRepository.Object);

        var provider = services.BuildServiceProvider();
        var cacheFactory = provider.GetRequiredService<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();

        var cachingRepository = new CachingLinqRepository<TestEntity>(mockRepository.Object, cacheFactory);

        // Act - First call
        var result1 = await cachingRepository.FindAsync("linq-cache-key", expression);
        mockRepository.Invocations.Clear();

        // Second call should hit cache
        var result2 = await cachingRepository.FindAsync("linq-cache-key", expression);

        // Assert
        result1.Should().BeEquivalentTo(expectedEntities);
        result2.Should().BeEquivalentTo(expectedEntities);
        mockRepository.Verify(x => x.FindAsync(expression, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CachingSqlMapperRepository_WithInMemoryCache_CachesQueryResults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var mockRepository = new Mock<ISqlMapperRepository<TestEntity>>();
        var expectedEntities = new List<TestEntity> { new TestEntity { Name = "SqlCachedEntity" } };
        Expression<Func<TestEntity, bool>> expression = x => x.IsActive;

        mockRepository
            .Setup(x => x.FindAsync(expression, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedEntities);

        services.AddTransient(_ => mockRepository.Object);

        var provider = services.BuildServiceProvider();
        var cacheFactory = provider.GetRequiredService<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();

        var cachingRepository = new CachingSqlMapperRepository<TestEntity>(mockRepository.Object, cacheFactory);

        // Act - First call
        var result1 = await cachingRepository.FindAsync("sql-cache-key", expression);
        mockRepository.Invocations.Clear();

        // Second call should hit cache
        var result2 = await cachingRepository.FindAsync("sql-cache-key", expression);

        // Assert
        result1.Should().BeEquivalentTo(expectedEntities);
        result2.Should().BeEquivalentTo(expectedEntities);
        mockRepository.Verify(x => x.FindAsync(expression, It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Cross-Repository Cache Sharing Tests

    [Fact]
    public async Task MultipleRepositories_ShareSameCacheService_WhenUsingSameKey()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        var sharedKey = "shared-cache-key";
        var entity = new TestEntity { Name = "SharedEntity" };

        // Act - Cache through one instance
        var result1 = await cacheService.GetOrCreateAsync(sharedKey, () => entity);

        // Get another instance and try to retrieve
        var cacheService2 = provider.GetRequiredService<ICacheService>();
        var result2 = await cacheService2.GetOrCreateAsync(sharedKey, () => new TestEntity { Name = "DifferentEntity" });

        // Assert - Both should return the same cached entity
        result1.Name.Should().Be("SharedEntity");
        result2.Name.Should().Be("SharedEntity");
    }

    #endregion

    #region Service Lifetime Tests

    [Fact]
    public void InMemoryPersistenceCaching_ServicesAreTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();

        // Act
        var cacheService1 = provider.GetService<ICacheService>();
        var cacheService2 = provider.GetService<ICacheService>();

        // Assert - Transient services should create new instances
        cacheService1.Should().NotBeSameAs(cacheService2);
    }

    [Fact]
    public void InMemoryPersistenceCaching_UnderlyingMemoryCache_IsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();

        // Act
        var memoryCache1 = provider.GetService<IMemoryCache>();
        var memoryCache2 = provider.GetService<IMemoryCache>();

        // Assert - IMemoryCache should be the same instance (singleton by default)
        memoryCache1.Should().BeSameAs(memoryCache2);
    }

    #endregion

    #region Complex Object Caching Tests

    [Fact]
    public async Task InMemoryPersistenceCaching_CachesComplexObjects_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        var complexEntity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "ComplexEntity",
            Description = "A complex entity with multiple properties",
            IsActive = true,
            CreatedDate = DateTime.UtcNow.AddDays(-30)
        };

        // Act
        var cached = await cacheService.GetOrCreateAsync("complex-entity-key", () => complexEntity);

        // Assert
        cached.Should().NotBeNull();
        cached.Id.Should().Be(complexEntity.Id);
        cached.Name.Should().Be(complexEntity.Name);
        cached.Description.Should().Be(complexEntity.Description);
        cached.IsActive.Should().Be(complexEntity.IsActive);
        cached.CreatedDate.Should().Be(complexEntity.CreatedDate);
    }

    [Fact]
    public async Task InMemoryPersistenceCaching_CachesCollections_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);
        builder.AddInMemoryPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        var entityCollection = new List<TestEntity>
        {
            new TestEntity { Name = "Entity1" },
            new TestEntity { Name = "Entity2" },
            new TestEntity { Name = "Entity3" }
        };

        // Act
        var cached = await cacheService.GetOrCreateAsync("entity-collection-key", () => entityCollection);

        // Assert
        cached.Should().NotBeNull();
        cached.Should().HaveCount(3);
        cached.Should().Contain(e => e.Name == "Entity1");
        cached.Should().Contain(e => e.Name == "Entity2");
        cached.Should().Contain(e => e.Name == "Entity3");
    }

    #endregion
}
