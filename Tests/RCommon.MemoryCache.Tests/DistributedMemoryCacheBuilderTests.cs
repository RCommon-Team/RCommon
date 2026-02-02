using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Caching;
using RCommon.MemoryCache;
using Xunit;

namespace RCommon.MemoryCache.Tests;

public class DistributedMemoryCacheBuilderTests
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
        var act = () => new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

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
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_ImplementsIDistributedMemoryCachingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IDistributedMemoryCachingBuilder>();
    }

    [Fact]
    public void Constructor_ImplementsIDistributedCachingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

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
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

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
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

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
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

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
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);
        var configurationCalled = false;

        // Act
        Action<DistributedMemoryCacheBuilder> configAction = b =>
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
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

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
        var builder = new DistributedMemoryCacheBuilder(rcommonBuilder);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Builder_CanAddServicesToRCommonBuilderServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new DistributedMemoryCacheBuilder(rcommonBuilder);

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
        var builder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeEmpty();
    }

    #endregion

    #region Comparison with InMemoryCachingBuilder Tests

    [Fact]
    public void DistributedBuilder_AndInMemoryBuilder_HaveSameBaseInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var distributedBuilder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);
        var inMemoryBuilder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Assert
        // Both should have Services property
        distributedBuilder.Services.Should().NotBeNull();
        inMemoryBuilder.Services.Should().NotBeNull();
    }

    [Fact]
    public void DistributedBuilder_AndInMemoryBuilder_ShareSameServicesCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var distributedBuilder = new DistributedMemoryCacheBuilder(mockRCommonBuilder.Object);
        var inMemoryBuilder = new InMemoryCachingBuilder(mockRCommonBuilder.Object);

        // Act
        distributedBuilder.Services.AddSingleton<ITestService, TestServiceImpl>();

        // Assert
        inMemoryBuilder.Services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    #endregion

    #region Test Helper Interfaces and Classes

    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
    private interface IAnotherTestService { }
    private class AnotherTestServiceImpl : IAnotherTestService { }

    #endregion
}
