using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using RCommon.Mediator.MediatR.Behaviors;
using RCommon.Persistence.Transactions;
using System.Transactions;
using Xunit;

namespace RCommon.Mediatr.Tests.Behaviors;

public class UnitOfWorkBehaviorTests
{
    #region UnitOfWorkRequestBehavior Constructor Tests

    [Fact]
    public void UnitOfWorkRequestBehavior_Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();

        // Act
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void UnitOfWorkRequestBehavior_Constructor_WithNullFactory_ThrowsArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();

        // Act
        Action act = () => new UnitOfWorkRequestBehavior<TestCommand, Unit>(null!, mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UnitOfWorkRequestBehavior_Constructor_WithNullLogger_ThrowsArgumentException()
    {
        // Arrange
        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();

        // Act
        Action act = () => new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UnitOfWorkRequestBehavior_ImplementsIPipelineBehavior()
    {
        // Arrange
        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();

        // Act
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().BeAssignableTo<IPipelineBehavior<TestCommand, Unit>>();
    }

    #endregion

    #region UnitOfWorkRequestBehavior Handle Tests

    [Fact]
    public async Task UnitOfWorkRequestBehavior_Handle_CreatesUnitOfWork()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestCommand { Data = "Test" };

        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(Unit.Value);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockUnitOfWorkFactory.Verify(x => x.Create(TransactionMode.Default), Times.Once);
    }

    [Fact]
    public async Task UnitOfWorkRequestBehavior_Handle_CommitsOnSuccess()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestCommand { Data = "Test" };

        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(Unit.Value);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockUnitOfWork.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public async Task UnitOfWorkRequestBehavior_Handle_DisposesUnitOfWork()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestCommand { Data = "Test" };

        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(Unit.Value);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockUnitOfWork.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task UnitOfWorkRequestBehavior_Handle_DoesNotCommitOnException()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestCommand { Data = "Test" };

        RequestHandlerDelegate<Unit> next = (ct) => throw new InvalidOperationException("Test exception");

        // Act
        Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        mockUnitOfWork.Verify(x => x.Commit(), Times.Never);
    }

    [Fact]
    public async Task UnitOfWorkRequestBehavior_Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestCommand { Data = "Test" };
        var expectedResponse = Unit.Value;

        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(expectedResponse);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task UnitOfWorkRequestBehavior_Handle_LogsTransactionStartAndCommit()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(transactionId);

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestBehavior<TestCommand, Unit>>>();
        var behavior = new UnitOfWorkRequestBehavior<TestCommand, Unit>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestCommand { Data = "Test" };

        RequestHandlerDelegate<Unit> next = (ct) => Task.FromResult(Unit.Value);

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Begin transaction")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Commit transaction")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region UnitOfWorkRequestWithResponseBehavior Constructor Tests

    [Fact]
    public void UnitOfWorkRequestWithResponseBehavior_Constructor_WithValidDependencies_CreatesInstance()
    {
        // Arrange
        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        var mockLogger = new Mock<ILogger<UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>>>();

        // Act
        var behavior = new UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().NotBeNull();
    }

    [Fact]
    public void UnitOfWorkRequestWithResponseBehavior_ImplementsIPipelineBehavior()
    {
        // Arrange
        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        var mockLogger = new Mock<ILogger<UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>>>();

        // Act
        var behavior = new UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);

        // Assert
        behavior.Should().BeAssignableTo<IPipelineBehavior<TestQuery, TestResult>>();
    }

    #endregion

    #region UnitOfWorkRequestWithResponseBehavior Handle Tests

    [Fact]
    public async Task UnitOfWorkRequestWithResponseBehavior_Handle_CreatesUnitOfWork()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestQuery { Query = "Test" };

        RequestHandlerDelegate<TestResult> next = (ct) => Task.FromResult(new TestResult { Value = "Result" });

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockUnitOfWorkFactory.Verify(x => x.Create(TransactionMode.Default), Times.Once);
    }

    [Fact]
    public async Task UnitOfWorkRequestWithResponseBehavior_Handle_CommitsOnSuccess()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestQuery { Query = "Test" };

        RequestHandlerDelegate<TestResult> next = (ct) => Task.FromResult(new TestResult { Value = "Result" });

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        mockUnitOfWork.Verify(x => x.Commit(), Times.Once);
    }

    [Fact]
    public async Task UnitOfWorkRequestWithResponseBehavior_Handle_ReturnsResponseFromNext()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
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
    public async Task UnitOfWorkRequestWithResponseBehavior_Handle_DoesNotCommitOnException()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestQuery { Query = "Test" };

        RequestHandlerDelegate<TestResult> next = (ct) => throw new InvalidOperationException("Test exception");

        // Act
        Func<Task> act = async () => await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        mockUnitOfWork.Verify(x => x.Commit(), Times.Never);
    }

    [Fact]
    public async Task UnitOfWorkRequestWithResponseBehavior_Handle_LogsErrorOnException()
    {
        // Arrange
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        mockUnitOfWork.Setup(x => x.TransactionId).Returns(Guid.NewGuid());

        var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        mockUnitOfWorkFactory
            .Setup(x => x.Create(TransactionMode.Default))
            .Returns(mockUnitOfWork.Object);

        var mockLogger = new Mock<ILogger<UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>>>();
        var behavior = new UnitOfWorkRequestWithResponseBehavior<TestQuery, TestResult>(
            mockUnitOfWorkFactory.Object,
            mockLogger.Object);
        var request = new TestQuery { Query = "Test" };

        RequestHandlerDelegate<TestResult> next = (ct) => throw new InvalidOperationException("Test exception");

        // Act
        try
        {
            await behavior.Handle(request, next, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ERROR")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
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
