using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Json;
using RCommon.JsonNet;
using Xunit;

namespace RCommon.JsonNet.Tests;

public class JsonNetBuilderTests
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
        var act = () => new JsonNetBuilder(mockRCommonBuilder.Object);

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
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_ImplementsIJsonNetBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IJsonNetBuilder>();
    }

    [Fact]
    public void Constructor_ImplementsIJsonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IJsonBuilder>();
    }

    [Fact]
    public void Constructor_RegistersIJsonSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IJsonSerializer));
    }

    [Fact]
    public void Constructor_RegistersJsonNetSerializerAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IJsonSerializer));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersJsonNetSerializerAsImplementation()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IJsonSerializer));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(JsonNetSerializer));
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
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().Contain(sd => sd.ServiceType == typeof(string));
    }

    [Fact]
    public void Services_AllowsAddingNewServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

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
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

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
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);
        var configurationCalled = false;

        // Act
        Action<JsonNetBuilder> configAction = b =>
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
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

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
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Builder_CanAddServicesToRCommonBuilderServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var rcommonBuilder = new RCommonBuilder(services);
        var builder = new JsonNetBuilder(rcommonBuilder);

        // Act
        builder.Services.AddSingleton<ITestService, TestServiceImpl>();
        var provider = services.BuildServiceProvider();
        var testService = provider.GetService<ITestService>();

        // Assert
        testService.Should().NotBeNull();
        testService.Should().BeOfType<TestServiceImpl>();
    }

    [Fact]
    public void Builder_RegistersJsonSerializerThatCanBeResolved()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act
        var builder = new JsonNetBuilder(rcommonBuilder);
        var provider = services.BuildServiceProvider();
        var jsonSerializer = provider.GetService<IJsonSerializer>();

        // Assert
        jsonSerializer.Should().NotBeNull();
        jsonSerializer.Should().BeOfType<JsonNetSerializer>();
    }

    #endregion

    #region Empty ServiceCollection Tests

    [Fact]
    public void Builder_WithEmptyServiceCollection_RegistersJsonSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        services.Should().ContainSingle(sd => sd.ServiceType == typeof(IJsonSerializer));
    }

    [Fact]
    public void Builder_ServiceCollectionContainsOnlyJsonSerializerRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert
        services.Should().HaveCount(1);
        services.First().ServiceType.Should().Be(typeof(IJsonSerializer));
    }

    #endregion

    #region Multiple Builder Creation Tests

    [Fact]
    public void MultipleBuilders_WithSameServiceCollection_RegisterMultipleJsonSerializers()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder1 = new JsonNetBuilder(mockRCommonBuilder.Object);
        var builder2 = new JsonNetBuilder(mockRCommonBuilder.Object);

        // Assert - Each builder registers a new service
        services.Count(sd => sd.ServiceType == typeof(IJsonSerializer)).Should().Be(2);
    }

    [Fact]
    public void MultipleBuilders_WithDifferentServiceCollections_AreIndependent()
    {
        // Arrange
        var services1 = new ServiceCollection();
        var services2 = new ServiceCollection();
        var mockRCommonBuilder1 = new Mock<IRCommonBuilder>();
        var mockRCommonBuilder2 = new Mock<IRCommonBuilder>();
        mockRCommonBuilder1.Setup(x => x.Services).Returns(services1);
        mockRCommonBuilder2.Setup(x => x.Services).Returns(services2);

        // Act
        var builder1 = new JsonNetBuilder(mockRCommonBuilder1.Object);
        var builder2 = new JsonNetBuilder(mockRCommonBuilder2.Object);
        builder1.Services.AddSingleton<ITestService, TestServiceImpl>();

        // Assert
        services1.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
        services2.Should().NotContain(sd => sd.ServiceType == typeof(ITestService));
    }

    #endregion

    #region Test Helper Interfaces and Classes

    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
    private interface IAnotherTestService { }
    private class AnotherTestServiceImpl : IAnotherTestService { }

    #endregion
}
