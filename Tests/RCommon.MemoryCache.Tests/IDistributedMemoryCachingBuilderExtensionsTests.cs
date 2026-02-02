using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Caching;
using RCommon.Json;
using RCommon.MemoryCache;
using Xunit;

namespace RCommon.MemoryCache.Tests;

public class IDistributedMemoryCachingBuilderExtensionsTests
{
    #region Configure Extension Method Tests

    [Fact]
    public void Configure_WithValidOptions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(options => { });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_AddsDistributedMemoryCacheToServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => { });
        var provider = services.BuildServiceProvider();
        var distributedCache = provider.GetService<IDistributedCache>();

        // Assert
        distributedCache.Should().NotBeNull();
    }

    [Fact]
    public void Configure_AppliesOptionsConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);
        var expectedSizeLimit = 2048L;

        // Act
        builder.Configure(options =>
        {
            options.SizeLimit = expectedSizeLimit;
        });
        var provider = services.BuildServiceProvider();
        var distributedCache = provider.GetService<IDistributedCache>();

        // Assert
        distributedCache.Should().NotBeNull();
    }

    [Fact]
    public void Configure_SupportsFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder
            .Configure(options => options.SizeLimit = 100)
            .CacheDynamicallyCompiledExpressions();

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion

    #region CacheDynamicallyCompiledExpressions Extension Method Tests

    [Fact]
    public void CacheDynamicallyCompiledExpressions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.CacheDynamicallyCompiledExpressions();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersICacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ICacheService));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersDistributedMemoryCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(DistributedMemoryCacheService));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersCommonFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ICommonFactory<ExpressionCachingStrategy, ICacheService>));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersCachingStrategyFunc()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(Func<ExpressionCachingStrategy, ICacheService>));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_ConfiguresCachingOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<CachingOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.CachingEnabled.Should().BeTrue();
        options.Value.CacheDynamicallyCompiledExpressions.Should().BeTrue();
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_ResolvedCacheService_IsDistributedMemoryCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();

        // Assert
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<DistributedMemoryCacheService>();
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_StrategyFunc_ReturnsDistributedMemoryCacheServiceForDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var strategyFunc = provider.GetService<Func<ExpressionCachingStrategy, ICacheService>>();

        // Assert
        strategyFunc.Should().NotBeNull();
        var cacheService = strategyFunc!(ExpressionCachingStrategy.Default);
        cacheService.Should().BeOfType<DistributedMemoryCacheService>();
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        builder.CacheDynamicallyCompiledExpressions();
        builder.CacheDynamicallyCompiledExpressions();

        // Assert - TryAdd should prevent duplicates
        var cacheServiceRegistrations = services.Count(sd => sd.ServiceType == typeof(ICacheService));
        cacheServiceRegistrations.Should().Be(1);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Configure_AndCacheDynamicallyCompiledExpressions_WorkTogether()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Act
        builder
            .Configure(options => options.SizeLimit = 1024)
            .CacheDynamicallyCompiledExpressions();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();
        var distributedCache = provider.GetService<IDistributedCache>();

        // Assert
        cacheService.Should().NotBeNull();
        distributedCache.Should().NotBeNull();
    }

    [Fact]
    public void FullIntegration_CacheServiceCanStoreAndRetrieveValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        mockJsonSerializer.Setup(x => x.Serialize(It.IsAny<object>(), null)).Returns("\"test-value\"");
        mockJsonSerializer.Setup(x => x.Deserialize<string>(It.IsAny<string>(), null)).Returns("test-value");
        services.AddSingleton(mockJsonSerializer.Object);
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        builder
            .Configure(options => { })
            .CacheDynamicallyCompiledExpressions();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        // Act
        var result1 = cacheService.GetOrCreate("test-key", () => "test-value");

        // Assert
        result1.Should().Be("test-value");
    }

    #endregion

    #region Comparison with InMemory Builder Extensions Tests

    [Fact]
    public void DistributedAndInMemory_RegisterDifferentCacheServiceImplementations()
    {
        // Arrange
        var inMemoryServices = new ServiceCollection();
        inMemoryServices.AddMemoryCache();
        var mockInMemoryBuilder = new Mock<IRCommonBuilder>();
        mockInMemoryBuilder.Setup(x => x.Services).Returns(inMemoryServices);
        var inMemoryBuilder = new InMemoryCachingBuilder(mockInMemoryBuilder.Object);

        var distributedServices = new ServiceCollection();
        distributedServices.AddDistributedMemoryCache();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        distributedServices.AddSingleton(mockJsonSerializer.Object);
        var mockDistributedBuilder = new Mock<IRCommonBuilder>();
        mockDistributedBuilder.Setup(x => x.Services).Returns(distributedServices);
        var distributedBuilder = new DistributedMemoryCacheBuilder(mockDistributedBuilder.Object);

        // Act
        inMemoryBuilder.CacheDynamicallyCompiledExpressions();
        distributedBuilder.CacheDynamicallyCompiledExpressions();

        var inMemoryProvider = inMemoryServices.BuildServiceProvider();
        var distributedProvider = distributedServices.BuildServiceProvider();

        var inMemoryCacheService = inMemoryProvider.GetService<ICacheService>();
        var distributedCacheService = distributedProvider.GetService<ICacheService>();

        // Assert
        inMemoryCacheService.Should().BeOfType<InMemoryCacheService>();
        distributedCacheService.Should().BeOfType<DistributedMemoryCacheService>();
    }

    #endregion
}
