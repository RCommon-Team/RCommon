using FluentAssertions;
using RCommon.Collections;
using Xunit;

namespace RCommon.Core.Tests;

public class PaginatedListTests
{
    #region Default Constructor Tests

    [Fact]
    public void DefaultConstructor_CreatesEmptyList()
    {
        // Arrange & Act
        var list = new PaginatedList<int>();

        // Assert
        list.Should().BeEmpty();
        list.PageIndex.Should().Be(0);
        list.PageSize.Should().Be(0);
        list.TotalCount.Should().Be(0);
        list.TotalPages.Should().Be(0);
    }

    #endregion

    #region IQueryable Constructor Tests

    [Fact]
    public void IQueryableConstructor_WithData_PaginatesCorrectly()
    {
        // Arrange
        var source = Enumerable.Range(1, 100).AsQueryable();

        // Act
        var list = new PaginatedList<int>(source, 1, 10);

        // Assert
        list.Should().HaveCount(10);
        list.PageIndex.Should().Be(1);
        list.PageSize.Should().Be(10);
        list.TotalCount.Should().Be(100);
        list.TotalPages.Should().Be(10);
    }

    [Fact]
    public void IQueryableConstructor_WithNullPageIndex_DefaultsToPage1()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();

        // Act
        var list = new PaginatedList<int>(source, null, 10);

        // Assert
        list.PageIndex.Should().Be(1);
        list.First().Should().Be(1);
        list.Last().Should().Be(10);
    }

    [Fact]
    public void IQueryableConstructor_Page2_ReturnsCorrectItems()
    {
        // Arrange
        var source = Enumerable.Range(1, 100).AsQueryable();

        // Act
        var list = new PaginatedList<int>(source, 2, 10);

        // Assert
        list.First().Should().Be(11);
        list.Last().Should().Be(20);
    }

    [Fact]
    public void IQueryableConstructor_LastPage_ReturnsRemainingItems()
    {
        // Arrange
        var source = Enumerable.Range(1, 25).AsQueryable();

        // Act
        var list = new PaginatedList<int>(source, 3, 10);

        // Assert
        list.Should().HaveCount(5);
        list.First().Should().Be(21);
        list.Last().Should().Be(25);
    }

    #endregion

    #region IList Constructor Tests

    [Fact]
    public void IListConstructor_WithData_PaginatesCorrectly()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).ToList();

        // Act
        var list = new PaginatedList<int>(source, 1, 15);

        // Assert
        list.Should().HaveCount(15);
        list.PageIndex.Should().Be(1);
        list.PageSize.Should().Be(15);
        list.TotalCount.Should().Be(50);
        list.TotalPages.Should().Be(4);
    }

    [Fact]
    public void IListConstructor_WithNullPageIndex_DefaultsToPage1()
    {
        // Arrange
        var source = Enumerable.Range(1, 30).ToList();

        // Act
        var list = new PaginatedList<int>(source, null, 10);

        // Assert
        list.PageIndex.Should().Be(1);
    }

    #endregion

    #region ICollection Constructor Tests

    [Fact]
    public void ICollectionConstructor_WithData_PaginatesCorrectly()
    {
        // Arrange
        ICollection<int> source = Enumerable.Range(1, 75).ToList();

        // Act
        var list = new PaginatedList<int>(source, 2, 20);

        // Assert
        list.Should().HaveCount(20);
        list.PageIndex.Should().Be(2);
        list.PageSize.Should().Be(20);
        list.TotalCount.Should().Be(75);
        list.TotalPages.Should().Be(4);
        list.First().Should().Be(21);
    }

    [Fact]
    public void ICollectionConstructor_WithNullPageIndex_DefaultsToPage1()
    {
        // Arrange
        ICollection<int> source = Enumerable.Range(1, 20).ToList();

        // Act
        var list = new PaginatedList<int>(source, null, 5);

        // Assert
        list.PageIndex.Should().Be(1);
    }

    #endregion

    #region TotalPages Calculation Tests

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(99, 10, 10)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(50, 25, 2)]
    [InlineData(51, 25, 3)]
    public void TotalPages_CalculatedCorrectly(int totalCount, int pageSize, int expectedTotalPages)
    {
        // Arrange
        var source = Enumerable.Range(1, totalCount).AsQueryable();

        // Act
        var list = new PaginatedList<int>(source, 1, pageSize);

        // Assert
        list.TotalPages.Should().Be(expectedTotalPages);
    }

    #endregion

    #region HasPreviousPage Tests

    [Fact]
    public void HasPreviousPage_WhenOnFirstPage_ReturnsFalse()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();
        var list = new PaginatedList<int>(source, 1, 10);

        // Act & Assert
        list.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_WhenOnSecondPage_ReturnsTrue()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();
        var list = new PaginatedList<int>(source, 2, 10);

        // Act & Assert
        list.HasPreviousPage.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(5, true)]
    public void HasPreviousPage_VariousPageNumbers_ReturnsCorrectValue(int pageNumber, bool expectedResult)
    {
        // Arrange
        var source = Enumerable.Range(1, 100).AsQueryable();
        var list = new PaginatedList<int>(source, pageNumber, 10);

        // Act & Assert
        list.HasPreviousPage.Should().Be(expectedResult);
    }

    #endregion

    #region HasNextPage Tests

    [Fact]
    public void HasNextPage_WhenOnLastPage_ReturnsFalse()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();
        var list = new PaginatedList<int>(source, 5, 10); // 50 items, page 5, 10 per page = last page

        // Act & Assert
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasNextPage_WhenNotOnLastPage_ReturnsTrue()
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();
        var list = new PaginatedList<int>(source, 1, 10);

        // Act & Assert
        list.HasNextPage.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    public void HasNextPage_VariousPageNumbers_ReturnsCorrectValue(int pageNumber, bool expectedResult)
    {
        // Arrange
        var source = Enumerable.Range(1, 50).AsQueryable();
        var list = new PaginatedList<int>(source, pageNumber, 10);

        // Act & Assert
        list.HasNextPage.Should().Be(expectedResult);
    }

    #endregion

    #region Complex Object Tests

    [Fact]
    public void PaginatedList_WithComplexObjects_WorksCorrectly()
    {
        // Arrange
        var source = Enumerable.Range(1, 30)
            .Select(i => new TestEntity { Id = i, Name = $"Entity{i}" })
            .AsQueryable();

        // Act
        var list = new PaginatedList<TestEntity>(source, 2, 10);

        // Assert
        list.Should().HaveCount(10);
        list.First().Id.Should().Be(11);
        list.Last().Id.Should().Be(20);
        list.TotalCount.Should().Be(30);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void PaginatedList_WithSingleItem_WorksCorrectly()
    {
        // Arrange
        var source = new List<int> { 1 }.AsQueryable();

        // Act
        var list = new PaginatedList<int>(source, 1, 10);

        // Assert
        list.Should().HaveCount(1);
        list.TotalPages.Should().Be(1);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedList_WithExactlyOnePageOfItems_WorksCorrectly()
    {
        // Arrange
        var source = Enumerable.Range(1, 10).AsQueryable();

        // Act
        var list = new PaginatedList<int>(source, 1, 10);

        // Assert
        list.Should().HaveCount(10);
        list.TotalPages.Should().Be(1);
        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedList_InheritsFromList_SupportsListOperations()
    {
        // Arrange
        var source = Enumerable.Range(1, 20).AsQueryable();
        var list = new PaginatedList<int>(source, 1, 10);

        // Act & Assert
        list.Contains(5).Should().BeTrue();
        list.Contains(15).Should().BeFalse();
        list.IndexOf(5).Should().Be(4);
        list[0].Should().Be(1);
    }

    #endregion

    #region Test Helper Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    #endregion
}
