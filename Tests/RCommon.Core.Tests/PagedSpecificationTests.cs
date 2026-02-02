using FluentAssertions;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Core.Tests;

public class PagedSpecificationTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = x => x.Id > 0;
        Expression<Func<TestEntity, object>> orderBy = x => x.Name!;
        var orderByAscending = true;
        var pageNumber = 1;
        var pageSize = 10;

        // Act
        var spec = new PagedSpecification<TestEntity>(predicate, orderBy, orderByAscending, pageNumber, pageSize);

        // Assert
        spec.Predicate.Should().NotBeNull();
        spec.OrderByExpression.Should().BeSameAs(orderBy);
        spec.OrderByAscending.Should().BeTrue();
        spec.PageNumber.Should().Be(1);
        spec.PageSize.Should().Be(10);
    }

    [Fact]
    public void Constructor_WithDescendingOrder_SetsOrderByAscendingToFalse()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = x => x.Id > 0;
        Expression<Func<TestEntity, object>> orderBy = x => x.Id;

        // Act
        var spec = new PagedSpecification<TestEntity>(predicate, orderBy, false, 1, 25);

        // Assert
        spec.OrderByAscending.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InheritsFromSpecification_PredicateWorks()
    {
        // Arrange
        Expression<Func<TestEntity, bool>> predicate = x => x.IsActive;
        Expression<Func<TestEntity, object>> orderBy = x => x.Name!;

        // Act
        var spec = new PagedSpecification<TestEntity>(predicate, orderBy, true, 1, 10);

        // Assert
        spec.IsSatisfiedBy(new TestEntity { IsActive = true }).Should().BeTrue();
        spec.IsSatisfiedBy(new TestEntity { IsActive = false }).Should().BeFalse();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void OrderByExpression_ReturnsExpressionSetInConstructor()
    {
        // Arrange
        Expression<Func<TestEntity, object>> orderBy = x => x.Id;
        var spec = new PagedSpecification<TestEntity>(x => true, orderBy, true, 1, 10);

        // Act
        var result = spec.OrderByExpression;

        // Assert
        result.Should().BeSameAs(orderBy);
    }

    [Fact]
    public void PageNumber_ReturnsValueSetInConstructor()
    {
        // Arrange
        var expectedPageNumber = 5;
        var spec = new PagedSpecification<TestEntity>(x => true, x => x.Id, true, expectedPageNumber, 10);

        // Act
        var result = spec.PageNumber;

        // Assert
        result.Should().Be(expectedPageNumber);
    }

    [Fact]
    public void PageSize_ReturnsValueSetInConstructor()
    {
        // Arrange
        var expectedPageSize = 25;
        var spec = new PagedSpecification<TestEntity>(x => true, x => x.Id, true, 1, expectedPageSize);

        // Act
        var result = spec.PageSize;

        // Assert
        result.Should().Be(expectedPageSize);
    }

    [Fact]
    public void OrderByAscending_CanBeModified()
    {
        // Arrange
        var spec = new PagedSpecification<TestEntity>(x => true, x => x.Id, true, 1, 10);

        // Act
        spec.OrderByAscending = false;

        // Assert
        spec.OrderByAscending.Should().BeFalse();
    }

    #endregion

    #region IsSatisfiedBy Tests (Inherited)

    [Fact]
    public void IsSatisfiedBy_WithMatchingEntity_ReturnsTrue()
    {
        // Arrange
        var spec = new PagedSpecification<TestEntity>(
            x => x.Id > 0 && x.IsActive,
            x => x.Name!,
            true,
            1,
            10);
        var entity = new TestEntity { Id = 1, IsActive = true, Name = "Test" };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSatisfiedBy_WithNonMatchingEntity_ReturnsFalse()
    {
        // Arrange
        var spec = new PagedSpecification<TestEntity>(
            x => x.Id > 10,
            x => x.Name!,
            true,
            1,
            10);
        var entity = new TestEntity { Id = 5 };

        // Act
        var result = spec.IsSatisfiedBy(entity);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Various Page Configuration Tests

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 25)]
    [InlineData(1, 50)]
    [InlineData(1, 100)]
    public void Constructor_WithVariousPageSizes_SetsPageSizeCorrectly(int pageNumber, int pageSize)
    {
        // Arrange & Act
        var spec = new PagedSpecification<TestEntity>(x => true, x => x.Id, true, pageNumber, pageSize);

        // Assert
        spec.PageNumber.Should().Be(pageNumber);
        spec.PageSize.Should().Be(pageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public void Constructor_WithVariousPageNumbers_SetsPageNumberCorrectly(int pageNumber)
    {
        // Arrange & Act
        var spec = new PagedSpecification<TestEntity>(x => true, x => x.Id, true, pageNumber, 10);

        // Assert
        spec.PageNumber.Should().Be(pageNumber);
    }

    #endregion

    #region OrderBy Expression Tests

    [Fact]
    public void OrderByExpression_WithIntProperty_Works()
    {
        // Arrange
        Expression<Func<TestEntity, object>> orderBy = x => x.Id;
        var spec = new PagedSpecification<TestEntity>(x => true, orderBy, true, 1, 10);

        // Act
        var compiledOrderBy = spec.OrderByExpression.Compile();
        var entity = new TestEntity { Id = 42 };
        var result = compiledOrderBy(entity);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void OrderByExpression_WithStringProperty_Works()
    {
        // Arrange
        Expression<Func<TestEntity, object>> orderBy = x => x.Name!;
        var spec = new PagedSpecification<TestEntity>(x => true, orderBy, true, 1, 10);

        // Act
        var compiledOrderBy = spec.OrderByExpression.Compile();
        var entity = new TestEntity { Name = "TestName" };
        var result = compiledOrderBy(entity);

        // Assert
        result.Should().Be("TestName");
    }

    [Fact]
    public void OrderByExpression_WithBoolProperty_Works()
    {
        // Arrange
        Expression<Func<TestEntity, object>> orderBy = x => x.IsActive;
        var spec = new PagedSpecification<TestEntity>(x => true, orderBy, true, 1, 10);

        // Act
        var compiledOrderBy = spec.OrderByExpression.Compile();
        var entity = new TestEntity { IsActive = true };
        var result = compiledOrderBy(entity);

        // Assert
        result.Should().Be(true);
    }

    #endregion

    #region Test Helper Classes

    public class TestEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion
}
