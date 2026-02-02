using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon;
using RCommon.EventHandling;
using RCommon.Mediator;
using RCommon.MediatR;
using Xunit;

namespace RCommon.Mediatr.Tests;

public class MediatREventHandlingBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidRCommonBuilder_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ImplementsIMediatREventHandlingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IMediatREventHandlingBuilder>();
    }

    [Fact]
    public void Constructor_ImplementsIEventHandlingBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IEventHandlingBuilder>();
    }

    [Fact]
    public void Constructor_RegistersIMediatorAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMediatorAdapter) &&
            sd.ImplementationType == typeof(MediatRAdapter) &&
            sd.Lifetime == ServiceLifetime.Scoped);
    }

    #endregion

    #region Services Property Tests

    [Fact]
    public void Services_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Services_ContainsRegisteredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().NotBeEmpty();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Constructor_AllowsAdditionalServiceRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);
        builder.Services.AddSingleton<ITestService, TestServiceImpl>();

        // Assert
        builder.Services.Should().Contain(sd =>
            sd.ServiceType == typeof(ITestService) &&
            sd.ImplementationType == typeof(TestServiceImpl));
    }

    [Fact]
    public void MultipleBuilderInstances_ShareSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder1 = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);
        var builder2 = new MediatREventHandlingBuilder(mockRCommonBuilder.Object);

        // Assert
        builder1.Services.Should().BeSameAs(builder2.Services);
    }

    #endregion

    #region Test Helper Classes

    public interface ITestService
    {
    }

    public class TestServiceImpl : ITestService
    {
    }

    #endregion
}
