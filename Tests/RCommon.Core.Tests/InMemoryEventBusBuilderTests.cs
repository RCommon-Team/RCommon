using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling;
using Xunit;

namespace RCommon.Core.Tests;

public class InMemoryEventBusBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidBuilder_SetsServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var eventBusBuilder = new InMemoryEventBusBuilder(mockBuilder.Object);

        // Assert
        eventBusBuilder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_WithRCommonBuilder_ExtractsServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var rCommonBuilder = new RCommonBuilder(services);

        // Act
        var eventBusBuilder = new InMemoryEventBusBuilder(rCommonBuilder);

        // Assert
        eventBusBuilder.Services.Should().BeSameAs(services);
    }

    #endregion

    #region Services Property Tests

    [Fact]
    public void Services_ReturnsServiceCollectionFromBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);
        var eventBusBuilder = new InMemoryEventBusBuilder(mockBuilder.Object);

        // Act
        var result = eventBusBuilder.Services;

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void Services_CanBeUsedToRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);
        var eventBusBuilder = new InMemoryEventBusBuilder(mockBuilder.Object);

        // Act
        eventBusBuilder.Services.AddSingleton<ITestService, TestService>();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ITestService) &&
            sd.ImplementationType == typeof(TestService));
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void InMemoryEventBusBuilder_ImplementsIInMemoryEventBusBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var eventBusBuilder = new InMemoryEventBusBuilder(mockBuilder.Object);

        // Assert
        eventBusBuilder.Should().BeAssignableTo<IInMemoryEventBusBuilder>();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void InMemoryEventBusBuilder_CanBuildServiceProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var rCommonBuilder = new RCommonBuilder(services);
        var eventBusBuilder = new InMemoryEventBusBuilder(rCommonBuilder);

        // Act
        eventBusBuilder.Services.AddTransient<ITestService, TestService>();
        var serviceProvider = eventBusBuilder.Services.BuildServiceProvider();

        // Assert
        var service = serviceProvider.GetService<ITestService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<TestService>();
    }

    #endregion

    #region Test Helper Classes

    public interface ITestService
    {
        string GetValue();
    }

    public class TestService : ITestService
    {
        public string GetValue() => "TestValue";
    }

    #endregion
}
