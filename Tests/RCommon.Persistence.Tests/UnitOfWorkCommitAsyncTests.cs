using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.EventHandling;
using RCommon.Models.Events;
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
        mockTracker.Setup(t => t.PersistEventsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockTracker.Setup(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);
        await uow.CommitAsync();
        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_Logs_Warning_When_Dispatch_Returns_False()
    {
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker.Setup(t => t.PersistEventsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockTracker.Setup(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
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
        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IEntityEventTracker> TrackerWithPendingEvents()
    {
        var entity = new Mock<IBusinessEntity>();
        entity.Setup(e => e.LocalEvents).Returns(new List<ISerializableEvent> { Mock.Of<ISerializableEvent>() });
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker.Setup(t => t.TrackedEntities).Returns(new List<IBusinessEntity> { entity.Object });
        return mockTracker;
    }

    private void VerifyWarningLogged(Times times)
    {
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times);
    }

    [Fact]
    public void Commit_Obsolete_Warns_When_Tracker_Has_Pending_Events()
    {
        // Sync Commit() skips Phase 1 (outbox persistence) and Phase 3 (dispatch), so pending domain
        // events are silently dropped. It must fail loud rather than silently discard them.
        var mockTracker = TrackerWithPendingEvents();
        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);
        #pragma warning disable CS0618
        uow.Commit();
        #pragma warning restore CS0618
        uow.State.Should().Be(UnitOfWorkState.Completed);
        VerifyWarningLogged(Times.Once());
    }

    [Fact]
    public void Commit_Obsolete_Does_Not_Warn_When_No_Pending_Events()
    {
        // A tracked entity with no local events loses nothing on a sync commit -> no false-positive warning.
        var entity = new Mock<IBusinessEntity>();
        entity.Setup(e => e.LocalEvents).Returns(new List<ISerializableEvent>());
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker.Setup(t => t.TrackedEntities).Returns(new List<IBusinessEntity> { entity.Object });
        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);
        #pragma warning disable CS0618
        uow.Commit();
        #pragma warning restore CS0618
        VerifyWarningLogged(Times.Never());
    }

    [Fact]
    public void Dispose_Without_Commit_Warns_That_Transaction_Was_Rolled_Back()
    {
        // AutoComplete is off. Disposing without a successful CommitAsync rolls back the ambient
        // transaction, silently discarding any writes made in the scope with no error. Fail loud.
        _settings.AutoCompleteScope = false;
        using (new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object))
        {
            // no commit attempted
        }
        VerifyWarningLogged(Times.Once());
    }

    [Fact]
    public async Task Dispose_After_CommitAsync_Does_Not_Warn_About_Rollback()
    {
        _settings.AutoCompleteScope = false;
        var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object);
        await uow.CommitAsync();
        uow.Dispose();
        VerifyWarningLogged(Times.Never());
    }

    [Fact]
    public void Dispose_AutoComplete_Warns_When_Tracker_Has_Pending_Events()
    {
        // AutoComplete-on-dispose routes through the obsolete sync Commit(), so it too silently drops
        // pending events. The warning must surface on this path as well.
        _settings.AutoCompleteScope = true;
        var mockTracker = TrackerWithPendingEvents();
        using (new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object))
        {
            // no explicit commit; dispose auto-completes
        }
        VerifyWarningLogged(Times.Once());
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

    [Fact]
    public async Task CommitAsync_Dispatches_Then_Persists_Then_Emits()
    {
        var callOrder = new System.Collections.Generic.List<string>();
        var mockTracker = new Mock<IEntityEventTracker>();
        mockTracker
            .Setup(t => t.DispatchDomainEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("DispatchDomainEventsAsync"))
            .Returns(Task.CompletedTask);
        mockTracker
            .Setup(t => t.PersistEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("PersistEventsAsync"))
            .Returns(Task.CompletedTask);
        mockTracker
            .Setup(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("EmitTransactionalEventsAsync"))
            .ReturnsAsync(true);

        using var uow = new UnitOfWork(
            _mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);
        await uow.CommitAsync();

        callOrder.Should().ContainInOrder("DispatchDomainEventsAsync", "PersistEventsAsync", "EmitTransactionalEventsAsync");
        mockTracker.Verify(t => t.DispatchDomainEventsAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockTracker.Verify(t => t.PersistEventsAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_When_PreCommit_Dispatch_Throws_Does_Not_Commit_Or_Persist_Or_Emit()
    {
        var mockTracker = new Mock<IEntityEventTracker>();
        // Stub the other two with non-null completed tasks so the pre-reorder red run doesn't NRE and mask
        // the assertion; after the reorder they must never be reached.
        mockTracker.Setup(t => t.PersistEventsAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockTracker.Setup(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        mockTracker.Setup(t => t.DispatchDomainEventsAsync(It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("handler failed"));

        using var uow = new UnitOfWork(_mockLogger.Object, _mockGuidGenerator.Object, _mockSettings.Object, mockTracker.Object);

        var act = () => uow.CommitAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
        uow.State.Should().NotBe(UnitOfWorkState.Completed);        // transaction not completed => rolls back
        mockTracker.Verify(t => t.PersistEventsAsync(It.IsAny<CancellationToken>()), Times.Never); // no outbox rows
        mockTracker.Verify(t => t.EmitTransactionalEventsAsync(It.IsAny<CancellationToken>()), Times.Never); // no relay
    }
}
