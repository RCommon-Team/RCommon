using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Caching;
using RCommon.MemoryCache;
using RCommon.Persistence.Caching.Crud;
using RCommon.Persistence.Caching.MemoryCache.Tests.TestHelpers;
using Xunit;

namespace RCommon.Persistence.Caching.MemoryCache.Tests;

public class IPersistenceBuilderExtensionsTests
{
    #region AddInMemoryPersistenceCaching Tests

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersICacheService_AsInMemoryCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheService = provider.GetService<ICacheService>();
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<InMemoryCacheService>();
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersInMemoryCacheService_Directly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheService = provider.GetService<InMemoryCacheService>();
        cacheService.Should().NotBeNull();
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersCachingOptions_WithDefaultValues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<CachingOptions>>();
        options.Should().NotBeNull();
        options!.Value.CachingEnabled.Should().BeTrue();
        options.Value.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersCacheServiceFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<Func<PersistenceCachingStrategy, ICacheService>>();
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_CacheServiceFactory_ReturnsInMemoryCacheService_ForDefaultStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<PersistenceCachingStrategy, ICacheService>>();
        var cacheService = factory(PersistenceCachingStrategy.Default);

        // Assert
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<InMemoryCacheService>();
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersICachingGraphRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ICachingGraphRepository<>) &&
            x.ImplementationType == typeof(CachingGraphRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersICachingLinqRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ICachingLinqRepository<>) &&
            x.ImplementationType == typeof(CachingLinqRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersICachingSqlMapperRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ICachingSqlMapperRepository<>) &&
            x.ImplementationType == typeof(CachingSqlMapperRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_RegistersCommonFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ICommonFactory<PersistenceCachingStrategy, ICacheService>));
        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_CalledMultipleTimes_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        builder.AddInMemoryPersistenceCaching();
        builder.AddInMemoryPersistenceCaching();

        // Assert
        var cacheServiceDescriptors = services.Where(x => x.ServiceType == typeof(ICacheService)).ToList();
        cacheServiceDescriptors.Should().HaveCount(1);
    }

    #endregion

    #region AddDistributedMemoryPersistenceCaching Tests

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_RegistersICacheService_AsDistributedMemoryCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheService = provider.GetService<ICacheService>();
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<DistributedMemoryCacheService>();
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_RegistersDistributedMemoryCacheService_Directly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var cacheService = provider.GetService<DistributedMemoryCacheService>();
        cacheService.Should().NotBeNull();
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_RegistersCachingOptions_WithDefaultValues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<CachingOptions>>();
        options.Should().NotBeNull();
        options!.Value.CachingEnabled.Should().BeTrue();
        options.Value.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_RegistersCacheServiceFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var factory = provider.GetService<Func<PersistenceCachingStrategy, ICacheService>>();
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_CacheServiceFactory_ReturnsDistributedMemoryCacheService_ForDefaultStrategy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<Func<PersistenceCachingStrategy, ICacheService>>();
        var cacheService = factory(PersistenceCachingStrategy.Default);

        // Assert
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<DistributedMemoryCacheService>();
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_RegistersICachingGraphRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ICachingGraphRepository<>) &&
            x.ImplementationType == typeof(CachingGraphRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_RegistersICachingLinqRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ICachingLinqRepository<>) &&
            x.ImplementationType == typeof(CachingLinqRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_RegistersICachingSqlMapperRepository()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(ICachingSqlMapperRepository<>) &&
            x.ImplementationType == typeof(CachingSqlMapperRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_CalledMultipleTimes_DoesNotDuplicateServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();
        builder.AddDistributedMemoryPersistenceCaching();
        builder.AddDistributedMemoryPersistenceCaching();

        // Assert
        var cacheServiceDescriptors = services.Where(x => x.ServiceType == typeof(ICacheService)).ToList();
        cacheServiceDescriptors.Should().HaveCount(1);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddInMemoryPersistenceCaching_AllServicesCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert - All required services should be resolvable
        provider.GetService<ICacheService>().Should().NotBeNull();
        provider.GetService<InMemoryCacheService>().Should().NotBeNull();
        provider.GetService<Func<PersistenceCachingStrategy, ICacheService>>().Should().NotBeNull();
        provider.GetService<IOptions<CachingOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddDistributedMemoryPersistenceCaching_AllServicesCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        SetupJsonSerializer(services);
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddDistributedMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert - All required services should be resolvable
        provider.GetService<ICacheService>().Should().NotBeNull();
        provider.GetService<DistributedMemoryCacheService>().Should().NotBeNull();
        provider.GetService<Func<PersistenceCachingStrategy, ICacheService>>().Should().NotBeNull();
        provider.GetService<IOptions<CachingOptions>>().Should().NotBeNull();
    }

    [Fact]
    public void AddInMemoryPersistenceCaching_TransientServices_CreateNewInstancesEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var builder = new TestPersistenceBuilder(services);

        // Act
        builder.AddInMemoryPersistenceCaching();
        var provider = services.BuildServiceProvider();

        // Assert - Transient services should create new instances
        var service1 = provider.GetService<ICacheService>();
        var service2 = provider.GetService<ICacheService>();

        // Note: Even though they're different instances, they share the same IMemoryCache
        service1.Should().NotBeSameAs(service2);
    }

    #endregion

    #region Helper Methods

    private static void SetupJsonSerializer(IServiceCollection services)
    {
        var mockJsonSerializer = new Mock<RCommon.Json.IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
    }

    #endregion
}
