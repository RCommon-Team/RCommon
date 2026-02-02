using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.ApplicationServices;
using RCommon.FluentValidation;
using Xunit;

namespace RCommon.FluentValidation.Tests;

public class IFluentValidationBuilderTests
{
    #region Interface Contract Tests

    [Fact]
    public void IFluentValidationBuilder_ExtendsIValidationBuilder()
    {
        // Arrange & Act
        var interfaceType = typeof(IFluentValidationBuilder);

        // Assert
        interfaceType.Should().BeAssignableTo<IValidationBuilder>();
    }

    [Fact]
    public void IFluentValidationBuilder_HasServicesProperty()
    {
        // Arrange
        var interfaceType = typeof(IFluentValidationBuilder);

        // Act
        var servicesProperty = interfaceType.GetProperty("Services");

        // Assert
        servicesProperty.Should().NotBeNull();
        servicesProperty!.PropertyType.Should().Be(typeof(IServiceCollection));
    }

    #endregion

    #region Mock Implementation Tests

    [Fact]
    public void MockIFluentValidationBuilder_CanReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IFluentValidationBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act
        var result = mockBuilder.Object.Services;

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void MockIFluentValidationBuilder_CanBeUsedWithExtensionMethods()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IFluentValidationBuilder>();
        mockBuilder.Setup(b => b.Services).Returns(services);

        // Act - Using the AddValidator extension method
        mockBuilder.Object.AddValidator<TestCommand, TestCommandValidator>();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<IValidator<TestCommand>>();
        validator.Should().NotBeNull();
    }

    #endregion

    #region FluentValidationBuilder Implementation Tests

    [Fact]
    public void FluentValidationBuilder_ImplementsIFluentValidationBuilder()
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
    public void FluentValidationBuilder_ServicesPropertyMatchesInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);

        // Act
        IFluentValidationBuilder builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    #endregion

    #region Test Helper Classes

    public class TestCommand
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    #endregion
}
