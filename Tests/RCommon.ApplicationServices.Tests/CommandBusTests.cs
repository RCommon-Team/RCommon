using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.Validation;
using RCommon.Caching;
using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

public class CommandBusTests
{
    private readonly Mock<ILogger<CommandBus>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IValidationService> _mockValidationService;
    private readonly Mock<IOptions<CqrsValidationOptions>> _mockValidationOptions;
    private readonly Mock<IOptions<CachingOptions>> _mockCachingOptions;

    public CommandBusTests()
    {
        _mockLogger = new Mock<ILogger<CommandBus>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockValidationService = new Mock<IValidationService>();
        _mockValidationOptions = new Mock<IOptions<CqrsValidationOptions>>();
        _mockCachingOptions = new Mock<IOptions<CachingOptions>>();

        // Default setup
        _mockValidationOptions.Setup(x => x.Value).Returns(new CqrsValidationOptions());
        _mockCachingOptions.Setup(x => x.Value).Returns(new CachingOptions { CachingEnabled = false });
    }

    private CommandBus CreateCommandBus()
    {
        return new CommandBus(
            _mockLogger.Object,
            _mockServiceProvider.Object,
            _mockValidationService.Object,
            _mockValidationOptions.Object,
            _mockCachingOptions.Object);
    }

    [Fact]
    public async Task DispatchCommandAsync_WithNullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        var commandBus = CreateCommandBus();

        // Act
        var act = async () => await commandBus.DispatchCommandAsync<SuccessExecutionResult>(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("command");
    }

    [Fact]
    public async Task DispatchCommandAsync_WithNoHandlerRegistered_ThrowsNoCommandHandlersException()
    {
        // Arrange
        var commandBus = CreateCommandBus();
        var command = new TestCommand();
        var handlerType = typeof(ICommandHandler<SuccessExecutionResult, TestCommand>);

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((IEnumerable<ICommandHandler>)new List<ICommandHandler>());

        // Act
        var act = async () => await commandBus.DispatchCommandAsync(command);

        // Assert
        await act.Should().ThrowAsync<NoCommandHandlersException>();
    }

    [Fact]
    public async Task DispatchCommandAsync_WithMultipleHandlers_ThrowsInvalidOperationException()
    {
        // Arrange
        var commandBus = CreateCommandBus();
        var command = new TestCommand();
        var handler1 = new Mock<ICommandHandler<SuccessExecutionResult, TestCommand>>();
        var handler2 = new Mock<ICommandHandler<SuccessExecutionResult, TestCommand>>();
        var handlers = new List<ICommandHandler> { handler1.Object, handler2.Object };

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(handlers);

        // Act
        var act = async () => await commandBus.DispatchCommandAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DispatchCommandAsync_WithValidationEnabled_ValidatesCommand()
    {
        // Arrange
        var validationOptions = new CqrsValidationOptions { ValidateCommands = true };
        _mockValidationOptions.Setup(x => x.Value).Returns(validationOptions);

        var command = new TestCommand();
        var mockHandler = new Mock<ICommandHandler<SuccessExecutionResult, TestCommand>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessExecutionResult());

        var handlers = new List<ICommandHandler> { mockHandler.Object };

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(handlers);

        _mockValidationService
            .Setup(x => x.ValidateAsync(It.IsAny<ICommand<SuccessExecutionResult>>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationOutcome());

        var commandBus = CreateCommandBus();

        // Act
        await commandBus.DispatchCommandAsync(command);

        // Assert
        _mockValidationService.Verify(
            x => x.ValidateAsync(It.IsAny<ICommand<SuccessExecutionResult>>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DispatchCommandAsync_WithValidationDisabled_DoesNotValidateCommand()
    {
        // Arrange
        var validationOptions = new CqrsValidationOptions { ValidateCommands = false };
        _mockValidationOptions.Setup(x => x.Value).Returns(validationOptions);

        var command = new TestCommand();
        var mockHandler = new Mock<ICommandHandler<SuccessExecutionResult, TestCommand>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessExecutionResult());

        var handlers = new List<ICommandHandler> { mockHandler.Object };

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(handlers);

        var commandBus = CreateCommandBus();

        // Act
        await commandBus.DispatchCommandAsync(command);

        // Assert
        _mockValidationService.Verify(
            x => x.ValidateAsync(It.IsAny<ICommand<SuccessExecutionResult>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DispatchCommandAsync_WithSingleHandler_ExecutesHandler()
    {
        // Arrange
        var command = new TestCommand();
        var expectedResult = new SuccessExecutionResult();

        var mockHandler = new Mock<ICommandHandler<SuccessExecutionResult, TestCommand>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handlers = new List<ICommandHandler> { mockHandler.Object };

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(handlers);

        var commandBus = CreateCommandBus();

        // Act
        var result = await commandBus.DispatchCommandAsync(command);

        // Assert
        result.Should().BeSameAs(expectedResult);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DispatchCommandAsync_WithCancellationToken_PassesTokenToHandler()
    {
        // Arrange
        var command = new TestCommand();
        var cancellationToken = new CancellationToken();

        var mockHandler = new Mock<ICommandHandler<SuccessExecutionResult, TestCommand>>();
        mockHandler
            .Setup(h => h.HandleAsync(It.IsAny<TestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessExecutionResult());

        var handlers = new List<ICommandHandler> { mockHandler.Object };

        _mockServiceProvider
            .Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns(handlers);

        var commandBus = CreateCommandBus();

        // Act
        await commandBus.DispatchCommandAsync(command, cancellationToken);

        // Assert
        mockHandler.Verify(
            h => h.HandleAsync(command, cancellationToken),
            Times.Once);
    }
}

// Test command class for testing
public class TestCommand : ICommand<SuccessExecutionResult>
{
}
