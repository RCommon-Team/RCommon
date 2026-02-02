using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.Queries;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class CqrsBuilderTests
{
    [Fact]
    public void Constructor_WithBuilder_InitializesServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var cqrsBuilder = new CqrsBuilder(mockBuilder.Object);

        // Assert
        cqrsBuilder.Services.Should().NotBeNull();
        cqrsBuilder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_RegistersCommandBus()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var cqrsBuilder = new CqrsBuilder(mockBuilder.Object);

        // Assert
        var commandBusDescriptor = services.FirstOrDefault(
            s => s.ServiceType == typeof(ICommandBus));
        commandBusDescriptor.Should().NotBeNull();
        commandBusDescriptor!.ImplementationType.Should().Be(typeof(CommandBus));
        commandBusDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersQueryBus()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var cqrsBuilder = new CqrsBuilder(mockBuilder.Object);

        // Assert
        var queryBusDescriptor = services.FirstOrDefault(
            s => s.ServiceType == typeof(IQueryBus));
        queryBusDescriptor.Should().NotBeNull();
        queryBusDescriptor!.ImplementationType.Should().Be(typeof(QueryBus));
        queryBusDescriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void Services_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);
        var cqrsBuilder = new CqrsBuilder(mockBuilder.Object);

        // Act
        var result = cqrsBuilder.Services;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ServiceCollection>();
    }

    [Fact]
    public void Constructor_WithNullBuilder_ThrowsException()
    {
        // Arrange & Act
        var act = () => new CqrsBuilder(null!);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Constructor_ImplementsICqrsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var cqrsBuilder = new CqrsBuilder(mockBuilder.Object);

        // Assert
        cqrsBuilder.Should().BeAssignableTo<ICqrsBuilder>();
    }

    [Fact]
    public void Constructor_RegistersServicesAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var cqrsBuilder = new CqrsBuilder(mockBuilder.Object);

        // Assert
        var registeredServices = services.Where(s =>
            s.ServiceType == typeof(ICommandBus) ||
            s.ServiceType == typeof(IQueryBus));

        registeredServices.Should().HaveCount(2);
        registeredServices.All(s => s.Lifetime == ServiceLifetime.Transient).Should().BeTrue();
    }
}
