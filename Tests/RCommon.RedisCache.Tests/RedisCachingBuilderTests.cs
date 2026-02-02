using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Caching;
using RCommon.RedisCache;
using Xunit;

namespace RCommon.RedisCache.Tests;

public class RedisCachingBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidRCommonBuilder_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var act = () => new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_SetsServicesProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_ImplementsIRedisCachingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IRedisCachingBuilder>();
    }

    [Fact]
    public void Constructor_ImplementsIDistributedCachingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IDistributedCachingBuilder>();
    }

    #endregion

    #region Services Property Tests

    [Fact]
    public void Services_ReturnsServiceCollectionFromRCommonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<string>("test-service");
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().HaveCount(1);
        builder.Services.Should().Contain(sd => sd.ServiceType == typeof(string));
    }

    [Fact]
    public void Services_AllowsAddingNewServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Services.AddSingleton<ITestService, TestServiceImpl>();

        // Assert
        builder.Services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    [Fact]
    public void Services_ModificationsReflectInOriginalCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Services.AddSingleton<ITestService, TestServiceImpl>();

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    #endregion

    #region Builder Pattern Tests

    [Fact]
    public void Builder_CanBeUsedWithConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);
        var configurationCalled = false;

        // Act
        Action<RedisCachingBuilder> configAction = b =>
        {
            configurationCalled = true;
            b.Services.AddSingleton<ITestService, TestServiceImpl>();
        };
        configAction(builder);

        // Assert
        configurationCalled.Should().BeTrue();
        builder.Services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    [Fact]
    public void Builder_MultipleConfigurationActionsCanBeApplied()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Services.AddSingleton<ITestService, TestServiceImpl>();
        builder.Services.AddTransient<IAnotherTestService, AnotherTestServiceImpl>();

        // Assert
        builder.Services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
        builder.Services.Should().Contain(sd => sd.ServiceType == typeof(IAnotherTestService));
    }

    #endregion

    #region Integration with RCommonBuilder Tests

    [Fact]
    public void Builder_WorksWithRealRCommonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var builder = new RedisCachingBuilder(rcommonBuilder);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Builder_CanAddServicesToRCommonBuilderServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new RedisCachingBuilder(rcommonBuilder);

        // Act
        builder.Services.AddSingleton<ITestService, TestServiceImpl>();
        var provider = services.BuildServiceProvider();
        var testService = provider.GetService<ITestService>();

        // Assert
        testService.Should().NotBeNull();
        testService.Should().BeOfType<TestServiceImpl>();
    }

    #endregion

    #region Empty ServiceCollection Tests

    [Fact]
    public void Builder_WorksWithEmptyServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeEmpty();
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void Builder_CanRegisterTransientService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Services.AddTransient<ITestService, TestServiceImpl>();

        // Assert
        var descriptor = builder.Services.First(sd => sd.ServiceType == typeof(ITestService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void Builder_CanRegisterScopedService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Services.AddScoped<ITestService, TestServiceImpl>();

        // Assert
        var descriptor = builder.Services.First(sd => sd.ServiceType == typeof(ITestService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void Builder_CanRegisterSingletonService()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Services.AddSingleton<ITestService, TestServiceImpl>();

        // Assert
        var descriptor = builder.Services.First(sd => sd.ServiceType == typeof(ITestService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    #endregion

    #region Interface Cast Tests

    [Fact]
    public void Builder_CanBeCastToIRedisCachingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);
        IRedisCachingBuilder cachingBuilder = builder;

        // Assert
        cachingBuilder.Should().NotBeNull();
        cachingBuilder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Builder_CanBeCastToIDistributedCachingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new RedisCachingBuilder(mockRCommonBuilder.Object);
        IDistributedCachingBuilder distributedCachingBuilder = builder;

        // Assert
        distributedCachingBuilder.Should().NotBeNull();
        distributedCachingBuilder.Services.Should().BeSameAs(services);
    }

    #endregion

    #region Multiple Builder Tests

    [Fact]
    public void MultipleBuildersFromSameRCommonBuilder_ShareSameServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder1 = new RedisCachingBuilder(mockRCommonBuilder.Object);
        var builder2 = new RedisCachingBuilder(mockRCommonBuilder.Object);

        builder1.Services.AddSingleton<ITestService, TestServiceImpl>();

        // Assert
        builder2.Services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    #endregion

    #region Test Helper Interfaces and Classes

    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
    private interface IAnotherTestService { }
    private class AnotherTestServiceImpl : IAnotherTestService { }

    #endregion
}
