using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.ApplicationServices.Validation;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class ValidationServiceTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IValidationProvider> _mockValidationProvider;

    public ValidationServiceTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockValidationProvider = new Mock<IValidationProvider>();

        // Setup the service scope factory chain
        var scopedServiceProvider = new Mock<IServiceProvider>();
        scopedServiceProvider
            .Setup(x => x.GetService(typeof(IValidationProvider)))
            .Returns(_mockValidationProvider.Object);

        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(scopedServiceProvider.Object);
        _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);
    }

    private ValidationService CreateValidationService()
    {
        return new ValidationService(_mockServiceProvider.Object);
    }

    [Fact]
    public async Task ValidateAsync_WithValidTarget_ReturnsValidOutcome()
    {
        // Arrange
        var target = new TestValidationTarget { Name = "Test" };
        var expectedOutcome = new ValidationOutcome();

        _mockValidationProvider
            .Setup(x => x.ValidateAsync(target, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutcome);

        var validationService = CreateValidationService();

        // Act
        var result = await validationService.ValidateAsync(target);

        // Assert
        result.Should().BeSameAs(expectedOutcome);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidTarget_ReturnsInvalidOutcome()
    {
        // Arrange
        var target = new TestValidationTarget { Name = "" };
        var validationFault = new ValidationFault("Name", "Name is required");
        var expectedOutcome = new ValidationOutcome(new[] { validationFault });

        _mockValidationProvider
            .Setup(x => x.ValidateAsync(target, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutcome);

        var validationService = CreateValidationService();

        // Act
        var result = await validationService.ValidateAsync(target);

        // Assert
        result.Should().BeSameAs(expectedOutcome);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public async Task ValidateAsync_WithThrowOnFaultsTrue_PassesParameterToProvider()
    {
        // Arrange
        var target = new TestValidationTarget { Name = "Test" };
        var expectedOutcome = new ValidationOutcome();

        _mockValidationProvider
            .Setup(x => x.ValidateAsync(target, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutcome);

        var validationService = CreateValidationService();

        // Act
        await validationService.ValidateAsync(target, throwOnFaults: true);

        // Assert
        _mockValidationProvider.Verify(
            x => x.ValidateAsync(target, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WithThrowOnFaultsFalse_PassesParameterToProvider()
    {
        // Arrange
        var target = new TestValidationTarget { Name = "Test" };
        var expectedOutcome = new ValidationOutcome();

        _mockValidationProvider
            .Setup(x => x.ValidateAsync(target, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutcome);

        var validationService = CreateValidationService();

        // Act
        await validationService.ValidateAsync(target, throwOnFaults: false);

        // Assert
        _mockValidationProvider.Verify(
            x => x.ValidateAsync(target, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_PassesTokenToProvider()
    {
        // Arrange
        var target = new TestValidationTarget { Name = "Test" };
        var cancellationToken = new CancellationToken();
        var expectedOutcome = new ValidationOutcome();

        _mockValidationProvider
            .Setup(x => x.ValidateAsync(target, false, cancellationToken))
            .ReturnsAsync(expectedOutcome);

        var validationService = CreateValidationService();

        // Act
        await validationService.ValidateAsync(target, false, cancellationToken);

        // Assert
        _mockValidationProvider.Verify(
            x => x.ValidateAsync(target, false, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_CreatesNewScope_DisposesScope()
    {
        // Arrange
        var target = new TestValidationTarget { Name = "Test" };
        var expectedOutcome = new ValidationOutcome();

        _mockValidationProvider
            .Setup(x => x.ValidateAsync(target, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutcome);

        var validationService = CreateValidationService();

        // Act
        await validationService.ValidateAsync(target);

        // Assert
        _mockServiceScopeFactory.Verify(x => x.CreateScope(), Times.Once);
        _mockServiceScope.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WhenProviderIsNull_ThrowsException()
    {
        // Arrange
        var target = new TestValidationTarget { Name = "Test" };

        var scopedServiceProvider = new Mock<IServiceProvider>();
        scopedServiceProvider
            .Setup(x => x.GetService(typeof(IValidationProvider)))
            .Returns((IValidationProvider)null!);

        _mockServiceScope.Setup(x => x.ServiceProvider).Returns(scopedServiceProvider.Object);

        var validationService = CreateValidationService();

        // Act
        var act = async () => await validationService.ValidateAsync(target);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }
}

// Test class for validation
public class TestValidationTarget
{
    public string Name { get; set; } = string.Empty;
}
