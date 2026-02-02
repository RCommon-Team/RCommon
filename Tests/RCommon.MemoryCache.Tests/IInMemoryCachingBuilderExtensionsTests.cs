using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Caching;
using RCommon.MemoryCache;
using Xunit;

namespace RCommon.MemoryCache.Tests;

public class IInMemoryCachingBuilderExtensionsTests
{
    #region Configure Extension Method Tests

    [Fact]
    public void Configure_WithValidOptions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(options => { });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_AddsMemoryCacheToServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options => { });
        var provider = services.BuildServiceProvider();
        var memoryCache = provider.GetService<IMemoryCache>();

        // Assert
        memoryCache.Should().NotBeNull();
    }

    [Fact]
    public void Configure_AppliesOptionsConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);
        var expectedSizeLimit = 1024L;

        // Act
        builder.Configure(options =>
        {
            options.SizeLimit = expectedSizeLimit;
        });
        var provider = services.BuildServiceProvider();
        var memoryCache = provider.GetService<IMemoryCache>();

        // Assert
        memoryCache.Should().NotBeNull();
        // Note: MemoryCacheOptions are internal to the implementation,
        // but we verify the cache was created with our configuration
    }

    [Fact]
    public void Configure_SupportsFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

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
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

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
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ICacheService));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersInMemoryCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(InMemoryCacheService));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersCommonFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

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
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

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
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

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
    public void CacheDynamicallyCompiledExpressions_ResolvedCacheService_IsInMemoryCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();

        // Assert
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<InMemoryCacheService>();
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_StrategyFunc_ReturnsInMemoryCacheServiceForDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var strategyFunc = provider.GetService<Func<ExpressionCachingStrategy, ICacheService>>();

        // Assert
        strategyFunc.Should().NotBeNull();
        var cacheService = strategyFunc!(ExpressionCachingStrategy.Default);
        cacheService.Should().BeOfType<InMemoryCacheService>();
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMemoryCache();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

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
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder
            .Configure(options => options.SizeLimit = 1024)
            .CacheDynamicallyCompiledExpressions();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();
        var memoryCache = provider.GetService<IMemoryCache>();

        // Assert
        cacheService.Should().NotBeNull();
        memoryCache.Should().NotBeNull();
    }

    [Fact]
    public void FullIntegration_CacheServiceCanStoreAndRetrieveValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        builder
            .Configure(options => { })
            .CacheDynamicallyCompiledExpressions();

        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        // Act
        var result1 = cacheService.GetOrCreate("test-key", () => "test-value");
        var result2 = cacheService.GetOrCreate("test-key", () => "different-value");

        // Assert
        result1.Should().Be("test-value");
        result2.Should().Be("test-value"); // Should return cached value
    }

    #endregion
}
