using FluentAssertions;
using RCommon.Models;
using Xunit;

namespace RCommon.Models.Tests;

/// <summary>
/// Tests for the SearchPaginatedListRequest record.
/// </summary>
public class SearchPaginatedListRequestTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var request = new SearchPaginatedListRequest();

        // Assert
        request.Should().NotBeNull();
        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.SortBy.Should().Be("id");
        request.SortDirection.Should().Be(SortDirectionEnum.None);
        request.SearchString.Should().BeNull();
    }

    [Fact]
    public void SearchString_ShouldBeSettable()
    {
        // Arrange
        var request = new SearchPaginatedListRequest();

        // Act
        request.SearchString = "test search";

        // Assert
        request.SearchString.Should().Be("test search");
    }

    [Fact]
    public void ShouldInheritFromPaginatedListRequest()
    {
        // Arrange & Act
        var request = new SearchPaginatedListRequest();

        // Assert
        request.Should().BeAssignableTo<PaginatedListRequest>();
    }

    [Fact]
    public void ShouldImplementISearchPaginatedListRequest()
    {
        // Arrange & Act
        var request = new SearchPaginatedListRequest();

        // Assert
        request.Should().BeAssignableTo<ISearchPaginatedListRequest>();
    }

    [Fact]
    public void ShouldImplementIPaginatedListRequest()
    {
        // Arrange & Act
        var request = new SearchPaginatedListRequest();

        // Assert
        request.Should().BeAssignableTo<IPaginatedListRequest>();
    }

    [Fact]
    public void ShouldImplementIModel()
    {
        // Arrange & Act
        var request = new SearchPaginatedListRequest();

        // Assert
        request.Should().BeAssignableTo<IModel>();
    }

    [Fact]
    public void PageNumber_ShouldBeSettable()
    {
        // Arrange
        var request = new SearchPaginatedListRequest();

        // Act
        request.PageNumber = 5;

        // Assert
        request.PageNumber.Should().Be(5);
    }

    [Fact]
    public void PageSize_ShouldBeSettable()
    {
        // Arrange
        var request = new SearchPaginatedListRequest();

        // Act
        request.PageSize = 50;

        // Assert
        request.PageSize.Should().Be(50);
    }

    [Fact]
    public void SortBy_ShouldBeSettable()
    {
        // Arrange
        var request = new SearchPaginatedListRequest();

        // Act
        request.SortBy = "name";

        // Assert
        request.SortBy.Should().Be("name");
    }

    [Fact]
    public void SortDirection_ShouldBeSettable()
    {
        // Arrange
        var request = new SearchPaginatedListRequest();

        // Act
        request.SortDirection = SortDirectionEnum.Ascending;

        // Assert
        request.SortDirection.Should().Be(SortDirectionEnum.Ascending);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("search term")]
    [InlineData("multi word search")]
    public void SearchString_ShouldAcceptVariousValues(string searchString)
    {
        // Arrange
        var request = new SearchPaginatedListRequest();

        // Act
        request.SearchString = searchString;

        // Assert
        request.SearchString.Should().Be(searchString);
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var request1 = new SearchPaginatedListRequest
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "name",
            SortDirection = SortDirectionEnum.Ascending,
            SearchString = "test"
        };
        var request2 = new SearchPaginatedListRequest
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "name",
            SortDirection = SortDirectionEnum.Ascending,
            SearchString = "test"
        };

        // Act & Assert
        request1.Should().Be(request2);
        (request1 == request2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithDifferentSearchString_ShouldNotBeEqual()
    {
        // Arrange
        var request1 = new SearchPaginatedListRequest { SearchString = "search1" };
        var request2 = new SearchPaginatedListRequest { SearchString = "search2" };

        // Act & Assert
        request1.Should().NotBe(request2);
        (request1 != request2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_TwoEqualInstances_ShouldHaveSameHashCode()
    {
        // Arrange
        var request1 = new SearchPaginatedListRequest { SearchString = "test" };
        var request2 = new SearchPaginatedListRequest { SearchString = "test" };

        // Act
        var hash1 = request1.GetHashCode();
        var hash2 = request2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Theory]
    [InlineData(SortDirectionEnum.Ascending)]
    [InlineData(SortDirectionEnum.Descending)]
    [InlineData(SortDirectionEnum.None)]
    public void SortDirection_ShouldAcceptAllEnumValues(SortDirectionEnum sortDirection)
    {
        // Arrange
        var request = new SearchPaginatedListRequest();

        // Act
        request.SortDirection = sortDirection;

        // Assert
        request.SortDirection.Should().Be(sortDirection);
    }
}
