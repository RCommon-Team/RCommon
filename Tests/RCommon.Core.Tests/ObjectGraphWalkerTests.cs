using FluentAssertions;
using RCommon.Reflection;
using Xunit;

namespace RCommon.Core.Tests;

public class ObjectGraphWalkerTests
{
    #region TraverseGraphFor_NullRoot Tests

    [Fact]
    public void TraverseGraphFor_NullRoot_ReturnsEmpty()
    {
        // Arrange
        object? root = null;

        // Act
        var act = () => ObjectGraphWalker.TraverseGraphFor<TargetItem>(root!);

        // Assert
        act.Should().NotThrow();
        act().Should().BeEmpty();
    }

    #endregion

    #region TraverseGraphFor_SimpleMatch Tests

    [Fact]
    public void TraverseGraphFor_SimpleMatch_FindsInstance()
    {
        // Arrange
        var target = new TargetItem { Name = "Found" };

        // Act
        var results = ObjectGraphWalker.TraverseGraphFor<TargetItem>(target);

        // Assert
        results.Should().ContainSingle()
            .Which.Should().BeSameAs(target);
    }

    #endregion

    #region TraverseGraphFor_NestedMatch Tests

    [Fact]
    public void TraverseGraphFor_NestedMatch_FindsNestedInstances()
    {
        // Arrange
        var nestedItem = new TargetItem { Name = "Nested" };
        var container = new Container { Item = nestedItem };

        // Act
        var results = ObjectGraphWalker.TraverseGraphFor<TargetItem>(container);

        // Assert
        results.Should().ContainSingle()
            .Which.Should().BeSameAs(nestedItem);
    }

    #endregion

    #region TraverseGraphFor_CircularReference Tests

    [Fact]
    public void TraverseGraphFor_CircularReference_DoesNotInfiniteLoop()
    {
        // Arrange
        var a = new Container { Item = new TargetItem { Name = "A" } };
        var b = new Container { Item = new TargetItem { Name = "B" } };
        a.Self = b;
        b.Self = a;

        // Act
        var act = () => ObjectGraphWalker.TraverseGraphFor<TargetItem>(a);

        // Assert
        act.Should().NotThrow();
        act().Should().HaveCount(2);
    }

    #endregion

    #region TraverseGraphFor_Collection Tests

    [Fact]
    public void TraverseGraphFor_Collection_FindsItemsInList()
    {
        // Arrange
        var item1 = new TargetItem { Name = "First" };
        var item2 = new TargetItem { Name = "Second" };
        var item3 = new TargetItem { Name = "Third" };
        var listContainer = new ListContainer
        {
            Items = new List<TargetItem> { item1, item2, item3 }
        };

        // Act
        var results = ObjectGraphWalker.TraverseGraphFor<TargetItem>(listContainer);

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(item1);
        results.Should().Contain(item2);
        results.Should().Contain(item3);
    }

    #endregion

    #region TraverseGraphFor_NoMatches Tests

    [Fact]
    public void TraverseGraphFor_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var container = new Container
        {
            Item = null,
            Self = null
        };

        // Act
        var results = ObjectGraphWalker.TraverseGraphFor<TargetItem>(container);

        // Assert
        results.Should().BeEmpty();
    }

    #endregion

    #region TraverseGraphFor_ValueTypeProperties Tests

    [Fact]
    public void TraverseGraphFor_ValueTypeProperties_DoesNotRecurseInfinitely()
    {
        // Arrange
        var obj = new ContainerWithValueTypes
        {
            Created = DateTime.UtcNow,
            Count = 42,
            Item = new TargetItem { Name = "WithValueTypes" }
        };

        // Act
        var act = () => ObjectGraphWalker.TraverseGraphFor<TargetItem>(obj);

        // Assert
        act.Should().NotThrow();
        act().Should().ContainSingle()
            .Which.Name.Should().Be("WithValueTypes");
    }

    #endregion

    #region TraverseGraphFor_DuplicateReferences Tests

    [Fact]
    public void TraverseGraphFor_DuplicateReferences_FoundOnce()
    {
        // Arrange
        var sharedItem = new TargetItem { Name = "Shared" };
        var dualRef = new DualRefContainer
        {
            ItemA = sharedItem,
            ItemB = sharedItem
        };

        // Act
        var results = ObjectGraphWalker.TraverseGraphFor<TargetItem>(dualRef);

        // Assert
        results.Should().ContainSingle()
            .Which.Should().BeSameAs(sharedItem);
    }

    #endregion

    #region Test Helper Classes

    private class TargetItem { public string? Name { get; set; } }

    private class Container
    {
        public TargetItem? Item { get; set; }
        public Container? Self { get; set; }
    }

    private class ContainerWithValueTypes
    {
        public DateTime Created { get; set; }
        public int Count { get; set; }
        public TargetItem? Item { get; set; }
    }

    private class DualRefContainer
    {
        public TargetItem? ItemA { get; set; }
        public TargetItem? ItemB { get; set; }
    }

    private class ListContainer
    {
        public List<TargetItem>? Items { get; set; }
    }

    #endregion
}
