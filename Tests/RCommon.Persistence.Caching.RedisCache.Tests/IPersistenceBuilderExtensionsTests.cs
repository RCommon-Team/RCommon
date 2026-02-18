using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon;
using RCommon.Caching;
using RCommon.Persistence.Caching;
using RCommon.Persistence.Caching.Crud;
using RCommon.Persistence.Caching.RedisCache;
using RCommon.RedisCache;
using Xunit;

namespace RCommon.Persistence.Caching.RedisCache.Tests;

public class IPersistenceBuilderExtensionsTests
{
    #region AddRedisPersistenceCaching Tests

    [Fact]
    public void AddRedisPersistenceCaching_RegistersRedisCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(RedisCacheService) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRedisPersistenceCaching_RegistersICacheService_AsRedisCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ICacheService) &&
            sd.ImplementationType == typeof(RedisCacheService) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRedisPersistenceCaching_RegistersCachingGraphRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ICachingGraphRepository<>) &&
            sd.ImplementationType == typeof(CachingGraphRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRedisPersistenceCaching_RegistersCachingLinqRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ICachingLinqRepository<>) &&
            sd.ImplementationType == typeof(CachingLinqRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRedisPersistenceCaching_RegistersCachingSqlMapperRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ICachingSqlMapperRepository<>) &&
            sd.ImplementationType == typeof(CachingSqlMapperRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRedisPersistenceCaching_RegistersCommonFactory_ForPersistenceCachingStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ICommonFactory<PersistenceCachingStrategy, ICacheService>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRedisPersistenceCaching_RegistersCacheServiceFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(Func<PersistenceCachingStrategy, ICacheService>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddRedisPersistenceCaching_ConfiguresCachingOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IConfigureOptions<CachingOptions>));
    }

    #endregion

    #region CachingOptions Configuration Tests

    [Fact]
    public void AddRedisPersistenceCaching_EnablesCachingByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Build and resolve
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<CachingOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.CachingEnabled.Should().BeTrue();
    }

    [Fact]
    public void AddRedisPersistenceCaching_EnablesCacheDynamicallyCompiledExpressionsByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Build and resolve
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<CachingOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    #endregion

    #region Factory Resolution Tests

    [Fact]
    public void CacheServiceFactory_WithDefaultStrategy_ReturnsRedisCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Add required dependencies for RedisCacheService
        services.AddSingleton(Mock.Of<Microsoft.Extensions.Caching.Distributed.IDistributedCache>());
        services.AddSingleton(Mock.Of<RCommon.Json.IJsonSerializer>());

        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<Func<PersistenceCachingStrategy, ICacheService>>();

        // Act
        var cacheService = factory?.Invoke(PersistenceCachingStrategy.Default);

        // Assert
        factory.Should().NotBeNull();
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<RedisCacheService>();
    }

    #endregion

    #region TryAdd Behavior Tests

    [Fact]
    public void AddRedisPersistenceCaching_DoesNotReplaceExistingICacheServiceRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockCacheService = new Mock<ICacheService>();
        services.AddTransient<ICacheService>(_ => mockCacheService.Object);

        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Build and resolve
        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();

        // Assert - should return the original mock, not RedisCacheService
        cacheService.Should().BeSameAs(mockCacheService.Object);
    }

    [Fact]
    public void AddRedisPersistenceCaching_DoesNotReplaceExistingRepositoryRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Pre-register a custom implementation
        services.AddTransient(typeof(ICachingLinqRepository<>), typeof(CustomCachingLinqRepository<>));

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert - should keep the original registration
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICachingLinqRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(CustomCachingLinqRepository<>));
    }

    [Fact]
    public void AddRedisPersistenceCaching_CanBeCalledMultipleTimes_WithoutDuplicatingRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert - should only have one registration due to TryAdd
        var redisCacheServiceRegistrations = services.Count(sd => sd.ServiceType == typeof(RedisCacheService));
        redisCacheServiceRegistrations.Should().Be(1);
    }

    #endregion

    #region Multiple Registration Tests

    [Fact]
    public void AddRedisPersistenceCaching_AllRepositoriesAreRegisteredAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        // Act
        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();

        // Assert
        var graphRepoDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICachingGraphRepository<>));
        var linqRepoDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICachingLinqRepository<>));
        var sqlMapperRepoDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICachingSqlMapperRepository<>));

        graphRepoDescriptor?.Lifetime.Should().Be(ServiceLifetime.Transient);
        linqRepoDescriptor?.Lifetime.Should().Be(ServiceLifetime.Transient);
        sqlMapperRepoDescriptor?.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    #endregion

    #region Service Provider Resolution Tests

    [Fact]
    public void ServiceProvider_CanResolveIOptions_CachingOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        mockPersistenceBuilder.Setup(x => x.Services).Returns(services);

        mockPersistenceBuilder.Object.AddRedisPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Act
        var options = provider.GetService<IOptions<CachingOptions>>();

        // Assert
        options.Should().NotBeNull();
    }

    #endregion

    #region Helper Classes for Testing

    private class CustomCachingLinqRepository<TEntity> : ICachingLinqRepository<TEntity>
        where TEntity : class, RCommon.Entities.IBusinessEntity
    {
        public Type ElementType => throw new NotImplementedException();
        public System.Linq.Expressions.Expression Expression => throw new NotImplementedException();
        public IQueryProvider Provider => throw new NotImplementedException();
        public string DataStoreName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Task AddAsync(TEntity entity, CancellationToken token = default) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default) => throw new NotImplementedException();
        public Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default) => throw new NotImplementedException();
        public Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default) => throw new NotImplementedException();
        public Task DeleteAsync(TEntity entity, CancellationToken token = default) => throw new NotImplementedException();
        public Task DeleteAsync(TEntity entity, bool isSoftDelete, CancellationToken token = default) => throw new NotImplementedException();
        public Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default) => throw new NotImplementedException();
        public Task<int> DeleteManyAsync(ISpecification<TEntity> specification, bool isSoftDelete, CancellationToken token = default) => throw new NotImplementedException();
        public Task<int> DeleteManyAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default) => throw new NotImplementedException();
        public Task<int> DeleteManyAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default) => throw new NotImplementedException();
        public Task<Collections.IPaginatedList<TEntity>> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, System.Linq.Expressions.Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default) => throw new NotImplementedException();
        public Task<Collections.IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default) => throw new NotImplementedException();
        public Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default) => throw new NotImplementedException();
        public Task<ICollection<TEntity>> FindAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default) => throw new NotImplementedException();
        public Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default) => throw new NotImplementedException();
        public Task<Collections.IPaginatedList<TEntity>> FindAsync(object cacheKey, System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, System.Linq.Expressions.Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0, CancellationToken token = default) => throw new NotImplementedException();
        public Task<Collections.IPaginatedList<TEntity>> FindAsync(object cacheKey, IPagedSpecification<TEntity> specification, CancellationToken token = default) => throw new NotImplementedException();
        public Task<ICollection<TEntity>> FindAsync(object cacheKey, ISpecification<TEntity> specification, CancellationToken token = default) => throw new NotImplementedException();
        public Task<ICollection<TEntity>> FindAsync(object cacheKey, System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default) => throw new NotImplementedException();
        public IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification) => throw new NotImplementedException();
        public IQueryable<TEntity> FindQuery(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression) => throw new NotImplementedException();
        public IQueryable<TEntity> FindQuery(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, System.Linq.Expressions.Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0) => throw new NotImplementedException();
        public IQueryable<TEntity> FindQuery(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, System.Linq.Expressions.Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending) => throw new NotImplementedException();
        public IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification) => throw new NotImplementedException();
        public Task<TEntity> FindSingleOrDefaultAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default) => throw new NotImplementedException();
        public Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default) => throw new NotImplementedException();
        public Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default) => throw new NotImplementedException();
        public Task<long> GetCountAsync(System.Linq.Expressions.Expression<Func<TEntity, bool>> expression, CancellationToken token = default) => throw new NotImplementedException();
        public IEnumerator<TEntity> GetEnumerator() => throw new NotImplementedException();
        public RCommon.Persistence.Crud.IEagerLoadableQueryable<TEntity> Include(System.Linq.Expressions.Expression<Func<TEntity, object>> path) => throw new NotImplementedException();
        public RCommon.Persistence.Crud.IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(System.Linq.Expressions.Expression<Func<object, TProperty>> path) => throw new NotImplementedException();
        public Task UpdateAsync(TEntity entity, CancellationToken token = default) => throw new NotImplementedException();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    #endregion
}
