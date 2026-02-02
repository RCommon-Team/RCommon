using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class DisposeActionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAction_CreatesInstance()
    {
        // Arrange & Act
        var disposeAction = new DisposeAction(() => { });

        // Assert
        disposeAction.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new DisposeAction(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_InvokesAction()
    {
        // Arrange
        var actionInvoked = false;
        var disposeAction = new DisposeAction(() => actionInvoked = true);

        // Act
        disposeAction.Dispose();

        // Assert
        actionInvoked.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_InvokesActionMultipleTimes()
    {
        // Arrange
        var invocationCount = 0;
        var disposeAction = new DisposeAction(() => invocationCount++);

        // Act
        disposeAction.Dispose();
        disposeAction.Dispose();
        disposeAction.Dispose();

        // Assert
        invocationCount.Should().Be(3);
    }

    [Fact]
    public void Dispose_WithUsingStatement_InvokesActionAtEndOfScope()
    {
        // Arrange
        var actionInvoked = false;

        // Act
        using (var disposeAction = new DisposeAction(() => actionInvoked = true))
        {
            actionInvoked.Should().BeFalse();
        }

        // Assert
        actionInvoked.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ActionWithSideEffects_ExecutesSideEffects()
    {
        // Arrange
        var counter = 0;
        var disposeAction = new DisposeAction(() => counter = 42);

        // Act
        disposeAction.Dispose();

        // Assert
        counter.Should().Be(42);
    }

    #endregion

    #region IDisposable Interface Tests

    [Fact]
    public void DisposeAction_ImplementsIDisposable()
    {
        // Arrange & Act
        var disposeAction = new DisposeAction(() => { });

        // Assert
        disposeAction.Should().BeAssignableTo<IDisposable>();
    }

    #endregion

    #region Cleanup Scenarios Tests

    [Fact]
    public void Dispose_CanBeUsedForResourceCleanup()
    {
        // Arrange
        var resource = new TestResource();
        var disposeAction = new DisposeAction(() => resource.Release());

        // Act
        disposeAction.Dispose();

        // Assert
        resource.IsReleased.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CanBeUsedForLogging()
    {
        // Arrange
        var logs = new List<string>();
        var disposeAction = new DisposeAction(() => logs.Add("Disposed"));

        // Act
        disposeAction.Dispose();

        // Assert
        logs.Should().Contain("Disposed");
    }

    [Fact]
    public void Dispose_CanRestoreState()
    {
        // Arrange
        var state = "modified";
        var originalState = "original";
        var disposeAction = new DisposeAction(() => state = originalState);

        // Assert before dispose
        state.Should().Be("modified");

        // Act
        disposeAction.Dispose();

        // Assert after dispose
        state.Should().Be("original");
    }

    #endregion

    #region Test Helper Classes

    private class TestResource
    {
        public bool IsReleased { get; private set; }

        public void Release()
        {
            IsReleased = true;
        }
    }

    #endregion
}
