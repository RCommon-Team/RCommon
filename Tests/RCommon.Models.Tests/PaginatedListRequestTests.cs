using FluentAssertions;
using RCommon.Models;
using Xunit;

namespace RCommon.Models.Tests;

/// <summary>
/// Tests for the PaginatedListRequest abstract record (tested via concrete implementation).
/// </summary>
public class PaginatedListRequestTests
{
    /// <summary>
    /// Concrete implementation of PaginatedListRequest for testing.
    /// </summary>
    private record TestPaginatedListRequest : PaginatedListRequest
    {
        public TestPaginatedListRequest()
        {
        }
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var request = new TestPaginatedListRequest();

        // Assert
        request.Should().NotBeNull();
        request.PageNumber.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.SortBy.Should().Be("id");
        request.SortDirection.Should().Be(SortDirectionEnum.None);
    }

    [Fact]
    public void PageNumber_DefaultValue_ShouldBeOne()
    {
        // Arrange & Act
        var request = new TestPaginatedListRequest();

        // Assert
        request.PageNumber.Should().Be(1);
    }

    [Fact]
    public void PageSize_DefaultValue_ShouldBeTwenty()
    {
        // Arrange & Act
        var request = new TestPaginatedListRequest();

        // Assert
        request.PageSize.Should().Be(20);
    }

    [Fact]
    public void SortBy_DefaultValue_ShouldBeId()
    {
        // Arrange & Act
        var request = new TestPaginatedListRequest();

        // Assert
        request.SortBy.Should().Be("id");
    }

    [Fact]
    public void SortDirection_DefaultValue_ShouldBeNone()
    {
        // Arrange & Act
        var request = new TestPaginatedListRequest();

        // Assert
        request.SortDirection.Should().Be(SortDirectionEnum.None);
    }

    [Fact]
    public void PageNumber_ShouldBeSettable()
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.PageNumber = 5;

        // Assert
        request.PageNumber.Should().Be(5);
    }

    [Fact]
    public void PageSize_ShouldBeSettable()
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.PageSize = 50;

        // Assert
        request.PageSize.Should().Be(50);
    }

    [Fact]
    public void SortBy_ShouldBeSettable()
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.SortBy = "name";

        // Assert
        request.SortBy.Should().Be("name");
    }

    [Fact]
    public void SortDirection_ShouldBeSettable()
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.SortDirection = SortDirectionEnum.Ascending;

        // Assert
        request.SortDirection.Should().Be(SortDirectionEnum.Ascending);
    }

    [Fact]
    public void ShouldImplementIPaginatedListRequest()
    {
        // Arrange & Act
        var request = new TestPaginatedListRequest();

        // Assert
        request.Should().BeAssignableTo<IPaginatedListRequest>();
    }

    [Fact]
    public void ShouldImplementIModel()
    {
        // Arrange & Act
        var request = new TestPaginatedListRequest();

        // Assert
        request.Should().BeAssignableTo<IModel>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void PageNumber_ShouldAcceptVariousValues(int pageNumber)
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.PageNumber = pageNumber;

        // Assert
        request.PageNumber.Should().Be(pageNumber);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public void PageSize_ShouldAcceptVariousValues(int pageSize)
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.PageSize = pageSize;

        // Assert
        request.PageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData("id")]
    [InlineData("name")]
    [InlineData("createdDate")]
    [InlineData("")]
    public void SortBy_ShouldAcceptVariousValues(string sortBy)
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.SortBy = sortBy;

        // Assert
        request.SortBy.Should().Be(sortBy);
    }

    [Theory]
    [InlineData(SortDirectionEnum.Ascending)]
    [InlineData(SortDirectionEnum.Descending)]
    [InlineData(SortDirectionEnum.None)]
    public void SortDirection_ShouldAcceptAllEnumValues(SortDirectionEnum sortDirection)
    {
        // Arrange
        var request = new TestPaginatedListRequest();

        // Act
        request.SortDirection = sortDirection;

        // Assert
        request.SortDirection.Should().Be(sortDirection);
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var request1 = new TestPaginatedListRequest
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "name",
            SortDirection = SortDirectionEnum.Ascending
        };
        var request2 = new TestPaginatedListRequest
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "name",
            SortDirection = SortDirectionEnum.Ascending
        };

        // Act & Assert
        request1.Should().Be(request2);
        (request1 == request2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithDifferentPageNumber_ShouldNotBeEqual()
    {
        // Arrange
        var request1 = new TestPaginatedListRequest { PageNumber = 1 };
        var request2 = new TestPaginatedListRequest { PageNumber = 2 };

        // Act & Assert
        request1.Should().NotBe(request2);
        (request1 != request2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithDifferentPageSize_ShouldNotBeEqual()
    {
        // Arrange
        var request1 = new TestPaginatedListRequest { PageSize = 10 };
        var request2 = new TestPaginatedListRequest { PageSize = 20 };

        // Act & Assert
        request1.Should().NotBe(request2);
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithDifferentSortBy_ShouldNotBeEqual()
    {
        // Arrange
        var request1 = new TestPaginatedListRequest { SortBy = "name" };
        var request2 = new TestPaginatedListRequest { SortBy = "date" };

        // Act & Assert
        request1.Should().NotBe(request2);
    }

    [Fact]
    public void RecordEquality_TwoInstancesWithDifferentSortDirection_ShouldNotBeEqual()
    {
        // Arrange
        var request1 = new TestPaginatedListRequest { SortDirection = SortDirectionEnum.Ascending };
        var request2 = new TestPaginatedListRequest { SortDirection = SortDirectionEnum.Descending };

        // Act & Assert
        request1.Should().NotBe(request2);
    }

    [Fact]
    public void GetHashCode_TwoEqualInstances_ShouldHaveSameHashCode()
    {
        // Arrange
        var request1 = new TestPaginatedListRequest
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "name",
            SortDirection = SortDirectionEnum.Ascending
        };
        var request2 = new TestPaginatedListRequest
        {
            PageNumber = 2,
            PageSize = 10,
            SortBy = "name",
            SortDirection = SortDirectionEnum.Ascending
        };

        // Act
        var hash1 = request1.GetHashCode();
        var hash2 = request2.GetHashCode();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void RecordWith_ShouldCreateNewInstanceWithModifiedValue()
    {
        // Arrange
        var original = new TestPaginatedListRequest
        {
            PageNumber = 1,
            PageSize = 10,
            SortBy = "id",
            SortDirection = SortDirectionEnum.None
        };

        // Act
        var modified = original with { PageNumber = 5 };

        // Assert
        modified.PageNumber.Should().Be(5);
        modified.PageSize.Should().Be(10);
        modified.SortBy.Should().Be("id");
        modified.SortDirection.Should().Be(SortDirectionEnum.None);
        original.PageNumber.Should().Be(1); // Original unchanged
    }
}
