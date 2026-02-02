using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.EventHandling;
using Xunit;

namespace RCommon.Wolverine.Tests;

public class WolverineEventHandlingBuilderTests
{
    private readonly Mock<IRCommonBuilder> _mockRCommonBuilder;
    private readonly IServiceCollection _services;

    public WolverineEventHandlingBuilderTests()
    {
        _services = new ServiceCollection();
        _mockRCommonBuilder = new Mock<IRCommonBuilder>();
        _mockRCommonBuilder.Setup(x => x.Services).Returns(_services);
    }

    [Fact]
    public void Constructor_WithValidBuilder_SetsServices()
    {
        // Act
        var builder = new WolverineEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(_services);
    }

    [Fact]
    public void Constructor_AccessesBuilderServices()
    {
        // Act
        var builder = new WolverineEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        _mockRCommonBuilder.Verify(x => x.Services, Times.AtLeastOnce);
    }

    [Fact]
    public void Builder_ImplementsIWolverineEventHandlingBuilder()
    {
        // Act
        var builder = new WolverineEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IWolverineEventHandlingBuilder>();
    }

    [Fact]
    public void Services_ReturnsServiceCollection()
    {
        // Arrange
        var builder = new WolverineEventHandlingBuilder(_mockRCommonBuilder.Object);

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
        var action = () => new WolverineEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void Services_CanAddRegistrations()
    {
        // Arrange
        var builder = new WolverineEventHandlingBuilder(_mockRCommonBuilder.Object);

        // Act
        builder.Services.AddSingleton<object>(new object());

        // Assert
        builder.Services.Should().HaveCount(1);
    }
}
