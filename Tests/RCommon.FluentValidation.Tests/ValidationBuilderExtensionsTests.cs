using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Validation;
using RCommon.FluentValidation;
using Xunit;

namespace RCommon.FluentValidation.Tests;

/// <summary>
/// Tests for ValidationBuilderExtensions integration with FluentValidation.
/// Note: The WithValidation extension methods exist in both RCommon.ApplicationServices
/// and RCommon.FluentValidation assemblies with the same namespace. These tests focus on
/// verifying the FluentValidationBuilder integration rather than the ambiguous extension methods.
/// </summary>
public class ValidationBuilderExtensionsTests
{
    #region FluentValidationBuilder Integration Tests

    [Fact]
    public void FluentValidationBuilder_RegistersValidationProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act - Direct construction mirrors what WithValidation does
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validationProvider = scope.ServiceProvider.GetService<IValidationProvider>();
        validationProvider.Should().NotBeNull();
        validationProvider.Should().BeOfType<FluentValidationProvider>();
    }

    [Fact]
    public void FluentValidationBuilder_CreatedViaActivator_RegistersValidationProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act - Simulate what WithValidation does with Activator.CreateInstance
        var builder = (FluentValidationBuilder)Activator.CreateInstance(
            typeof(FluentValidationBuilder),
            new object[] { rcommonBuilder })!;
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validationProvider = scope.ServiceProvider.GetService<IValidationProvider>();
        validationProvider.Should().NotBeNull();
        validationProvider.Should().BeOfType<FluentValidationProvider>();
    }

    #endregion

    #region CqrsValidationOptions Tests

    [Fact]
    public void CqrsValidationOptions_ConfiguredViaServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Configure options the way WithValidation does
        services.Configure<CqrsValidationOptions>(options =>
        {
            options.ValidateQueries = true;
            options.ValidateCommands = true;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<CqrsValidationOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.ValidateQueries.Should().BeTrue();
        options!.Value.ValidateCommands.Should().BeTrue();
    }

    [Fact]
    public void CqrsValidationOptions_DefaultConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Configure with empty action (default behavior)
        services.Configure<CqrsValidationOptions>(_ => { });
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<CqrsValidationOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.ValidateQueries.Should().BeFalse();
        options!.Value.ValidateCommands.Should().BeFalse();
    }

    [Fact]
    public void CqrsValidationOptions_PartialConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.Configure<CqrsValidationOptions>(options =>
        {
            options.ValidateCommands = true;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<CqrsValidationOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.ValidateCommands.Should().BeTrue();
        options!.Value.ValidateQueries.Should().BeFalse();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FluentValidationBuilder_CanBeChainedWithOtherServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act - Setup FluentValidation and other services
        var fluentValidationBuilder = new FluentValidationBuilder(rcommonBuilder);
        services.Configure<CqrsValidationOptions>(options =>
        {
            options.ValidateCommands = true;
        });
        rcommonBuilder.WithSimpleGuidGenerator();
        var result = rcommonBuilder.Configure();

        // Assert
        result.Should().BeSameAs(services);
        var provider = services.BuildServiceProvider();
        provider.GetService<IValidationProvider>().Should().NotBeNull();
        provider.GetService<IGuidGenerator>().Should().NotBeNull();
    }

    [Fact]
    public void ValidationProvider_RegisteredAsScoped()
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

    #endregion

    #region IValidationBuilder Interface Tests

    [Fact]
    public void FluentValidationBuilder_ImplementsIValidationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Should().BeAssignableTo<IValidationBuilder>();
    }

    [Fact]
    public void IValidationBuilder_ServicesProperty_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act
        IValidationBuilder builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    #endregion
}
