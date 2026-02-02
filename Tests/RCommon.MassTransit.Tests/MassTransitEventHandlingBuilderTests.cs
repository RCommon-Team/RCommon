using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling;
using Xunit;

namespace RCommon.MassTransit.Tests;

public class MassTransitEventHandlingBuilderTests
{
    private readonly Mock<IRCommonBuilder> _mockRCommonBuilder;
    private readonly IServiceCollection _services;

    public MassTransitEventHandlingBuilderTests()
    {
        _services = new ServiceCollection();
        _mockRCommonBuilder = new Mock<IRCommonBuilder>();
        _mockRCommonBuilder.Setup(x => x.Services).Returns(_services);
    }

    [Fact]
    public void Constructor_WithValidBuilder_SetsServices()
    {
        // Act
        var builder = new MassTransitEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(_services);
    }

    [Fact]
    public void Constructor_AccessesBuilderServices()
    {
        // Act
        var builder = new MassTransitEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        _mockRCommonBuilder.Verify(x => x.Services, Times.AtLeastOnce);
    }

    [Fact]
    public void Builder_ImplementsIMassTransitEventHandlingBuilder()
    {
        // Act
        var builder = new MassTransitEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IMassTransitEventHandlingBuilder>();
    }

    [Fact]
    public void Services_ReturnsServiceCollection()
    {
        // Arrange
        var builder = new MassTransitEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Act & Assert
        builder.Services.Should().NotBeNull();
        builder.Services.Should().BeAssignableTo<IServiceCollection>();
    }

    [Fact]
    public void Constructor_WithEmptyServiceCollection_Succeeds()
    {
        // Arrange
        var emptyServices = new ServiceCollection();
        _mockRCommonBuilder.Setup(x => x.Services).Returns(emptyServices);

        // Act
        var action = () => new MassTransitEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Services_CanAddRegistrations()
    {
        // Arrange
        var builder = new MassTransitEventHandlingBuilder(_mockRCommonBuilder.Object);
        var initialCount = builder.Services.Count;

        // Act
        builder.Services.AddSingleton<ITestService, TestService>();

        // Assert
        builder.Services.Should().HaveCount(initialCount + 1);
        builder.Services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    // Test helper types
    private interface ITestService { }
    private class TestService : ITestService { }
}
