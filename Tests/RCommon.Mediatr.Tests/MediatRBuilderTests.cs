using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.MediatR;
using Xunit;

namespace RCommon.Mediatr.Tests;

public class MediatRBuilderTests
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
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ImplementsIMediatRBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IMediatRBuilder>();
    }

    [Fact]
    public void Constructor_ImplementsIMediatorBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IMediatorBuilder>();
    }

    [Fact]
    public void Constructor_RegistersIMediatorAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMediatorAdapter) &&
            sd.ImplementationType == typeof(MediatRAdapter) &&
            sd.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void Constructor_RegistersMediatR()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(IMediator));
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
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    #endregion

    #region Configure Tests

    [Fact]
    public void Configure_WithActionOptions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Configure(options =>
        {
            options.RegisterServicesFromAssembly(typeof(MediatRBuilderTests).Assembly);
        });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_WithActionOptions_CallsAddMediatR()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);
        var configurationCalled = false;

        // Act
        builder.Configure(options =>
        {
            configurationCalled = true;
            options.RegisterServicesFromAssembly(typeof(MediatRBuilderTests).Assembly);
        });

        // Assert
        configurationCalled.Should().BeTrue();
    }

    [Fact]
    public void Configure_WithConfigurationObject_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);
        var config = new MediatRServiceConfiguration();
        config.RegisterServicesFromAssembly(typeof(MediatRBuilderTests).Assembly);

        // Act
        var result = builder.Configure(config);

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Configure_AllowsFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);
        var config = new MediatRServiceConfiguration();
        config.RegisterServicesFromAssembly(typeof(MediatRBuilderTests).Assembly);

        // Act
        var result = builder
            .Configure(options => options.RegisterServicesFromAssembly(typeof(MediatRBuilderTests).Assembly))
            .Configure(config);

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion

    #region Service Resolution Tests

    [Fact]
    public void ServiceProvider_CanResolveIMediatorAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var adapter = serviceProvider.GetService<IMediatorAdapter>();

        // Assert
        adapter.Should().NotBeNull();
        adapter.Should().BeOfType<MediatRAdapter>();
    }

    [Fact]
    public void ServiceProvider_CanResolveIMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(x => x.Services).Returns(services);
        var builder = new MediatRBuilder(mockRCommonBuilder.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var mediator = serviceProvider.GetService<IMediator>();

        // Assert
        mediator.Should().NotBeNull();
    }

    #endregion
}
