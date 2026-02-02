using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.ApplicationServices.Validation;
using RCommon.FluentValidation;
using Xunit;

using RCommonValidationException = RCommon.ApplicationServices.Validation.ValidationException;

namespace RCommon.FluentValidation.Tests;

public class FluentValidationProviderTests
{
    private readonly Mock<ILogger<FluentValidationProvider>> _mockLogger;

    public FluentValidationProviderTests()
    {
        _mockLogger = new Mock<ILogger<FluentValidationProvider>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsNullReferenceExceptionOnValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(null!, serviceProvider);

        // Act
        var act = async () => await provider.ValidateAsync(new TestCommand { Name = "" }, false);

        // Assert - Exception thrown when validation fails and tries to log
        act.Should().ThrowAsync<NullReferenceException>();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsNullReferenceExceptionOnValidation()
    {
        // Arrange
        var provider = new FluentValidationProvider(_mockLogger.Object, null!);

        // Act
        var act = async () => await provider.ValidateAsync(new TestCommand { Name = "Test" }, false);

        // Assert
        act.Should().ThrowAsync<NullReferenceException>();
    }

    #endregion

    #region ValidateAsync Tests - No Validators Registered

    [Fact]
    public async Task ValidateAsync_WithNoValidatorsRegistered_ReturnsValidOutcome()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "Test" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithNoValidatorsRegisteredAndThrowOnFaults_ReturnsValidOutcome()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "Test" };

        // Act
        var result = await provider.ValidateAsync(target, true);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateAsync Tests - With Validators

    [Fact]
    public async Task ValidateAsync_WithValidTarget_ReturnsValidOutcome()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "ValidName", Email = "test@example.com" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidTarget_ReturnsInvalidOutcome()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "", Email = "invalid-email" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidTargetAndThrowOnFaults_ThrowsValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "", Email = "invalid-email" };

        // Act
        var act = async () => await provider.ValidateAsync(target, true);

        // Assert
        await act.Should().ThrowAsync<RCommonValidationException>();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidTarget_ReturnsCorrectErrorCount()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "", Email = "invalid-email" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        result.Errors.Should().HaveCount(2); // Name empty and email invalid
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidTarget_ReturnsCorrectPropertyNames()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "", Email = "invalid-email" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        result.Errors.Select(e => e.PropertyName).Should().Contain("Name");
        result.Errors.Select(e => e.PropertyName).Should().Contain("Email");
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidTarget_ReturnsAttemptedValues()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "", Email = "invalid-email" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        var nameError = result.Errors.First(e => e.PropertyName == "Name");
        nameError.AttemptedValue.Should().Be("");

        var emailError = result.Errors.First(e => e.PropertyName == "Email");
        emailError.AttemptedValue.Should().Be("invalid-email");
    }

    #endregion

    #region ValidateAsync Tests - Multiple Validators

    [Fact]
    public async Task ValidateAsync_WithMultipleValidators_RunsAllValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        services.AddScoped<IValidator<TestCommand>, AdditionalTestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "Test", Email = "test@example.com", Description = "" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().Contain("Description");
    }

    [Fact]
    public async Task ValidateAsync_WithMultipleValidatorsAndValidTarget_ReturnsValidOutcome()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        services.AddScoped<IValidator<TestCommand>, AdditionalTestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "Test", Email = "test@example.com", Description = "Valid Description" };

        // Act
        var result = await provider.ValidateAsync(target, false);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region ValidateAsync Tests - CancellationToken

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_PassesTokenToValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "Test", Email = "test@example.com" };
        var cancellationToken = new CancellationToken();

        // Act
        var result = await provider.ValidateAsync(target, false, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "Test", Email = "test@example.com" };
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var act = async () => await provider.ValidateAsync(target, false, cancellationTokenSource.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region ValidateAsync Tests - Scoped Service Provider

    [Fact]
    public async Task ValidateAsync_CreatesNewScopeForEachValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
        var serviceProvider = services.BuildServiceProvider();
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);
        var target = new TestCommand { Name = "Test", Email = "test@example.com" };

        // Act
        var result1 = await provider.ValidateAsync(target, false);
        var result2 = await provider.ValidateAsync(target, false);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
    }

    #endregion

    #region IValidationProvider Implementation Tests

    [Fact]
    public void Provider_ImplementsIValidationProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var provider = new FluentValidationProvider(_mockLogger.Object, serviceProvider);

        // Assert
        provider.Should().BeAssignableTo<IValidationProvider>();
    }

    #endregion

    #region Test Helper Classes

    public class TestCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Email).EmailAddress().WithMessage("Email must be valid");
        }
    }

    public class AdditionalTestCommandValidator : AbstractValidator<TestCommand>
    {
        public AdditionalTestCommandValidator()
        {
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required");
        }
    }

    #endregion
}
