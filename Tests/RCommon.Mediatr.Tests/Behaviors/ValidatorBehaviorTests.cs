using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.ApplicationServices.Validation;
using RCommon.Mediator.MediatR.Behaviors;
using RCommon.Mediator.Subscribers;
using Xunit;

namespace RCommon.Mediatr.Tests.Behaviors;

public class ValidatorBehaviorTests
{
    #region ValidatorBehavior Constructor Tests

    [Fact]
    public void ValidatorBehavior_Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<ValidatorBehavior<TestAppRequest, TestResponse>>>();

        // Act
        var behavior = new ValidatorBehavior<TestAppRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void ValidatorBehavior_Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();

        // Act
        Action act = () => new ValidatorBehavior<TestAppRequest, TestResponse>(
            mockValidationService.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidatorBehavior_ImplementsIPipelineBehavior()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<ValidatorBehavior<TestAppRequest, TestResponse>>>();

        // Act
        var behavior = new ValidatorBehavior<TestAppRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().BeAssignableTo<IPipelineBehavior<TestAppRequest, TestResponse>>();
    }

    #endregion

    #region ValidatorBehavior Handle Tests

    [Fact]
    public async Task ValidatorBehavior_Handle_CallsValidationService()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<TestAppRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var mockLogger = new Mock<ILogger<ValidatorBehavior<TestAppRequest, TestResponse>>>();
        var behavior = new ValidatorBehavior<TestAppRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);
        var request = new TestAppRequest { Data = "Test" };

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockValidationService.Verify(
            x => x.ValidateAsync<TestAppRequest>(request, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidatorBehavior_Handle_CallsNextAfterValidation()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<TestAppRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var mockLogger = new Mock<ILogger<ValidatorBehavior<TestAppRequest, TestResponse>>>();
        var behavior = new ValidatorBehavior<TestAppRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);
        var request = new TestAppRequest { Data = "Test" };
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatorBehavior_Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<TestAppRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var mockLogger = new Mock<ILogger<ValidatorBehavior<TestAppRequest, TestResponse>>>();
        var behavior = new ValidatorBehavior<TestAppRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);
        var request = new TestAppRequest { Data = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    [Fact]
    public async Task ValidatorBehavior_Handle_LogsValidationInfo()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<TestAppRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var mockLogger = new Mock<ILogger<ValidatorBehavior<TestAppRequest, TestResponse>>>();
        var behavior = new ValidatorBehavior<TestAppRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);
        var request = new TestAppRequest { Data = "Test" };

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validating")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region ValidatorBehaviorForMediatR Constructor Tests

    [Fact]
    public void ValidatorBehaviorForMediatR_Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>>>();

        // Act
        var behavior = new ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void ValidatorBehaviorForMediatR_Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();

        // Act
        Action act = () => new ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>(
            mockValidationService.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidatorBehaviorForMediatR_ImplementsIPipelineBehavior()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        var mockLogger = new Mock<ILogger<ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>>>();

        // Act
        var behavior = new ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().BeAssignableTo<IPipelineBehavior<TestMediatRRequest, TestResponse>>();
    }

    #endregion

    #region ValidatorBehaviorForMediatR Handle Tests

    [Fact]
    public async Task ValidatorBehaviorForMediatR_Handle_CallsValidationService()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<TestMediatRRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var mockLogger = new Mock<ILogger<ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>>>();
        var behavior = new ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);
        var request = new TestMediatRRequest { Data = "Test" };

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(new TestResponse());

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockValidationService.Verify(
            x => x.ValidateAsync<TestMediatRRequest>(request, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidatorBehaviorForMediatR_Handle_CallsNextAfterValidation()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<TestMediatRRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var mockLogger = new Mock<ILogger<ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>>>();
        var behavior = new ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);
        var request = new TestMediatRRequest { Data = "Test" };
        var nextCalled = false;

        RequestHandlerDelegate<TestResponse> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse());
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatorBehaviorForMediatR_Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var mockValidationService = new Mock<IValidationService>();
        mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<TestMediatRRequest>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var mockLogger = new Mock<ILogger<ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>>>();
        var behavior = new ValidatorBehaviorForMediatR<TestMediatRRequest, TestResponse>(
            mockValidationService.Object,
            mockLogger.Object);
        var request = new TestMediatRRequest { Data = "Test" };
        var expectedResponse = new TestResponse { Result = "Success" };

        RequestHandlerDelegate<TestResponse> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
    }

    #endregion

    #region Test Helper Classes

    public class TestAppRequest : IAppRequest<TestResponse>
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestMediatRRequest : IRequest<TestResponse>
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    #endregion
}
