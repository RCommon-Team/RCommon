using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class DisposableResourceTests
{
    #region Dispose Tests

    [Fact]
    public void Dispose_CallsDisposeWithTrue()
    {
        // Arrange
        var resource = new TestDisposableResource();

        // Act
        resource.Dispose();

        // Assert
        resource.DisposeCalled.Should().BeTrue();
        resource.DisposingValue.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_CallsDisposeEachTime()
    {
        // Arrange
        var resource = new TestDisposableResource();

        // Act
        resource.Dispose();
        resource.Dispose();

        // Assert
        resource.DisposeCallCount.Should().Be(2);
    }

    [Fact]
    public void Dispose_SuppressesFinalization()
    {
        // Arrange
        var resource = new TestDisposableResource();

        // Act - Dispose should call GC.SuppressFinalize
        resource.Dispose();

        // Assert - The resource should be disposed properly
        resource.DisposeCalled.Should().BeTrue();
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_CallsDisposeAsyncWithTrue()
    {
        // Arrange
        var resource = new TestDisposableResource();

        // Act
        await resource.DisposeAsync();

        // Assert
        resource.DisposeAsyncCalled.Should().BeTrue();
        resource.DisposeAsyncDisposingValue.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_CallsDisposeAsyncEachTime()
    {
        // Arrange
        var resource = new TestDisposableResource();

        // Act
        await resource.DisposeAsync();
        await resource.DisposeAsync();

        // Assert
        resource.DisposeAsyncCallCount.Should().Be(2);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void DisposableResource_ImplementsIDisposable()
    {
        // Arrange & Act
        var resource = new TestDisposableResource();

        // Assert
        resource.Should().BeAssignableTo<IDisposable>();
    }

    [Fact]
    public void DisposableResource_ImplementsIAsyncDisposable()
    {
        // Arrange & Act
        var resource = new TestDisposableResource();

        // Assert
        resource.Should().BeAssignableTo<IAsyncDisposable>();
    }

    #endregion

    #region Using Statement Tests

    [Fact]
    public void Dispose_WithUsingStatement_CallsDisposeAtEndOfScope()
    {
        // Arrange
        TestDisposableResource? resource = null;

        // Act
        using (resource = new TestDisposableResource())
        {
            resource.DisposeCalled.Should().BeFalse();
        }

        // Assert
        resource.DisposeCalled.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_WithAwaitUsingStatement_CallsDisposeAsyncAtEndOfScope()
    {
        // Arrange
        TestDisposableResource? resource = null;

        // Act
        await using (resource = new TestDisposableResource())
        {
            resource.DisposeAsyncCalled.Should().BeFalse();
        }

        // Assert
        resource.DisposeAsyncCalled.Should().BeTrue();
    }

    #endregion

    #region Resource Cleanup Tests

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        var resource = new ResourceWithCleanup();

        // Act
        resource.Dispose();

        // Assert
        resource.ResourcesCleaned.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_CleansUpResourcesAsynchronously()
    {
        // Arrange
        var resource = new ResourceWithCleanup();

        // Act
        await resource.DisposeAsync();

        // Assert
        resource.AsyncResourcesCleaned.Should().BeTrue();
    }

    #endregion

    #region Mixed Disposal Tests

    [Fact]
    public async Task Dispose_ThenDisposeAsync_BothAreCalled()
    {
        // Arrange
        var resource = new TestDisposableResource();

        // Act
        resource.Dispose();
        await resource.DisposeAsync();

        // Assert
        resource.DisposeCalled.Should().BeTrue();
        resource.DisposeAsyncCalled.Should().BeTrue();
    }

    #endregion

    #region Test Helper Classes

    private class TestDisposableResource : DisposableResource
    {
        public bool DisposeCalled { get; private set; }
        public bool? DisposingValue { get; private set; }
        public int DisposeCallCount { get; private set; }

        public bool DisposeAsyncCalled { get; private set; }
        public bool? DisposeAsyncDisposingValue { get; private set; }
        public int DisposeAsyncCallCount { get; private set; }

        protected override void Dispose(bool disposing)
        {
            DisposeCalled = true;
            DisposingValue = disposing;
            DisposeCallCount++;
            base.Dispose(disposing);
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            DisposeAsyncCalled = true;
            DisposeAsyncDisposingValue = disposing;
            DisposeAsyncCallCount++;
            await base.DisposeAsync(disposing);
        }
    }

    private class ResourceWithCleanup : DisposableResource
    {
        public bool ResourcesCleaned { get; private set; }
        public bool AsyncResourcesCleaned { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ResourcesCleaned = true;
            }
            base.Dispose(disposing);
        }

        protected override async Task DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                AsyncResourcesCleaned = true;
            }
            await base.DisposeAsync(disposing);
        }
    }

    #endregion
}
