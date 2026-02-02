using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.ApplicationServices.Validation;
using RCommon.FluentValidation;
using Xunit;

namespace RCommon.FluentValidation.Tests;

public class FluentValidationBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidRCommonBuilder_InitializesServicesProperty()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_RegistersFluentValidationProviderAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validationProvider = scope.ServiceProvider.GetService<IValidationProvider>();
        validationProvider.Should().NotBeNull();
        validationProvider.Should().BeOfType<FluentValidationProvider>();
    }

    [Fact]
    public void Constructor_RegistersValidationProviderAsScopedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var validationProvider1 = scope1.ServiceProvider.GetService<IValidationProvider>();
        var validationProvider2 = scope2.ServiceProvider.GetService<IValidationProvider>();

        validationProvider1.Should().NotBeNull();
        validationProvider2.Should().NotBeNull();
        validationProvider1.Should().NotBeSameAs(validationProvider2);
    }

    [Fact]
    public void Constructor_WithNullBuilder_ThrowsNullReferenceException()
    {
        // Arrange & Act
        var act = () => new FluentValidationBuilder(null!);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    #endregion

    #region Services Property Tests

    [Fact]
    public void Services_ReturnsServiceCollectionFromBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        var result = builder.Services;

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void Services_CanBeUsedToAddAdditionalServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        builder.Services.AddSingleton<ITestService, TestService>();
        var provider = services.BuildServiceProvider();

        // Assert
        var testService = provider.GetService<ITestService>();
        testService.Should().NotBeNull();
    }

    #endregion

    #region IFluentValidationBuilder Implementation Tests

    [Fact]
    public void Builder_ImplementsIFluentValidationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IFluentValidationBuilder>();
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
