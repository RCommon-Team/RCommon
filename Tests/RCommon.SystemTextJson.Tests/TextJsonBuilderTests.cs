using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Json;
using RCommon.SystemTextJson;
using Xunit;

namespace RCommon.SystemTextJson.Tests;

public class TextJsonBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidBuilder_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_SetsServicesProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_RegistersTextJsonSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IJsonSerializer) &&
            sd.ImplementationType == typeof(TextJsonSerializer));
    }

    [Fact]
    public void Constructor_RegistersTextJsonSerializerAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        var descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IJsonSerializer));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    #endregion

    #region Services Property Tests

    [Fact]
    public void Services_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Act
        var result = builder.Services;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<ServiceCollection>();
    }

    [Fact]
    public void Services_ReturnsSameInstanceAsProvidedBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Act
        var result = builder.Services;

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion

    #region ITextJsonBuilder Interface Tests

    [Fact]
    public void TextJsonBuilder_ImplementsITextJsonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<ITextJsonBuilder>();
    }

    [Fact]
    public void TextJsonBuilder_ImplementsIJsonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IJsonBuilder>();
    }

    #endregion

    #region Service Resolution Tests

    [Fact]
    public void ResolvedSerializer_IsTextJsonSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<System.Text.Json.JsonSerializerOptions>(options => { });

        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockBuilder.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var serializer = serviceProvider.GetService<IJsonSerializer>();

        // Assert
        serializer.Should().NotBeNull();
        serializer.Should().BeOfType<TextJsonSerializer>();
    }

    [Fact]
    public void MultipleResolutions_CreateNewInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<System.Text.Json.JsonSerializerOptions>(options => { });

        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);
        var builder = new TextJsonBuilder(mockBuilder.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var serializer1 = serviceProvider.GetService<IJsonSerializer>();
        var serializer2 = serviceProvider.GetService<IJsonSerializer>();

        // Assert
        serializer1.Should().NotBeNull();
        serializer2.Should().NotBeNull();
        serializer1.Should().NotBeSameAs(serializer2);
    }

    #endregion

    #region Registration Behavior Tests

    [Fact]
    public void Constructor_DoesNotRemoveExistingServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITestService, TestServiceImplementation>();
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    [Fact]
    public void Constructor_AddsToExistingRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;
        var mockBuilder = new Mock<IRCommonBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new TextJsonBuilder(mockBuilder.Object);

        // Assert
        services.Count.Should().BeGreaterThan(initialCount);
    }

    #endregion

    #region Test Helper Interfaces and Classes

    private interface ITestService { }
    private class TestServiceImplementation : ITestService { }

    #endregion
}
