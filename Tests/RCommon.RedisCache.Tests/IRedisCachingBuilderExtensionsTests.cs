using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Caching;
using RCommon.Json;
using RCommon.RedisCache;
using Xunit;

namespace RCommon.RedisCache.Tests;

public class IRedisCachingBuilderExtensionsTests
{
    #region Configure Extension Method Tests

    [Fact]
    public void Configure_WithValidOptions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(options => { });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_AddsStackExchangeRedisCacheServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options =>
        {
            options.Configuration = "localhost:6379";
        });

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IDistributedCache));
    }

    [Fact]
    public void Configure_AppliesOptionsConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);
        var expectedConfiguration = "localhost:6379,abortConnect=false";

        // Act
        builder.Configure(options =>
        {
            options.Configuration = expectedConfiguration;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<RedisCacheOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.Configuration.Should().Be(expectedConfiguration);
    }

    [Fact]
    public void Configure_AppliesInstanceName()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);
        var expectedInstanceName = "MyApp_";

        // Act
        builder.Configure(options =>
        {
            options.Configuration = "localhost:6379";
            options.InstanceName = expectedInstanceName;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<RedisCacheOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.InstanceName.Should().Be(expectedInstanceName);
    }

    [Fact]
    public void Configure_SupportsFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder
            .Configure(options => options.Configuration = "localhost:6379")
            .CacheDynamicallyCompiledExpressions();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        var act = () => builder
            .Configure(options => options.Configuration = "localhost:6379")
            .Configure(options => options.InstanceName = "Test_");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region CacheDynamicallyCompiledExpressions Extension Method Tests

    [Fact]
    public void CacheDynamicallyCompiledExpressions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

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
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ICacheService));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersRedisCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICacheService));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(RedisCacheService));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_RegistersCommonFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

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
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

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
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

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
    public void CacheDynamicallyCompiledExpressions_RegistersServiceAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();

        // Assert
        var cacheServiceDescriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ICacheService));
        cacheServiceDescriptor.Should().NotBeNull();
        cacheServiceDescriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_CalledMultipleTimes_DoesNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        builder.CacheDynamicallyCompiledExpressions();
        builder.CacheDynamicallyCompiledExpressions();

        // Assert - TryAdd should prevent duplicates
        var cacheServiceRegistrations = services.Count(sd => sd.ServiceType == typeof(ICacheService));
        cacheServiceRegistrations.Should().Be(1);
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_StrategyFunc_ReturnsRedisCacheServiceForDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Add required dependencies
        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockDistributedCache.Object);
        services.AddSingleton(mockJsonSerializer.Object);
        services.AddTransient<RedisCacheService>();

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var strategyFunc = provider.GetService<Func<ExpressionCachingStrategy, ICacheService>>();

        // Assert
        strategyFunc.Should().NotBeNull();
        var cacheService = strategyFunc!(ExpressionCachingStrategy.Default);
        cacheService.Should().BeOfType<RedisCacheService>();
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
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder
            .Configure(options => options.Configuration = "localhost:6379")
            .CacheDynamicallyCompiledExpressions();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IDistributedCache));
        services.Should().Contain(sd => sd.ServiceType == typeof(ICacheService));
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_ThenConfigure_WorkTogether()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act - call in reverse order
        builder
            .CacheDynamicallyCompiledExpressions()
            .Configure(options => options.Configuration = "localhost:6379");

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IDistributedCache));
        services.Should().Contain(sd => sd.ServiceType == typeof(ICacheService));
    }

    [Fact]
    public void FullIntegration_WithMockedDependencies_CanResolveRedisCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockDistributedCache.Object);
        services.AddSingleton(mockJsonSerializer.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetService<ICacheService>();

        // Assert
        cacheService.Should().NotBeNull();
        cacheService.Should().BeOfType<RedisCacheService>();
    }

    [Fact]
    public void FullIntegration_WithMockedDependencies_CanUseResolvedCacheService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();

        mockDistributedCache
            .Setup(x => x.Get(It.IsAny<string>()))
            .Returns((byte[]?)null);
        mockJsonSerializer
            .Setup(x => x.Serialize(It.IsAny<object>(), null))
            .Returns("\"test-value\"");

        services.AddSingleton(mockDistributedCache.Object);
        services.AddSingleton(mockJsonSerializer.Object);

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var cacheService = provider.GetRequiredService<ICacheService>();

        var result = cacheService.GetOrCreate("test-key", () => "test-value");

        // Assert
        result.Should().Be("test-value");
        mockDistributedCache.Verify(x => x.Get("test-key"), Times.Once);
    }

    #endregion

    #region Configure Options Tests

    [Fact]
    public void Configure_WithNullConfiguration_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        var act = () => builder.Configure(options => { });

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Configure_WithConnectionMultiplexerConfiguration_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Configure(options =>
        {
            options.Configuration = "localhost:6379,password=test,ssl=true";
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<RedisCacheOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.Configuration.Should().Contain("localhost:6379");
        options.Value.Configuration.Should().Contain("password=test");
        options.Value.Configuration.Should().Contain("ssl=true");
    }

    #endregion

    #region Builder Interface Tests

    [Fact]
    public void Configure_ReturnsSameInterfaceType()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        IRedisCachingBuilder builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(options => options.Configuration = "localhost:6379");

        // Assert
        result.Should().BeAssignableTo<IRedisCachingBuilder>();
    }

    [Fact]
    public void CacheDynamicallyCompiledExpressions_ReturnsSameInterfaceType()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        IRedisCachingBuilder builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.CacheDynamicallyCompiledExpressions();

        // Assert
        result.Should().BeAssignableTo<IRedisCachingBuilder>();
    }

    #endregion

    #region Factory Resolution Tests

    [Fact]
    public void CacheDynamicallyCompiledExpressions_CommonFactory_CanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        var mockDistributedCache = new Mock<IDistributedCache>();
        var mockJsonSerializer = new Mock<IJsonSerializer>();
        services.AddSingleton(mockDistributedCache.Object);
        services.AddSingleton(mockJsonSerializer.Object);
        services.AddTransient<RedisCacheService>();

        // Act
        builder.CacheDynamicallyCompiledExpressions();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<ICommonFactory<ExpressionCachingStrategy, ICacheService>>();

        // Assert
        factory.Should().NotBeNull();
    }

    #endregion
}
