using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon;
using RCommon.Caching;
using RCommon.Persistence.Caching;
using RCommon.Persistence.Caching.Crud;
using Xunit;

namespace RCommon.Persistence.Caching.Tests;

public class IPersistenceBuilderExtensionsTests
{
    private readonly ServiceCollection _services;
    private readonly Mock<IPersistenceBuilder> _mockPersistenceBuilder;

    public IPersistenceBuilderExtensionsTests()
    {
        _services = new ServiceCollection();
        _mockPersistenceBuilder = new Mock<IPersistenceBuilder>();
        _mockPersistenceBuilder.Setup(x => x.Services).Returns(_services);
    }

    #region AddPersistenceCaching Tests

    [Fact]
    public void AddPersistenceCaching_RegistersCacheServiceFactory()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var resolvedFactory = serviceProvider.GetService<Func<PersistenceCachingStrategy, ICacheService>>();
        resolvedFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceCaching_RegistersCommonFactory()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert
        var descriptor = _services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICommonFactory<PersistenceCachingStrategy, ICacheService>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(CommonFactory<PersistenceCachingStrategy, ICacheService>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddPersistenceCaching_RegistersCachingGraphRepository()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert
        var descriptor = _services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICachingGraphRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(CachingGraphRepository<>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddPersistenceCaching_RegistersCachingLinqRepository()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert
        var descriptor = _services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICachingLinqRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(CachingLinqRepository<>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddPersistenceCaching_RegistersCachingSqlMapperRepository()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert
        var descriptor = _services.FirstOrDefault(d =>
            d.ServiceType == typeof(ICachingSqlMapperRepository<>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(CachingSqlMapperRepository<>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddPersistenceCaching_ConfiguresCachingOptions()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert
        var serviceProvider = _services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptions<CachingOptions>>();
        options.Should().NotBeNull();
        options!.Value.CachingEnabled.Should().BeTrue();
        options.Value.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    [Fact]
    public void AddPersistenceCaching_CacheFactoryReceivesServiceProvider()
    {
        // Arrange
        IServiceProvider? capturedServiceProvider = null;
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp =>
            {
                capturedServiceProvider = sp;
                return strategy => mockCacheService.Object;
            };

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);
        var serviceProvider = _services.BuildServiceProvider();
        var resolvedFactory = serviceProvider.GetRequiredService<Func<PersistenceCachingStrategy, ICacheService>>();

        // Assert
        capturedServiceProvider.Should().NotBeNull();
        // The captured service provider may be a scoped wrapper, so we verify it can resolve services
        capturedServiceProvider!.GetService<Func<PersistenceCachingStrategy, ICacheService>>().Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceCaching_CacheFactoryReceivesStrategy()
    {
        // Arrange
        PersistenceCachingStrategy? capturedStrategy = null;
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy =>
            {
                capturedStrategy = strategy;
                return mockCacheService.Object;
            };

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);
        var serviceProvider = _services.BuildServiceProvider();
        var resolvedFactory = serviceProvider.GetRequiredService<Func<PersistenceCachingStrategy, ICacheService>>();
        resolvedFactory(PersistenceCachingStrategy.Default);

        // Assert
        capturedStrategy.Should().Be(PersistenceCachingStrategy.Default);
    }

    [Fact]
    public void AddPersistenceCaching_MultipleCalls_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert - TryAdd should prevent duplicates
        var graphRepoDescriptors = _services.Where(d =>
            d.ServiceType == typeof(ICachingGraphRepository<>)).ToList();
        graphRepoDescriptors.Should().HaveCount(1);

        var linqRepoDescriptors = _services.Where(d =>
            d.ServiceType == typeof(ICachingLinqRepository<>)).ToList();
        linqRepoDescriptors.Should().HaveCount(1);

        var sqlMapperRepoDescriptors = _services.Where(d =>
            d.ServiceType == typeof(ICachingSqlMapperRepository<>)).ToList();
        sqlMapperRepoDescriptors.Should().HaveCount(1);
    }

    [Fact]
    public void AddPersistenceCaching_AllServicesAreTransient()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);

        // Assert
        var relevantDescriptors = _services.Where(d =>
            d.ServiceType == typeof(ICachingGraphRepository<>) ||
            d.ServiceType == typeof(ICachingLinqRepository<>) ||
            d.ServiceType == typeof(ICachingSqlMapperRepository<>) ||
            d.ServiceType == typeof(ICommonFactory<PersistenceCachingStrategy, ICacheService>) ||
            d.ServiceType == typeof(Func<PersistenceCachingStrategy, ICacheService>)).ToList();

        foreach (var descriptor in relevantDescriptors)
        {
            descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AddPersistenceCaching_CanResolveCommonFactory()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        var factory = serviceProvider.GetService<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceCaching_CommonFactoryReturnsCorrectCacheService()
    {
        // Arrange
        var mockCacheService = new Mock<ICacheService>();
        Func<IServiceProvider, Func<PersistenceCachingStrategy, ICacheService>> cacheFactory =
            sp => strategy => mockCacheService.Object;

        // Act
        _mockPersistenceBuilder.Object.AddPersistenceCaching(cacheFactory);
        var serviceProvider = _services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<ICommonFactory<PersistenceCachingStrategy, ICacheService>>();
        var cacheService = factory.Create(PersistenceCachingStrategy.Default);

        // Assert
        cacheService.Should().BeSameAs(mockCacheService.Object);
    }

    #endregion
}
