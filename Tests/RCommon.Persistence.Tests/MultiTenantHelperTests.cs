using FluentAssertions;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class MultiTenantHelperTests
{
    [Fact]
    public void IsMultiTenant_WithIMultiTenantEntity_ReturnsTrue()
    {
        // Act & Assert
        MultiTenantHelper.IsMultiTenant<MultiTenantTestEntity>().Should().BeTrue();
    }

    [Fact]
    public void IsMultiTenant_WithNonIMultiTenantEntity_ReturnsFalse()
    {
        // Act & Assert
        MultiTenantHelper.IsMultiTenant<NonMultiTenantTestEntity>().Should().BeFalse();
    }

    [Fact]
    public void EnsureMultiTenant_WithIMultiTenantEntity_DoesNotThrow()
    {
        // Arrange & Act
        var action = MultiTenantHelper.EnsureMultiTenant<MultiTenantTestEntity>;

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureMultiTenant_WithNonIMultiTenantEntity_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        var action = MultiTenantHelper.EnsureMultiTenant<NonMultiTenantTestEntity>;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*NonMultiTenantTestEntity*does not implement IMultiTenant*");
    }

    [Fact]
    public void SetTenantIdIfApplicable_OnMultiTenantEntity_SetsTenantId()
    {
        // Arrange
        var entity = new MultiTenantTestEntity { TenantId = null };

        // Act
        MultiTenantHelper.SetTenantIdIfApplicable(entity, "tenant-1");

        // Assert
        entity.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public void SetTenantIdIfApplicable_OnMultiTenantEntity_WithNullTenantId_DoesNotSet()
    {
        // Arrange
        var entity = new MultiTenantTestEntity { TenantId = "original" };

        // Act
        MultiTenantHelper.SetTenantIdIfApplicable(entity, null);

        // Assert
        entity.TenantId.Should().Be("original");
    }

    [Fact]
    public void SetTenantIdIfApplicable_OnMultiTenantEntity_WithEmptyTenantId_DoesNotSet()
    {
        // Arrange
        var entity = new MultiTenantTestEntity { TenantId = "original" };

        // Act
        MultiTenantHelper.SetTenantIdIfApplicable(entity, "");

        // Assert
        entity.TenantId.Should().Be("original");
    }

    [Fact]
    public void SetTenantIdIfApplicable_OnNonMultiTenantEntity_DoesNothing()
    {
        // Arrange
        var entity = new NonMultiTenantTestEntity { Name = "Test" };

        // Act — should be a no-op, not throw
        var action = () => MultiTenantHelper.SetTenantIdIfApplicable(entity, "tenant-1");

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void GetTenantFilter_FiltersEntitiesByTenantId()
    {
        // Arrange
        var entities = new List<MultiTenantTestEntity>
        {
            new() { Name = "T1-A", TenantId = "tenant-1" },
            new() { Name = "T2-A", TenantId = "tenant-2" },
            new() { Name = "T1-B", TenantId = "tenant-1" }
        };

        // Act
        var filter = MultiTenantHelper.GetTenantFilter<MultiTenantTestEntity>("tenant-1");
        var filtered = entities.AsQueryable().Where(filter).ToList();

        // Assert
        filtered.Should().HaveCount(2);
        filtered.Should().OnlyContain(e => e.TenantId == "tenant-1");
    }

    [Fact]
    public void GetTenantFilter_ExcludesEntitiesWithDifferentTenantId()
    {
        // Arrange
        var entities = new List<MultiTenantTestEntity>
        {
            new() { Name = "T1", TenantId = "tenant-1" },
            new() { Name = "T2", TenantId = "tenant-2" }
        };

        // Act
        var filter = MultiTenantHelper.GetTenantFilter<MultiTenantTestEntity>("tenant-2");
        var filtered = entities.AsQueryable().Where(filter).ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].Name.Should().Be("T2");
    }

    [Fact]
    public void GetTenantFilter_ExcludesEntitiesWithNullTenantId()
    {
        // Arrange
        var entities = new List<MultiTenantTestEntity>
        {
            new() { Name = "WithTenant", TenantId = "tenant-1" },
            new() { Name = "NoTenant", TenantId = null }
        };

        // Act
        var filter = MultiTenantHelper.GetTenantFilter<MultiTenantTestEntity>("tenant-1");
        var filtered = entities.AsQueryable().Where(filter).ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].Name.Should().Be("WithTenant");
    }

    [Fact]
    public void CombineWithTenantFilter_OnMultiTenantEntity_AddsTenantClause()
    {
        // Arrange
        var entities = new List<MultiTenantTestEntity>
        {
            new() { Name = "Match", TenantId = "tenant-1" },
            new() { Name = "Match", TenantId = "tenant-2" },
            new() { Name = "NoMatch", TenantId = "tenant-1" }
        };
        Expression<Func<MultiTenantTestEntity, bool>> expression = e => e.Name == "Match";

        // Act
        var combined = MultiTenantHelper.CombineWithTenantFilter(expression, "tenant-1");
        var filtered = entities.AsQueryable().Where(combined).ToList();

        // Assert — only matching entity in tenant-1
        filtered.Should().HaveCount(1);
        filtered[0].TenantId.Should().Be("tenant-1");
        filtered[0].Name.Should().Be("Match");
    }

    [Fact]
    public void CombineWithTenantFilter_OnNonMultiTenantEntity_ReturnsOriginalExpression()
    {
        // Arrange
        Expression<Func<NonMultiTenantTestEntity, bool>> expression = e => e.Name == "Test";

        // Act
        var result = MultiTenantHelper.CombineWithTenantFilter(expression, "tenant-1");

        // Assert — should return the exact same expression (no filter added)
        result.Should().BeSameAs(expression);
    }

    [Fact]
    public void CombineWithTenantFilter_WithNullTenantId_ReturnsOriginalExpression()
    {
        // Arrange
        Expression<Func<MultiTenantTestEntity, bool>> expression = e => e.Name == "Test";

        // Act
        var result = MultiTenantHelper.CombineWithTenantFilter(expression, null);

        // Assert — should return the exact same expression when no tenant context
        result.Should().BeSameAs(expression);
    }

    [Fact]
    public void CombineWithTenantFilter_WithEmptyTenantId_ReturnsOriginalExpression()
    {
        // Arrange
        Expression<Func<MultiTenantTestEntity, bool>> expression = e => e.Name == "Test";

        // Act
        var result = MultiTenantHelper.CombineWithTenantFilter(expression, "");

        // Assert — should return the exact same expression when empty tenant
        result.Should().BeSameAs(expression);
    }
}

/// <summary>
/// Test entity that implements IMultiTenant for multitenancy testing.
/// </summary>
public class MultiTenantTestEntity : BusinessEntity<Guid>, IMultiTenant
{
    public string? Name { get; set; }
    public string? TenantId { get; set; }

    public MultiTenantTestEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public MultiTenantTestEntity(Guid id) : base(id)
    {
    }
}

/// <summary>
/// Test entity that does NOT implement IMultiTenant for negative multitenancy testing.
/// </summary>
public class NonMultiTenantTestEntity : BusinessEntity<Guid>
{
    public string? Name { get; set; }

    public NonMultiTenantTestEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public NonMultiTenantTestEntity(Guid id) : base(id)
    {
    }
}
