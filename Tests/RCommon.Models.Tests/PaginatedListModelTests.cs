using FluentAssertions;
using RCommon.Models;
using Xunit;

namespace RCommon.Models.Tests;

/// <summary>
/// Tests for the PaginatedListModel abstract records.
/// </summary>
public class PaginatedListModelTests
{
    #region Test Helpers

    /// <summary>
    /// Test entity class for paginated list testing.
    /// </summary>
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Test DTO class for paginated list model with projection testing.
    /// </summary>
    private class TestDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Concrete implementation of PaginatedListModel for single type.
    /// </summary>
    private record TestPaginatedListModel : PaginatedListModel<TestEntity>
    {
        public TestPaginatedListModel(IQueryable<TestEntity> source, PaginatedListRequest request)
            : base(source, request)
        {
        }
    }

    /// <summary>
    /// Concrete implementation of PaginatedListModel with projection (two types).
    /// </summary>
    private record TestProjectedPaginatedListModel : PaginatedListModel<TestEntity, TestDto>
    {
        public TestProjectedPaginatedListModel(IQueryable<TestEntity> source, PaginatedListRequest request)
            : base(source, request)
        {
        }

        protected override IQueryable<TestDto> CastItems(IQueryable<TestEntity> source)
        {
            return source.Select(e => new TestDto
            {
                Id = e.Id,
                DisplayName = e.Name
            });
        }
    }

    /// <summary>
    /// Concrete implementation of PaginatedListRequest for testing.
    /// </summary>
    private record TestPaginatedListRequest : PaginatedListRequest
    {
        public TestPaginatedListRequest()
        {
        }
    }

    private static IQueryable<TestEntity> CreateTestData(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => new TestEntity { Id = i, Name = $"Entity {i}" })
            .AsQueryable();
    }

    #endregion

    #region PaginatedListModel<TSource> Tests

    [Fact]
    public void SingleType_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(50);
        result.TotalPages.Should().Be(5);
    }

    [Fact]
    public void SingleType_Constructor_WithNullSource_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        var act = () => new TestPaginatedListModel(null!, request);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Source Data cannot be null");
    }

    [Fact]
    public void SingleType_Constructor_WithNullRequest_ShouldThrowArgumentException()
    {
        // Arrange
        var source = CreateTestData(10);

        // Act
        var act = () => new TestPaginatedListModel(source, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Request input cannot be null");
    }

    [Fact]
    public void SingleType_Items_ShouldReturnCorrectPage()
    {
        // Arrange
        var source = CreateTestData(25);
        var request = new TestPaginatedListRequest { PageNumber = 2, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Items.Should().HaveCount(10);
        result.Items.First().Id.Should().Be(11);
        result.Items.Last().Id.Should().Be(20);
    }

    [Fact]
    public void SingleType_LastPage_ShouldReturnRemainingItems()
    {
        // Arrange
        var source = CreateTestData(25);
        var request = new TestPaginatedListRequest { PageNumber = 3, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Items.Should().HaveCount(5);
        result.Items.First().Id.Should().Be(21);
        result.Items.Last().Id.Should().Be(25);
    }

    [Fact]
    public void SingleType_TotalPages_ShouldCalculateCorrectly_WhenExactMultiple()
    {
        // Arrange
        var source = CreateTestData(30);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.TotalPages.Should().Be(3);
    }

    [Fact]
    public void SingleType_TotalPages_ShouldCalculateCorrectly_WhenNotExactMultiple()
    {
        // Arrange
        var source = CreateTestData(35);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.TotalPages.Should().Be(4);
    }

    [Fact]
    public void SingleType_HasPreviousPage_ShouldReturnFalse_OnFirstPage()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void SingleType_HasPreviousPage_ShouldReturnTrue_OnSecondPage()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 2, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void SingleType_HasNextPage_ShouldReturnTrue_WhenNotOnLastPage()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void SingleType_HasNextPage_ShouldReturnFalse_OnLastPage()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 5, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void SingleType_SortBy_ShouldUseDefaultId_WhenNullInRequest()
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest { SortBy = null! };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.SortBy.Should().Be("id");
    }

    [Fact]
    public void SingleType_SortBy_ShouldUseRequestValue_WhenProvided()
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest { SortBy = "name" };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.SortBy.Should().Be("name");
    }

    [Theory]
    [InlineData(SortDirectionEnum.Ascending)]
    [InlineData(SortDirectionEnum.Descending)]
    [InlineData(SortDirectionEnum.None)]
    public void SingleType_SortDirection_ShouldMatchRequest(SortDirectionEnum sortDirection)
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest { SortDirection = sortDirection };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.SortDirection.Should().Be(sortDirection);
    }

    [Fact]
    public void SingleType_ShouldImplementIModel()
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest();

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Should().BeAssignableTo<IModel>();
    }

    [Fact]
    public void SingleType_EmptySource_ShouldReturnEmptyItems()
    {
        // Arrange
        var source = Enumerable.Empty<TestEntity>().AsQueryable();
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public void SingleType_SingleItem_ShouldReturnCorrectValues()
    {
        // Arrange
        var source = CreateTestData(1);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region PaginatedListModel<TSource, TOut> Tests

    [Fact]
    public void ProjectedType_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestProjectedPaginatedListModel(source, request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(50);
        result.TotalPages.Should().Be(5);
    }

    [Fact]
    public void ProjectedType_Items_ShouldBeProjectedCorrectly()
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestProjectedPaginatedListModel(source, request);

        // Assert
        result.Items.Should().AllBeOfType<TestDto>();
        result.Items.First().DisplayName.Should().Be("Entity 1");
        result.Items.Last().DisplayName.Should().Be("Entity 10");
    }

    [Fact]
    public void ProjectedType_Constructor_WithNullSource_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        var act = () => new TestProjectedPaginatedListModel(null!, request);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Source Data cannot be null");
    }

    [Fact]
    public void ProjectedType_Constructor_WithNullRequest_ShouldThrowArgumentException()
    {
        // Arrange
        var source = CreateTestData(10);

        // Act
        var act = () => new TestProjectedPaginatedListModel(source, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Request input cannot be null");
    }

    [Fact]
    public void ProjectedType_Items_ShouldReturnCorrectPage()
    {
        // Arrange
        var source = CreateTestData(25);
        var request = new TestPaginatedListRequest { PageNumber = 2, PageSize = 10 };

        // Act
        var result = new TestProjectedPaginatedListModel(source, request);

        // Assert
        result.Items.Should().HaveCount(10);
        result.Items.First().Id.Should().Be(11);
        result.Items.Last().Id.Should().Be(20);
    }

    [Fact]
    public void ProjectedType_HasPreviousPage_ShouldReturnCorrectValue()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 3, PageSize = 10 };

        // Act
        var result = new TestProjectedPaginatedListModel(source, request);

        // Assert
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void ProjectedType_HasNextPage_ShouldReturnCorrectValue()
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = 3, PageSize = 10 };

        // Act
        var result = new TestProjectedPaginatedListModel(source, request);

        // Assert
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void ProjectedType_ShouldImplementIModel()
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest();

        // Act
        var result = new TestProjectedPaginatedListModel(source, request);

        // Assert
        result.Should().BeAssignableTo<IModel>();
    }

    [Fact]
    public void ProjectedType_EmptySource_ShouldReturnEmptyItems()
    {
        // Arrange
        var source = Enumerable.Empty<TestEntity>().AsQueryable();
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 10 };

        // Act
        var result = new TestProjectedPaginatedListModel(source, request);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 5)]
    [InlineData(1, 100)]
    [InlineData(2, 10)]
    [InlineData(5, 5)]
    public void SingleType_VariousPageSizes_ShouldCalculateCorrectly(int pageNumber, int pageSize)
    {
        // Arrange
        var source = CreateTestData(50);
        var request = new TestPaginatedListRequest { PageNumber = pageNumber, PageSize = pageSize };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be(50);
    }

    [Fact]
    public void SingleType_LargePageSize_ShouldReturnAllItemsOnFirstPage()
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest { PageNumber = 1, PageSize = 100 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void SingleType_PageBeyondTotalPages_ShouldReturnEmptyItems()
    {
        // Arrange
        var source = CreateTestData(10);
        var request = new TestPaginatedListRequest { PageNumber = 100, PageSize = 10 };

        // Act
        var result = new TestPaginatedListModel(source, request);

        // Assert
        result.Items.Should().BeEmpty();
        result.PageNumber.Should().Be(100);
        result.TotalPages.Should().Be(1);
    }

    #endregion
}
