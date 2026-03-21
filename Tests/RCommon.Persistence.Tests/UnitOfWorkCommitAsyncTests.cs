using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.Persistence.Transactions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class UnitOfWorkCommitAsyncTests
{
    private readonly Mock<ILogger<UnitOfWork>> _mockLogger;
    private readonly Mock<IGuidGenerator> _mockGuidGenerator;
    private readonly Mock<IOptions<UnitOfWorkSettings>> _mockSettings;
    private readonly UnitOfWorkSettings _settings;

    public UnitOfWorkCommitAsyncTests()
    {
        _mockLogger = new Mock<ILogger<UnitOfWork>>();
        _mockGuidGenerator = new Mock<IGuidGenerator>();
        _mockGuidGenerator.Setup(g => g.Create()).Returns(Guid.NewGuid());
        _settings = new UnitOfWorkSettings
        {
            DefaultIsolation = System.Transactions.IsolationLevel.ReadCommitted,
            AutoCompleteScope = false
        };
        _mockSettings = new Mock<IOptions<UnitOfWorkSettings>>();
        _mockSettings.Setup(s => s.Value).Returns(_settings);
    }

    [Fact]
    public async Task CommitAsync_Without_Tracker_Completes_Successfully()
    {
        using var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);
        await uow.CommitAsync();
        uow.State.Should().Be(UnitOfWorkState.Completed);
    }

    [Fact]
    public async Task CommitAsync_With_Tracker_Dispatches_Events()
    {
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker.Setup(t => t.EmitTransactionalEventsAsync()).ReturnsAsync(true);
        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);
        await uow.CommitAsync();
        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_Logs_Warning_When_Dispatch_Returns_False()
    {
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker.Setup(t => t.EmitTransactionalEventsAsync()).ReturnsAsync(false);
        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);
        await uow.CommitAsync();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Commit_Obsolete_Still_Works_Without_Dispatch()
    {
        var mockTracker = new Mock<IEntityEventTracker>();
        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);
        #pragma warning disable CS0618
        uow.Commit();
        #pragma warning restore CS0618
        uow.State.Should().Be(UnitOfWorkState.Completed);
        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(), Times.Never);
    }

    [Fact]
    public async Task CommitAsync_On_Disposed_UoW_Throws_ObjectDisposedException()
    {
        var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);
        uow.Dispose();
        var act = () => uow.CommitAsync();
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task CommitAsync_On_Already_Completed_UoW_Throws_UnitOfWorkException()
    {
        using var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);
        await uow.CommitAsync();
        var act = () => uow.CommitAsync();
        await act.Should().ThrowAsync<UnitOfWorkException>();
    }

    [Fact]
    public async Task CommitAsync_Then_Dispose_Does_Not_Double_Dispose_TransactionScope()
    {
        var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);
        await uow.CommitAsync();
        var act = () => { uow.Dispose(); };
        act.Should().NotThrow("Dispose after CommitAsync must be safe (no double-dispose)");
    }
}
