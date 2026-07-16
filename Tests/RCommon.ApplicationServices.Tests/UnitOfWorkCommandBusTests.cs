using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using RCommon.ApplicationServices.Commands;
using RCommon.Models.Commands;
using RCommon.Models.ExecutionResults;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.ApplicationServices.Tests;

/// <summary>
/// Tests for UnitOfWorkCommandBus, the native-ICommandBus equivalent of RCommon.Mediatr's
/// AddUnitOfWorkToRequestPipeline() -- see docs/specs/cqrs/native-command-bus-transactions.md.
/// </summary>
public class UnitOfWorkCommandBusTests
{
    private readonly Mock<ICommandBus> _mockInnerBus;
    private readonly Mock<IUnitOfWorkFactory> _mockUnitOfWorkFactory;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public UnitOfWorkCommandBusTests()
    {
        _mockInnerBus = new Mock<ICommandBus>();
        _mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockUnitOfWorkFactory
            .Setup(f => f.Create(TransactionMode.Default))
            .Returns(_mockUnitOfWork.Object);
    }

    private UnitOfWorkCommandBus CreateBus()
        => new UnitOfWorkCommandBus(_mockInnerBus.Object, _mockUnitOfWorkFactory.Object);

    [Fact]
    public async Task DispatchCommandAsync_OnSuccess_CommitsUnitOfWorkExactlyOnce()
    {
        // Arrange
        var command = new TestUowCommand();
        _mockInnerBus
            .Setup(b => b.DispatchCommandAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessExecutionResult());
        var bus = CreateBus();

        // Act
        await bus.DispatchCommandAsync(command);

        // Assert
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchCommandAsync_OnSuccess_DisposesUnitOfWork()
    {
        // Arrange
        var command = new TestUowCommand();
        _mockInnerBus
            .Setup(b => b.DispatchCommandAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessExecutionResult());
        var bus = CreateBus();

        // Act
        await bus.DispatchCommandAsync(command);

        // Assert
        _mockUnitOfWork.Verify(u => u.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DispatchCommandAsync_OnSuccess_ReturnsInnerBusResult()
    {
        // Arrange
        var command = new TestUowCommand();
        var expectedResult = new SuccessExecutionResult();
        _mockInnerBus
            .Setup(b => b.DispatchCommandAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);
        var bus = CreateBus();

        // Act
        var result = await bus.DispatchCommandAsync(command);

        // Assert
        result.Should().BeSameAs(expectedResult);
    }

    [Fact]
    public async Task DispatchCommandAsync_WhenHandlerThrows_DoesNotCommitAndPropagatesException()
    {
        // Arrange -- rollback path: the using-scoped IUnitOfWork is disposed without CommitAsync
        // ever being called, so the transaction rolls back.
        var command = new TestUowCommand();
        _mockInnerBus
            .Setup(b => b.DispatchCommandAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("handler failure"));
        var bus = CreateBus();

        // Act
        var act = async () => await bus.DispatchCommandAsync(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("handler failure");
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DispatchCommandAsync_CreatesUnitOfWorkWithDefaultTransactionMode()
    {
        // Arrange
        var command = new TestUowCommand();
        _mockInnerBus
            .Setup(b => b.DispatchCommandAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SuccessExecutionResult());
        var bus = CreateBus();

        // Act
        await bus.DispatchCommandAsync(command);

        // Assert
        _mockUnitOfWorkFactory.Verify(f => f.Create(TransactionMode.Default), Times.Once);
    }

    [Fact]
    public async Task DispatchCommandAsync_PassesCancellationTokenToInnerBusAndCommit()
    {
        // Arrange
        var command = new TestUowCommand();
        using var cts = new CancellationTokenSource();
        _mockInnerBus
            .Setup(b => b.DispatchCommandAsync(command, cts.Token))
            .ReturnsAsync(new SuccessExecutionResult());
        var bus = CreateBus();

        // Act
        await bus.DispatchCommandAsync(command, cts.Token);

        // Assert
        _mockInnerBus.Verify(b => b.DispatchCommandAsync(command, cts.Token), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(cts.Token), Times.Once);
    }
}

public class TestUowCommand : ICommand<SuccessExecutionResult>
{
}
