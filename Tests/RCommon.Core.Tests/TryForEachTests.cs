using FluentAssertions;
using Xunit;

namespace RCommon.Core.Tests;

public class TryForEachTests
{
    #region IEnumerable overload

    [Fact]
    public void TryForEach_AllSucceed_ExecutesAllActions()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var visited = new List<int>();

        // Act
        items.TryForEach(item => visited.Add(item));

        // Assert
        visited.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void TryForEach_ExceptionThrown_ContinuesEnumerating()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var visited = new List<int>();

        // Act
        items.TryForEach(item =>
        {
            if (item == 2) throw new InvalidOperationException("fail");
            visited.Add(item);
        });

        // Assert - item 2 threw but 1 and 3 were still processed
        visited.Should().Equal(1, 3);
    }

    [Fact]
    public void TryForEach_WithOnError_CallbackReceivesItemAndException()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var errors = new List<(int item, Exception ex)>();

        // Act
        items.TryForEach(
            item => { if (item == 2) throw new InvalidOperationException("fail"); },
            onError: (item, ex) => errors.Add((item, ex))
        );

        // Assert
        errors.Should().HaveCount(1);
        errors[0].item.Should().Be(2);
        errors[0].ex.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public void TryForEach_WithoutOnError_SilentlySwallowsExceptions()
    {
        // Arrange
        var items = new[] { 1, 2, 3 };
        var visited = new List<int>();

        // Act - no onError callback provided; exceptions must not propagate
        var act = () => items.TryForEach(item =>
        {
            if (item == 2) throw new InvalidOperationException("fail");
            visited.Add(item);
        });

        // Assert - no exception escapes the extension method
        act.Should().NotThrow();
        visited.Should().Equal(1, 3);
    }

    [Fact]
    public void TryForEach_MultipleFailures_OnErrorCalledForEach()
    {
        // Arrange
        var items = new[] { 1, 2, 3, 4, 5 };
        var errors = new List<(int item, Exception ex)>();
        var visited = new List<int>();

        // Act - items 2 and 4 throw
        items.TryForEach(
            item =>
            {
                if (item == 2 || item == 4) throw new ArgumentException($"bad item {item}");
                visited.Add(item);
            },
            onError: (item, ex) => errors.Add((item, ex))
        );

        // Assert
        errors.Should().HaveCount(2);
        errors[0].item.Should().Be(2);
        errors[1].item.Should().Be(4);
        errors.Should().AllSatisfy(e => e.ex.Should().BeOfType<ArgumentException>());
        visited.Should().Equal(1, 3, 5);
    }

    [Fact]
    public void TryForEach_EmptyCollection_DoesNothing()
    {
        // Arrange
        var items = Array.Empty<int>();
        var visited = new List<int>();
        var errors = new List<(int item, Exception ex)>();

        // Act
        items.TryForEach(
            item => visited.Add(item),
            onError: (item, ex) => errors.Add((item, ex))
        );

        // Assert
        visited.Should().BeEmpty();
        errors.Should().BeEmpty();
    }

    #endregion

    #region IEnumerator overload

    [Fact]
    public void TryForEach_Enumerator_AllSucceed_ExecutesAllActions()
    {
        // Arrange
        var items = new List<int> { 10, 20, 30 };
        var visited = new List<int>();

        // Act
        using var enumerator = items.GetEnumerator();
        enumerator.TryForEach(item => visited.Add(item));

        // Assert
        visited.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void TryForEach_Enumerator_ExceptionThrown_ContinuesEnumerating()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };
        var visited = new List<string>();

        // Act
        using var enumerator = items.GetEnumerator();
        enumerator.TryForEach(item =>
        {
            if (item == "b") throw new InvalidOperationException("fail");
            visited.Add(item);
        });

        // Assert - "b" threw but "a" and "c" were still processed
        visited.Should().Equal("a", "c");
    }

    [Fact]
    public void TryForEach_Enumerator_WithOnError_CallbackReceivesItemAndException()
    {
        // Arrange
        var items = new List<string> { "x", "y", "z" };
        var errors = new List<(string item, Exception ex)>();

        // Act
        using var enumerator = items.GetEnumerator();
        enumerator.TryForEach(
            item => { if (item == "y") throw new NotSupportedException("unsupported"); },
            onError: (item, ex) => errors.Add((item, ex))
        );

        // Assert
        errors.Should().HaveCount(1);
        errors[0].item.Should().Be("y");
        errors[0].ex.Should().BeOfType<NotSupportedException>();
    }

    #endregion
}
