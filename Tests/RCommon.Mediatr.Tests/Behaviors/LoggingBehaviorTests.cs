using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.Mediator.MediatR.Behaviors;
using Xunit;

namespace RCommon.Mediatr.Tests.Behaviors;

public class LoggingBehaviorTests
{
    #region LoggingRequestBehavior Constructor Tests

    [Fact]
    public void LoggingRequestBehavior_Constructor_WithValidLogger_CreatesInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestBehavior<TestCommand, Unit>>>();

        // Act
        var behavior = new LoggingRequestBehavior<TestCommand, Unit>(mockLogger.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void LoggingRequestBehavior_ImplementsIPipelineBehavior()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestBehavior<TestCommand, Unit>>>();

        // Act
        var behavior = new LoggingRequestBehavior<TestCommand, Unit>(mockLogger.Object);

        // Assert
        behavior.Should().BeAssignableTo<IPipelineBehavior<TestCommand, Unit>>();
    }

    #endregion

    #region LoggingRequestBehavior Handle Tests

    [Fact]
    public async Task LoggingRequestBehavior_Handle_CallsNext()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestBehavior<TestCommand, Unit>>>();
        var behavior = new LoggingRequestBehavior<TestCommand, Unit>(mockLogger.Object);
        var request = new TestCommand { Data = "Test" };
        var nextCalled = false;

        RequestHandlerDelegate<Unit> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Unit.Value);
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task LoggingRequestBehavior_Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestBehavior<TestCommand, Unit>>>();
        var behavior = new LoggingRequestBehavior<TestCommand, Unit>(mockLogger.Object);
        var request = new TestCommand { Data = "Test" };
        var expectedResponse = Unit.Value;

        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task LoggingRequestBehavior_Handle_LogsBeforeAndAfterExecution()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestBehavior<TestCommand, Unit>>>();
        var behavior = new LoggingRequestBehavior<TestCommand, Unit>(mockLogger.Object);
        var request = new TestCommand { Data = "Test" };

        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(Unit.Value);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling command")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("handled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region LoggingRequestWithResponseBehavior Constructor Tests

    [Fact]
    public void LoggingRequestWithResponseBehavior_Constructor_WithValidLogger_CreatesInstance()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestWithResponseBehavior<TestQuery, TestResult>>>();

        // Act
        var behavior = new LoggingRequestWithResponseBehavior<TestQuery, TestResult>(mockLogger.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void LoggingRequestWithResponseBehavior_ImplementsIPipelineBehavior()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestWithResponseBehavior<TestQuery, TestResult>>>();

        // Act
        var behavior = new LoggingRequestWithResponseBehavior<TestQuery, TestResult>(mockLogger.Object);

        // Assert
        behavior.Should().BeAssignableTo<IPipelineBehavior<TestQuery, TestResult>>();
    }

    #endregion

    #region LoggingRequestWithResponseBehavior Handle Tests

    [Fact]
    public async Task LoggingRequestWithResponseBehavior_Handle_CallsNext()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new LoggingRequestWithResponseBehavior<TestQuery, TestResult>(mockLogger.Object);
        var request = new TestQuery { Query = "Test" };
        var nextCalled = false;

        RequestHandlerDelegate<TestResult> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResult { Value = "Result" });
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task LoggingRequestWithResponseBehavior_Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new LoggingRequestWithResponseBehavior<TestQuery, TestResult>(mockLogger.Object);
        var request = new TestQuery { Query = "Test" };
        var expectedResponse = new TestResult { Value = "ExpectedResult" };

        RequestHandlerDelegate<TestResult> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedResponse);
        result.Value.Should().Be("ExpectedResult");
    }

    [Fact]
    public async Task LoggingRequestWithResponseBehavior_Handle_LogsBeforeAndAfterExecution()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new LoggingRequestWithResponseBehavior<TestQuery, TestResult>(mockLogger.Object);
        var request = new TestQuery { Query = "Test" };

        RequestHandlerDelegate<TestResult> next = (ct) => Task.FromResult(new TestResult { Value = "Result" });

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling command")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("handled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoggingRequestWithResponseBehavior_Handle_PropagatesException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<LoggingRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new LoggingRequestWithResponseBehavior<TestQuery, TestResult>(mockLogger.Object);
        var request = new TestQuery { Query = "Test" };
        var expectedException = new InvalidOperationException("Test exception");

        RequestHandlerDelegate<TestResult> next = (ct) => throw expectedException;

        // Act
        Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    #endregion

    #region Test Helper Classes

    public class TestCommand : IRequest
    {
        public string Data { get; set; } = string.Empty;
    }

    public class TestQuery : IRequest<TestResult>
    {
        public string Query { get; set; } = string.Empty;
    }

    public class TestResult
    {
        public string Value { get; set; } = string.Empty;
    }

    #endregion
}
