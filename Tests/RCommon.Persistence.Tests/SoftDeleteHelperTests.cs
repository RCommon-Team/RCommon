using FluentAssertions;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class SoftDeleteHelperTests
{
    [Fact]
    public void IsSoftDeletable_WithISoftDeleteEntity_ReturnsTrue()
    {
        // Act & Assert
        SoftDeleteHelper.IsSoftDeletable<SoftDeletableTestEntity>().Should().BeTrue();
    }

    [Fact]
    public void IsSoftDeletable_WithNonISoftDeleteEntity_ReturnsFalse()
    {
        // Act & Assert
        SoftDeleteHelper.IsSoftDeletable<NonSoftDeletableTestEntity>().Should().BeFalse();
    }

    [Fact]
    public void EnsureSoftDeletable_WithISoftDeleteEntity_DoesNotThrow()
    {
        // Arrange & Act
        var action = SoftDeleteHelper.EnsureSoftDeletable<SoftDeletableTestEntity>;

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureSoftDeletable_WithNonISoftDeleteEntity_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        var action = SoftDeleteHelper.EnsureSoftDeletable<NonSoftDeletableTestEntity>;

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*NonSoftDeletableTestEntity*does not implement ISoftDelete*");
    }

    [Fact]
    public void MarkAsDeleted_SetsIsDeletedToTrue()
    {
        // Arrange
        var entity = new SoftDeletableTestEntity { IsDeleted = false };

        // Act
        SoftDeleteHelper.MarkAsDeleted(entity);

        // Assert
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void MarkAsDeleted_WhenAlreadyDeleted_RemainsTrue()
    {
        // Arrange
        var entity = new SoftDeletableTestEntity { IsDeleted = true };

        // Act
        SoftDeleteHelper.MarkAsDeleted(entity);

        // Assert
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void MarkAsDeleted_WithNonISoftDeleteEntity_ThrowsInvalidCastException()
    {
        // Arrange
        var entity = new NonSoftDeletableTestEntity();

        // Act
        var action = () => SoftDeleteHelper.MarkAsDeleted(entity);

        // Assert
        action.Should().Throw<InvalidCastException>();
    }

    [Fact]
    public void GetNotDeletedFilter_ExcludesDeletedEntities()
    {
        // Arrange
        var entities = new List<SoftDeletableTestEntity>
        {
            new() { Name = "Active", IsDeleted = false },
            new() { Name = "Deleted", IsDeleted = true }
        };

        // Act
        var filter = SoftDeleteHelper.GetNotDeletedFilter<SoftDeletableTestEntity>();
        var filtered = entities.AsQueryable().Where(filter).ToList();

        // Assert
        filtered.Should().HaveCount(1);
        filtered[0].Name.Should().Be("Active");
    }

    [Fact]
    public void CombineWithNotDeletedFilter_OnSoftDeletableEntity_AddsNotDeletedClause()
    {
        // Arrange
        var entities = new List<SoftDeletableTestEntity>
        {
            new() { Name = "Active-Match", IsDeleted = false },
            new() { Name = "Deleted-Match", IsDeleted = true },
            new() { Name = "Active-NoMatch", IsDeleted = false }
        };
        Expression<Func<SoftDeletableTestEntity, bool>> expression = e => e.Name!.Contains("Match");

        // Act
        var combined = SoftDeleteHelper.CombineWithNotDeletedFilter(expression);
        var filtered = entities.AsQueryable().Where(combined).ToList();

        // Assert — only the active entities matching "Match" should be returned
        filtered.Should().HaveCount(2);
        filtered.Should().OnlyContain(e => !e.IsDeleted);
    }

    [Fact]
    public void CombineWithNotDeletedFilter_OnNonSoftDeletableEntity_ReturnsOriginalExpression()
    {
        // Arrange
        Expression<Func<NonSoftDeletableTestEntity, bool>> expression = e => e.Name == "Test";

        // Act
        var result = SoftDeleteHelper.CombineWithNotDeletedFilter(expression);

        // Assert — should return the exact same expression (no filter added)
        result.Should().BeSameAs(expression);
    }
}

/// <summary>
/// Test entity that implements ISoftDelete for soft-delete testing.
/// </summary>
public class SoftDeletableTestEntity : BusinessEntity<Guid>, ISoftDelete
{
    public string? Name { get; set; }
    public bool IsDeleted { get; set; }

    public SoftDeletableTestEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public SoftDeletableTestEntity(Guid id) : base(id)
    {
    }
}

/// <summary>
/// Test entity that does NOT implement ISoftDelete for negative soft-delete testing.
/// </summary>
public class NonSoftDeletableTestEntity : BusinessEntity<Guid>
{
    public string? Name { get; set; }

    public NonSoftDeletableTestEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public NonSoftDeletableTestEntity(Guid id) : base(id)
    {
    }
}
