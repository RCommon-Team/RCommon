using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.ApplicationServices;
using RCommon.FluentValidation;
using Xunit;

namespace RCommon.FluentValidation.Tests;

public class FluentValidationBuilderExtensionsTests
{
    #region AddValidator Tests

    [Fact]
    public void AddValidator_RegistersValidatorInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        builder.AddValidator<TestCommand, TestCommandValidator>();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<IValidator<TestCommand>>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<TestCommandValidator>();
    }

    [Fact]
    public void AddValidator_RegistersValidatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        builder.AddValidator<TestCommand, TestCommandValidator>();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var validator1 = scope1.ServiceProvider.GetService<IValidator<TestCommand>>();
        var validator2 = scope2.ServiceProvider.GetService<IValidator<TestCommand>>();

        validator1.Should().NotBeNull();
        validator2.Should().NotBeNull();
        validator1.Should().NotBeSameAs(validator2);
    }

    [Fact]
    public void AddValidator_CanRegisterMultipleValidatorsForSameType()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        builder.AddValidator<TestCommand, TestCommandValidator>();
        builder.AddValidator<TestCommand, AdditionalTestCommandValidator>();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<TestCommand>>();
        validators.Should().HaveCount(2);
    }

    [Fact]
    public void AddValidator_CanRegisterValidatorsForDifferentTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        builder.AddValidator<TestCommand, TestCommandValidator>();
        builder.AddValidator<AnotherCommand, AnotherCommandValidator>();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var testCommandValidator = scope.ServiceProvider.GetService<IValidator<TestCommand>>();
        var anotherCommandValidator = scope.ServiceProvider.GetService<IValidator<AnotherCommand>>();

        testCommandValidator.Should().NotBeNull();
        anotherCommandValidator.Should().NotBeNull();
    }

    #endregion

    #region AddValidatorsFromAssembly Tests

    [Fact]
    public void AddValidatorsFromAssembly_RegistersValidatorsFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var assembly = typeof(FluentValidationBuilderExtensionsTests).Assembly;

        // Act
        builder.AddValidatorsFromAssembly(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<TestCommand>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void AddValidatorsFromAssembly_WithScopedLifetime_RegistersAsScopedByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var assembly = typeof(FluentValidationBuilderExtensionsTests).Assembly;

        // Act
        builder.AddValidatorsFromAssembly(assembly);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var validator1 = scope1.ServiceProvider.GetService<IValidator<TestCommand>>();
        var validator2 = scope2.ServiceProvider.GetService<IValidator<TestCommand>>();

        validator1.Should().NotBeSameAs(validator2);
    }

    [Fact]
    public void AddValidatorsFromAssembly_WithSingletonLifetime_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var assembly = typeof(FluentValidationBuilderExtensionsTests).Assembly;

        // Act
        builder.AddValidatorsFromAssembly(assembly, ServiceLifetime.Singleton);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var validator1 = scope1.ServiceProvider.GetService<IValidator<TestCommand>>();
        var validator2 = scope2.ServiceProvider.GetService<IValidator<TestCommand>>();

        validator1.Should().BeSameAs(validator2);
    }

    [Fact]
    public void AddValidatorsFromAssembly_WithTransientLifetime_RegistersAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var assembly = typeof(FluentValidationBuilderExtensionsTests).Assembly;

        // Act
        builder.AddValidatorsFromAssembly(assembly, ServiceLifetime.Transient);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validator1 = scope.ServiceProvider.GetService<IValidator<TestCommand>>();
        var validator2 = scope.ServiceProvider.GetService<IValidator<TestCommand>>();

        validator1.Should().NotBeSameAs(validator2);
    }

    [Fact]
    public void AddValidatorsFromAssembly_WithFilter_OnlyRegistersMatchingValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var assembly = typeof(FluentValidationBuilderExtensionsTests).Assembly;

        // Act
        builder.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped,
            result => result.ValidatorType.Name.Contains("TestCommand"));
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var testCommandValidator = scope.ServiceProvider.GetService<IValidator<TestCommand>>();
        testCommandValidator.Should().NotBeNull();
    }

    #endregion

    #region AddValidatorsFromAssemblies Tests

    [Fact]
    public void AddValidatorsFromAssemblies_RegistersValidatorsFromMultipleAssemblies()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var assemblies = new[] { typeof(FluentValidationBuilderExtensionsTests).Assembly };

        // Act
        builder.AddValidatorsFromAssemblies(assemblies);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validators = scope.ServiceProvider.GetServices<IValidator<TestCommand>>();
        validators.Should().NotBeEmpty();
    }

    [Fact]
    public void AddValidatorsFromAssemblies_WithEmptyAssemblies_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);
        var assemblies = Array.Empty<System.Reflection.Assembly>();

        // Act
        var act = () => builder.AddValidatorsFromAssemblies(assemblies);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region AddValidatorsFromAssemblyContaining Tests

    [Fact]
    public void AddValidatorsFromAssemblyContaining_RegistersValidatorsFromTypeAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        builder.AddValidatorsFromAssemblyContaining(typeof(TestCommandValidator));
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var validator = scope.ServiceProvider.GetService<IValidator<TestCommand>>();
        validator.Should().NotBeNull();
    }

    [Fact]
    public void AddValidatorsFromAssemblyContaining_WithLifetime_RegistersWithSpecifiedLifetime()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockRCommonBuilder = new Mock<IRCommonBuilder>();
        mockRCommonBuilder.Setup(b => b.Services).Returns(services);
        var builder = new FluentValidationBuilder(mockRCommonBuilder.Object);

        // Act
        builder.AddValidatorsFromAssemblyContaining(typeof(TestCommandValidator), ServiceLifetime.Singleton);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var validator1 = scope1.ServiceProvider.GetService<IValidator<TestCommand>>();
        var validator2 = scope2.ServiceProvider.GetService<IValidator<TestCommand>>();

        validator1.Should().BeSameAs(validator2);
    }

    #endregion

    #region Test Helper Classes

    public class TestCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AnotherCommand
    {
        public int Value { get; set; }
    }

    public class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Email).EmailAddress();
        }
    }

    public class AdditionalTestCommandValidator : AbstractValidator<TestCommand>
    {
        public AdditionalTestCommandValidator()
        {
            RuleFor(x => x.Name).MaximumLength(100);
        }
    }

    public class AnotherCommandValidator : AbstractValidator<AnotherCommand>
    {
        public AnotherCommandValidator()
        {
            RuleFor(x => x.Value).GreaterThan(0);
        }
    }

    #endregion
}
