using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Collections;
using RCommon.Entities;
using RCommon.Persistence.Crud;
using RCommon.Security.Claims;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Tests;

/// <summary>
/// Tests soft-delete behavior through concrete repository implementations that follow
/// the same pattern as EFCoreRepository, DapperRepository, and Linq2DbRepository.
/// </summary>
public class SoftDeleteLinqRepositoryTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public SoftDeleteLinqRepositoryTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        _defaultOptions = new DefaultDataStoreOptions();

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
    }

    // --- Auto-detection tests (parameterless DeleteAsync/DeleteManyAsync) ---

    [Fact]
    public async Task DeleteAsync_PlainCall_OnISoftDeleteEntity_AutoSoftDeletes()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test", IsDeleted = false };
        await repository.AddAsync(entity);

        // Act — plain DeleteAsync without isSoftDelete parameter
        await repository.DeleteAsync(entity);

        // Assert — entity is soft-deleted (IsDeleted=true) and hidden from filtered queries
        entity.IsDeleted.Should().BeTrue();
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_PlainCall_OnNonISoftDeleteEntity_PhysicallyDeletes()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act — plain DeleteAsync on non-ISoftDelete entity
        await repository.DeleteAsync(entity);

        // Assert — entity is physically removed
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_PlainCall_OnISoftDeleteEntity_AutoSoftDeletes()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity1 = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        var entity2 = new SoftDeletableTestEntity { Name = "NoMatch", IsDeleted = false };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Act — plain DeleteManyAsync without isSoftDelete parameter
        var count = await repository.DeleteManyAsync(e => e.Name == "Match");

        // Assert — matching entity is soft-deleted and hidden from queries, non-matching is untouched
        count.Should().Be(1);
        entity1.IsDeleted.Should().BeTrue();
        entity2.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_PlainCall_OnNonISoftDeleteEntity_PhysicallyDeletes()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act — plain DeleteManyAsync on non-ISoftDelete entity
        var count = await repository.DeleteManyAsync(e => e.Name == "Test");

        // Assert — entity is physically removed
        count.Should().Be(1);
        var remaining = await repository.GetCountAsync(e => true);
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_ExplicitHardDelete_OnISoftDeleteEntity_ForcesPhysicalDelete()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test", IsDeleted = false };
        await repository.AddAsync(entity);

        // Act — explicit isSoftDelete: false bypasses auto-detection
        await repository.DeleteAsync(entity, isSoftDelete: false);

        // Assert — entity is physically removed despite implementing ISoftDelete
        entity.IsDeleted.Should().BeFalse();
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_ExplicitHardDelete_OnISoftDeleteEntity_ForcesPhysicalDelete()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity1 = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        var entity2 = new SoftDeletableTestEntity { Name = "NoMatch", IsDeleted = false };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Act — explicit isSoftDelete: false bypasses auto-detection
        var count = await repository.DeleteManyAsync(e => e.Name == "Match", isSoftDelete: false);

        // Assert — matching entity is physically removed
        count.Should().Be(1);
        entity1.IsDeleted.Should().BeFalse();
        var remaining = await repository.GetCountAsync(e => true);
        remaining.Should().Be(1);
    }

    // --- DeleteAsync(entity, isSoftDelete) tests ---

    [Fact]
    public async Task DeleteAsync_WithSoftDelete_SetsIsDeletedTrue()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test", IsDeleted = false };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity, isSoftDelete: true);

        // Assert
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithSoftDelete_HidesEntityFromQueries()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test", IsDeleted = false };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity, isSoftDelete: true);

        // Assert — entity is soft-deleted and hidden from filtered queries
        entity.IsDeleted.Should().BeTrue();
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithHardDelete_RemovesEntityFromStore()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity, isSoftDelete: false);

        // Assert
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithSoftDelete_OnNonISoftDeleteEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        var action = async () => await repository.DeleteAsync(entity, isSoftDelete: true);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*NonSoftDeletableTestEntity*does not implement ISoftDelete*");
    }

    [Fact]
    public async Task DeleteAsync_WithHardDelete_OnNonISoftDeleteEntity_Succeeds()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity, isSoftDelete: false);

        // Assert — entity is physically removed
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    // --- DeleteManyAsync(expression, isSoftDelete) tests ---

    [Fact]
    public async Task DeleteManyAsync_WithSoftDelete_MarksAllMatchingAsDeleted()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity1 = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        var entity2 = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        var entity3 = new SoftDeletableTestEntity { Name = "NoMatch", IsDeleted = false };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);
        await repository.AddAsync(entity3);

        // Act
        var count = await repository.DeleteManyAsync(e => e.Name == "Match", isSoftDelete: true);

        // Assert
        count.Should().Be(2);
        entity1.IsDeleted.Should().BeTrue();
        entity2.IsDeleted.Should().BeTrue();
        entity3.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_WithHardDelete_RemovesMatchingEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity1 = new SoftDeletableTestEntity { Name = "Match" };
        var entity2 = new SoftDeletableTestEntity { Name = "Match" };
        var entity3 = new SoftDeletableTestEntity { Name = "NoMatch" };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);
        await repository.AddAsync(entity3);

        // Act
        var count = await repository.DeleteManyAsync(e => e.Name == "Match", isSoftDelete: false);

        // Assert
        count.Should().Be(2);
        var remaining = await repository.GetCountAsync(e => true);
        remaining.Should().Be(1);
    }

    [Fact]
    public async Task DeleteManyAsync_WithSoftDelete_OnNonISoftDeleteEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        var action = async () => await repository.DeleteManyAsync(e => e.Name == "Test", isSoftDelete: true);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*NonSoftDeletableTestEntity*does not implement ISoftDelete*");
    }

    // --- DeleteManyAsync(specification, isSoftDelete) tests ---

    [Fact]
    public async Task DeleteManyAsync_WithSpecAndSoftDelete_DelegatesToExpressionOverload()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        await repository.AddAsync(entity);

        var mockSpec = new Mock<ISpecification<SoftDeletableTestEntity>>();
        mockSpec.Setup(s => s.Predicate).Returns(e => e.Name == "Match");

        // Act
        var count = await repository.DeleteManyAsync(mockSpec.Object, isSoftDelete: true);

        // Assert
        count.Should().Be(1);
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteManyAsync_WithSpecAndHardDelete_RemovesEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Match" };
        await repository.AddAsync(entity);

        var mockSpec = new Mock<ISpecification<SoftDeletableTestEntity>>();
        mockSpec.Setup(s => s.Predicate).Returns(e => e.Name == "Match");

        // Act
        var count = await repository.DeleteManyAsync(mockSpec.Object, isSoftDelete: false);

        // Assert
        count.Should().Be(1);
        var remaining = await repository.GetCountAsync(e => true);
        remaining.Should().Be(0);
    }

    // --- Query filtering tests ---

    [Fact]
    public async Task FindAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var active = new SoftDeletableTestEntity { Name = "Active", IsDeleted = false };
        var deleted = new SoftDeletableTestEntity { Name = "Deleted", IsDeleted = true };
        await repository.AddAsync(active);
        await repository.AddAsync(deleted);

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert — only the active entity should be returned
        results.Should().HaveCount(1);
        results.Should().OnlyContain(e => e.Name == "Active");
    }

    [Fact]
    public async Task AnyAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var deleted = new SoftDeletableTestEntity { Name = "OnlyOne", IsDeleted = true };
        await repository.AddAsync(deleted);

        // Act
        var any = await repository.AnyAsync(e => e.Name == "OnlyOne");

        // Assert — soft-deleted entity should not be found
        any.Should().BeFalse();
    }

    [Fact]
    public async Task GetCountAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var active1 = new SoftDeletableTestEntity { Name = "A", IsDeleted = false };
        var active2 = new SoftDeletableTestEntity { Name = "B", IsDeleted = false };
        var deleted = new SoftDeletableTestEntity { Name = "C", IsDeleted = true };
        await repository.AddAsync(active1);
        await repository.AddAsync(active2);
        await repository.AddAsync(deleted);

        // Act
        var count = await repository.GetCountAsync(e => true);

        // Assert — only active entities counted
        count.Should().Be(2);
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var deleted = new SoftDeletableTestEntity { Name = "Target", IsDeleted = true };
        await repository.AddAsync(deleted);

        // Act
        var result = await repository.FindSingleOrDefaultAsync(e => e.Name == "Target");

        // Assert — soft-deleted entity should not be found
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_OnNonSoftDeletable_ReturnsAllEntities()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity1 = new NonSoftDeletableTestEntity { Name = "A" };
        var entity2 = new NonSoftDeletableTestEntity { Name = "B" };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert — non-ISoftDelete entities are never filtered
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindQuery_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var active = new SoftDeletableTestEntity { Name = "Active", IsDeleted = false };
        var deleted = new SoftDeletableTestEntity { Name = "Deleted", IsDeleted = true };
        await repository.AddAsync(active);
        await repository.AddAsync(deleted);

        // Act
        var results = repository.FindQuery(e => true).ToList();

        // Assert — only the active entity should be returned
        results.Should().HaveCount(1);
        results.Should().OnlyContain(e => e.Name == "Active");
    }

    // --- Helper methods ---

    private TestSoftDeletableLinqRepository CreateSoftDeletableRepository()
    {
        return new TestSoftDeletableLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }

    private TestNonSoftDeletableLinqRepository CreateNonSoftDeletableRepository()
    {
        return new TestNonSoftDeletableLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }
}

/// <summary>
/// Tests soft-delete behavior through the SQL (micro-ORM) repository base class pattern.
/// </summary>
public class SoftDeleteSqlRepositoryTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public SoftDeleteSqlRepositoryTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        _defaultOptions = new DefaultDataStoreOptions();

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
    }

    // --- Auto-detection tests (parameterless DeleteAsync/DeleteManyAsync) ---

    [Fact]
    public async Task DeleteAsync_PlainCall_OnISoftDeleteEntity_AutoSoftDeletes()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test", IsDeleted = false };
        await repository.AddAsync(entity);

        // Act — plain DeleteAsync without isSoftDelete parameter
        await repository.DeleteAsync(entity);

        // Assert — entity is soft-deleted (IsDeleted=true) and hidden from filtered queries
        entity.IsDeleted.Should().BeTrue();
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_PlainCall_OnNonISoftDeleteEntity_PhysicallyDeletes()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity);

        // Assert — entity is physically removed
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_PlainCall_OnISoftDeleteEntity_AutoSoftDeletes()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity1 = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        var entity2 = new SoftDeletableTestEntity { Name = "NoMatch", IsDeleted = false };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Act — plain DeleteManyAsync without isSoftDelete parameter
        var count = await repository.DeleteManyAsync(e => e.Name == "Match");

        // Assert — matching entity is soft-deleted and hidden from queries, non-matching is untouched
        count.Should().Be(1);
        entity1.IsDeleted.Should().BeTrue();
        entity2.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_PlainCall_OnNonISoftDeleteEntity_PhysicallyDeletes()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        var count = await repository.DeleteManyAsync(e => e.Name == "Test");

        // Assert — entity is physically removed
        count.Should().Be(1);
        var remaining = await repository.GetCountAsync(e => true);
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task DeleteAsync_ExplicitHardDelete_OnISoftDeleteEntity_ForcesPhysicalDelete()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test", IsDeleted = false };
        await repository.AddAsync(entity);

        // Act — explicit isSoftDelete: false bypasses auto-detection
        await repository.DeleteAsync(entity, isSoftDelete: false);

        // Assert — entity is physically removed despite implementing ISoftDelete
        entity.IsDeleted.Should().BeFalse();
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_ExplicitHardDelete_OnISoftDeleteEntity_ForcesPhysicalDelete()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity1 = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        var entity2 = new SoftDeletableTestEntity { Name = "NoMatch", IsDeleted = false };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Act — explicit isSoftDelete: false bypasses auto-detection
        var count = await repository.DeleteManyAsync(e => e.Name == "Match", isSoftDelete: false);

        // Assert — matching entity is physically removed
        count.Should().Be(1);
        entity1.IsDeleted.Should().BeFalse();
        var remaining = await repository.GetCountAsync(e => true);
        remaining.Should().Be(1);
    }

    // --- DeleteAsync(entity, isSoftDelete) tests ---

    [Fact]
    public async Task DeleteAsync_WithSoftDelete_SetsIsDeletedTrue()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test", IsDeleted = false };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity, isSoftDelete: true);

        // Assert
        entity.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WithSoftDelete_OnNonISoftDeleteEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        var action = async () => await repository.DeleteAsync(entity, isSoftDelete: true);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*NonSoftDeletableTestEntity*does not implement ISoftDelete*");
    }

    [Fact]
    public async Task DeleteAsync_WithHardDelete_RemovesEntityFromStore()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity = new SoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity, isSoftDelete: false);

        // Assert
        var found = await repository.AnyAsync(e => e.Id == entity.Id);
        found.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_WithSoftDelete_MarksAllMatchingAsDeleted()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var entity1 = new SoftDeletableTestEntity { Name = "Match", IsDeleted = false };
        var entity2 = new SoftDeletableTestEntity { Name = "NoMatch", IsDeleted = false };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Act
        var count = await repository.DeleteManyAsync(e => e.Name == "Match", isSoftDelete: true);

        // Assert
        count.Should().Be(1);
        entity1.IsDeleted.Should().BeTrue();
        entity2.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteManyAsync_WithSoftDelete_OnNonISoftDeleteEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity = new NonSoftDeletableTestEntity { Name = "Test" };
        await repository.AddAsync(entity);

        // Act
        var action = async () => await repository.DeleteManyAsync(e => e.Name == "Test", isSoftDelete: true);

        // Assert
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*NonSoftDeletableTestEntity*does not implement ISoftDelete*");
    }

    // --- Query filtering tests ---

    [Fact]
    public async Task FindAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var active = new SoftDeletableTestEntity { Name = "Active", IsDeleted = false };
        var deleted = new SoftDeletableTestEntity { Name = "Deleted", IsDeleted = true };
        await repository.AddAsync(active);
        await repository.AddAsync(deleted);

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert — only the active entity should be returned
        results.Should().HaveCount(1);
        results.Should().OnlyContain(e => e.Name == "Active");
    }

    [Fact]
    public async Task AnyAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var deleted = new SoftDeletableTestEntity { Name = "OnlyOne", IsDeleted = true };
        await repository.AddAsync(deleted);

        // Act
        var any = await repository.AnyAsync(e => e.Name == "OnlyOne");

        // Assert — soft-deleted entity should not be found
        any.Should().BeFalse();
    }

    [Fact]
    public async Task GetCountAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var active1 = new SoftDeletableTestEntity { Name = "A", IsDeleted = false };
        var active2 = new SoftDeletableTestEntity { Name = "B", IsDeleted = false };
        var deleted = new SoftDeletableTestEntity { Name = "C", IsDeleted = true };
        await repository.AddAsync(active1);
        await repository.AddAsync(active2);
        await repository.AddAsync(deleted);

        // Act
        var count = await repository.GetCountAsync(e => true);

        // Assert — only active entities counted
        count.Should().Be(2);
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_ExcludesSoftDeletedEntities()
    {
        // Arrange
        var repository = CreateSoftDeletableRepository();
        var deleted = new SoftDeletableTestEntity { Name = "Target", IsDeleted = true };
        await repository.AddAsync(deleted);

        // Act
        var result = await repository.FindSingleOrDefaultAsync(e => e.Name == "Target");

        // Assert — soft-deleted entity should not be found
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_OnNonSoftDeletable_ReturnsAllEntities()
    {
        // Arrange
        var repository = CreateNonSoftDeletableRepository();
        var entity1 = new NonSoftDeletableTestEntity { Name = "A" };
        var entity2 = new NonSoftDeletableTestEntity { Name = "B" };
        await repository.AddAsync(entity1);
        await repository.AddAsync(entity2);

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert — non-ISoftDelete entities are never filtered
        results.Should().HaveCount(2);
    }

    // --- Helper methods ---

    private TestSoftDeletableSqlRepository CreateSoftDeletableRepository()
    {
        return new TestSoftDeletableSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }

    private TestNonSoftDeletableSqlRepository CreateNonSoftDeletableRepository()
    {
        return new TestNonSoftDeletableSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }
}

// ============================================================================
// Test repository implementations for SoftDeletableTestEntity (Linq-based)
// ============================================================================

/// <summary>
/// Concrete LinqRepositoryBase implementation for SoftDeletableTestEntity.
/// Mimics the soft-delete logic used by EFCoreRepository and Linq2DbRepository.
/// </summary>
public class TestSoftDeletableLinqRepository : LinqRepositoryBase<SoftDeletableTestEntity>
{
    private readonly List<SoftDeletableTestEntity> _entities = new();

    public TestSoftDeletableLinqRepository(
        IDataStoreFactory dataStoreFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    protected override IQueryable<SoftDeletableTestEntity> RepositoryQuery => _entities.AsQueryable();

    public override Task AddAsync(SoftDeletableTestEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<SoftDeletableTestEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(SoftDeletableTestEntity entity, CancellationToken token = default)
    {
        // Auto-detect: if entity implements ISoftDelete, perform soft delete automatically
        if (SoftDeleteHelper.IsSoftDeletable<SoftDeletableTestEntity>())
        {
            SoftDeleteHelper.MarkAsDeleted(entity);
            return UpdateAsync(entity, token);
        }

        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(SoftDeletableTestEntity entity, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            _entities.Remove(entity);
            return Task.CompletedTask;
        }

        SoftDeleteHelper.EnsureSoftDeletable<SoftDeletableTestEntity>();
        SoftDeleteHelper.MarkAsDeleted(entity);
        return UpdateAsync(entity, token);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        // Auto-detect: if entity implements ISoftDelete, perform soft delete automatically
        if (SoftDeleteHelper.IsSoftDeletable<SoftDeletableTestEntity>())
        {
            return DeleteManyAsync(expression, isSoftDelete: true, token);
        }

        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            var hardMatches = _entities.Where(expression.Compile()).ToList();
            foreach (var e in hardMatches) _entities.Remove(e);
            return Task.FromResult(hardMatches.Count);
        }

        SoftDeleteHelper.EnsureSoftDeletable<SoftDeletableTestEntity>();

        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var entity in matches)
        {
            SoftDeleteHelper.MarkAsDeleted(entity);
        }
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<SoftDeletableTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, isSoftDelete, token);

    public override Task UpdateAsync(SoftDeletableTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    // Read methods use FilteredRepositoryQuery (from LinqRepositoryBase) to automatically
    // exclude soft-deleted entities, mirroring the real EFCoreRepository/Linq2DbRepository behavior.

    public override Task<ICollection<SoftDeletableTestEntity>> FindAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<SoftDeletableTestEntity>>(FilteredRepositoryQuery.Where(specification.Predicate).ToList());

    public override Task<ICollection<SoftDeletableTestEntity>> FindAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<SoftDeletableTestEntity>>(FilteredRepositoryQuery.Where(expression).ToList());

    public override Task<SoftDeletableTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
    {
        // Mimics EFCore FindAsync(pk) post-fetch soft-delete check
        var entity = _entities.FirstOrDefault();
        if (entity != null && SoftDeleteHelper.IsSoftDeletable<SoftDeletableTestEntity>() && ((ISoftDelete)entity).IsDeleted)
            return Task.FromResult<SoftDeletableTestEntity>(default!);
        return Task.FromResult(entity!);
    }

    public override Task<long> GetCountAsync(ISpecification<SoftDeletableTestEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)FilteredRepositoryQuery.Where(selectSpec.Predicate).Count());

    public override Task<long> GetCountAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)FilteredRepositoryQuery.Where(expression).Count());

    public override Task<SoftDeletableTestEntity> FindSingleOrDefaultAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(expression).SingleOrDefault()!);

    public override Task<SoftDeletableTestEntity> FindSingleOrDefaultAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(specification.Predicate).SingleOrDefault()!);

    public override Task<bool> AnyAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(expression).Any());

    public override Task<bool> AnyAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(specification.Predicate).Any());

    public override IQueryable<SoftDeletableTestEntity> FindQuery(ISpecification<SoftDeletableTestEntity> specification)
        => FilteredRepositoryQuery.Where(specification.Predicate);

    public override IQueryable<SoftDeletableTestEntity> FindQuery(Expression<Func<SoftDeletableTestEntity, bool>> expression)
        => FilteredRepositoryQuery.Where(expression);

    public override IQueryable<SoftDeletableTestEntity> FindQuery(Expression<Func<SoftDeletableTestEntity, bool>> expression,
        Expression<Func<SoftDeletableTestEntity, object>> orderByExpression, bool orderByAscending)
        => FilteredRepositoryQuery.Where(expression);

    public override Task<IPaginatedList<SoftDeletableTestEntity>> FindAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression,
        Expression<Func<SoftDeletableTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1,
        int pageSize = 0, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<SoftDeletableTestEntity>>(null!);

    public override Task<IPaginatedList<SoftDeletableTestEntity>> FindAsync(IPagedSpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<SoftDeletableTestEntity>>(null!);

    public override IQueryable<SoftDeletableTestEntity> FindQuery(Expression<Func<SoftDeletableTestEntity, bool>> expression,
        Expression<Func<SoftDeletableTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        => FilteredRepositoryQuery.Where(expression);

    public override IQueryable<SoftDeletableTestEntity> FindQuery(IPagedSpecification<SoftDeletableTestEntity> specification)
        => FilteredRepositoryQuery;

    public override IEagerLoadableQueryable<SoftDeletableTestEntity> Include(Expression<Func<SoftDeletableTestEntity, object>> path)
        => null!;

    public override IEagerLoadableQueryable<SoftDeletableTestEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        => null!;
}

// ============================================================================
// Test repository implementations for NonSoftDeletableTestEntity (Linq-based)
// ============================================================================

/// <summary>
/// Concrete LinqRepositoryBase implementation for NonSoftDeletableTestEntity.
/// Used to verify that soft delete throws InvalidOperationException when the entity
/// does not implement ISoftDelete.
/// </summary>
public class TestNonSoftDeletableLinqRepository : LinqRepositoryBase<NonSoftDeletableTestEntity>
{
    private readonly List<NonSoftDeletableTestEntity> _entities = new();

    public TestNonSoftDeletableLinqRepository(
        IDataStoreFactory dataStoreFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    protected override IQueryable<NonSoftDeletableTestEntity> RepositoryQuery => _entities.AsQueryable();

    public override Task AddAsync(NonSoftDeletableTestEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<NonSoftDeletableTestEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonSoftDeletableTestEntity entity, CancellationToken token = default)
    {
        // Auto-detect: ISoftDelete check returns false, so physical delete
        if (SoftDeleteHelper.IsSoftDeletable<NonSoftDeletableTestEntity>())
        {
            SoftDeleteHelper.MarkAsDeleted(entity);
            return UpdateAsync(entity, token);
        }

        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonSoftDeletableTestEntity entity, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            _entities.Remove(entity);
            return Task.CompletedTask;
        }

        SoftDeleteHelper.EnsureSoftDeletable<NonSoftDeletableTestEntity>();
        SoftDeleteHelper.MarkAsDeleted(entity);
        return UpdateAsync(entity, token);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        // Auto-detect: ISoftDelete check returns false, so physical delete
        if (SoftDeleteHelper.IsSoftDeletable<NonSoftDeletableTestEntity>())
        {
            return DeleteManyAsync(expression, isSoftDelete: true, token);
        }

        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            var hardMatches = _entities.Where(expression.Compile()).ToList();
            foreach (var e in hardMatches) _entities.Remove(e);
            return Task.FromResult(hardMatches.Count);
        }

        SoftDeleteHelper.EnsureSoftDeletable<NonSoftDeletableTestEntity>();
        return Task.FromResult(0);
    }

    public override Task<int> DeleteManyAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<NonSoftDeletableTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, isSoftDelete, token);

    public override Task UpdateAsync(NonSoftDeletableTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    public override Task<ICollection<NonSoftDeletableTestEntity>> FindAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<NonSoftDeletableTestEntity>>(_entities.Where(specification.Predicate.Compile()).ToList());

    public override Task<ICollection<NonSoftDeletableTestEntity>> FindAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<NonSoftDeletableTestEntity>>(_entities.Where(expression.Compile()).ToList());

    public override Task<NonSoftDeletableTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<NonSoftDeletableTestEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(selectSpec.Predicate.Compile()));

    public override Task<long> GetCountAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(expression.Compile()));

    public override Task<NonSoftDeletableTestEntity> FindSingleOrDefaultAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(expression.Compile())!);

    public override Task<NonSoftDeletableTestEntity> FindSingleOrDefaultAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(specification.Predicate.Compile())!);

    public override Task<bool> AnyAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.Any(expression.Compile()));

    public override Task<bool> AnyAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.Any(specification.Predicate.Compile()));

    public override IQueryable<NonSoftDeletableTestEntity> FindQuery(ISpecification<NonSoftDeletableTestEntity> specification)
        => _entities.AsQueryable().Where(specification.Predicate);

    public override IQueryable<NonSoftDeletableTestEntity> FindQuery(Expression<Func<NonSoftDeletableTestEntity, bool>> expression)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<NonSoftDeletableTestEntity> FindQuery(Expression<Func<NonSoftDeletableTestEntity, bool>> expression,
        Expression<Func<NonSoftDeletableTestEntity, object>> orderByExpression, bool orderByAscending)
        => _entities.AsQueryable().Where(expression);

    public override Task<IPaginatedList<NonSoftDeletableTestEntity>> FindAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression,
        Expression<Func<NonSoftDeletableTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1,
        int pageSize = 0, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<NonSoftDeletableTestEntity>>(null!);

    public override Task<IPaginatedList<NonSoftDeletableTestEntity>> FindAsync(IPagedSpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<NonSoftDeletableTestEntity>>(null!);

    public override IQueryable<NonSoftDeletableTestEntity> FindQuery(Expression<Func<NonSoftDeletableTestEntity, bool>> expression,
        Expression<Func<NonSoftDeletableTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<NonSoftDeletableTestEntity> FindQuery(IPagedSpecification<NonSoftDeletableTestEntity> specification)
        => _entities.AsQueryable();

    public override IEagerLoadableQueryable<NonSoftDeletableTestEntity> Include(Expression<Func<NonSoftDeletableTestEntity, object>> path)
        => null!;

    public override IEagerLoadableQueryable<NonSoftDeletableTestEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        => null!;
}

// ============================================================================
// Test repository implementations for SqlRepositoryBase (SoftDeletable)
// ============================================================================

/// <summary>
/// Concrete SqlRepositoryBase implementation for SoftDeletableTestEntity.
/// Mimics the soft-delete logic used by DapperRepository.
/// </summary>
public class TestSoftDeletableSqlRepository : SqlRepositoryBase<SoftDeletableTestEntity>
{
    private readonly List<SoftDeletableTestEntity> _entities = new();

    public TestSoftDeletableSqlRepository(
        IDataStoreFactory dataStoreFactory,
        ILoggerFactory loggerFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, loggerFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    public override Task AddAsync(SoftDeletableTestEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<SoftDeletableTestEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(SoftDeletableTestEntity entity, CancellationToken token = default)
    {
        // Auto-detect: if entity implements ISoftDelete, perform soft delete automatically
        if (SoftDeleteHelper.IsSoftDeletable<SoftDeletableTestEntity>())
        {
            SoftDeleteHelper.MarkAsDeleted(entity);
            return UpdateAsync(entity, token);
        }

        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(SoftDeletableTestEntity entity, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            _entities.Remove(entity);
            return Task.CompletedTask;
        }

        SoftDeleteHelper.EnsureSoftDeletable<SoftDeletableTestEntity>();
        SoftDeleteHelper.MarkAsDeleted(entity);
        return UpdateAsync(entity, token);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        // Auto-detect: if entity implements ISoftDelete, perform soft delete automatically
        if (SoftDeleteHelper.IsSoftDeletable<SoftDeletableTestEntity>())
        {
            return DeleteManyAsync(expression, isSoftDelete: true, token);
        }

        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            var hardMatches = _entities.Where(expression.Compile()).ToList();
            foreach (var e in hardMatches) _entities.Remove(e);
            return Task.FromResult(hardMatches.Count);
        }

        SoftDeleteHelper.EnsureSoftDeletable<SoftDeletableTestEntity>();

        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var entity in matches)
        {
            SoftDeleteHelper.MarkAsDeleted(entity);
        }
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<SoftDeletableTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, isSoftDelete, token);

    public override Task UpdateAsync(SoftDeletableTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    // Read methods wrap expressions with CombineWithNotDeletedFilter to automatically
    // exclude soft-deleted entities, mirroring the real DapperRepository behavior.

    public override Task<ICollection<SoftDeletableTestEntity>> FindAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(specification.Predicate);
        return Task.FromResult<ICollection<SoftDeletableTestEntity>>(_entities.Where(filtered.Compile()).ToList());
    }

    public override Task<ICollection<SoftDeletableTestEntity>> FindAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(expression);
        return Task.FromResult<ICollection<SoftDeletableTestEntity>>(_entities.Where(filtered.Compile()).ToList());
    }

    public override Task<SoftDeletableTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
    {
        // Mimics DapperRepository FindAsync(pk) post-fetch soft-delete check
        var entity = _entities.FirstOrDefault();
        if (entity != null && SoftDeleteHelper.IsSoftDeletable<SoftDeletableTestEntity>() && ((ISoftDelete)entity).IsDeleted)
            return Task.FromResult<SoftDeletableTestEntity>(default!);
        return Task.FromResult(entity!);
    }

    public override Task<long> GetCountAsync(ISpecification<SoftDeletableTestEntity> selectSpec, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(selectSpec.Predicate);
        return Task.FromResult((long)_entities.Count(filtered.Compile()));
    }

    public override Task<long> GetCountAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(expression);
        return Task.FromResult((long)_entities.Count(filtered.Compile()));
    }

    public override Task<SoftDeletableTestEntity> FindSingleOrDefaultAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(expression);
        return Task.FromResult(_entities.SingleOrDefault(filtered.Compile())!);
    }

    public override Task<SoftDeletableTestEntity> FindSingleOrDefaultAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(specification.Predicate);
        return Task.FromResult(_entities.SingleOrDefault(filtered.Compile())!);
    }

    public override Task<bool> AnyAsync(Expression<Func<SoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(expression);
        return Task.FromResult(_entities.Any(filtered.Compile()));
    }

    public override Task<bool> AnyAsync(ISpecification<SoftDeletableTestEntity> specification, CancellationToken token = default)
    {
        var filtered = SoftDeleteHelper.CombineWithNotDeletedFilter(specification.Predicate);
        return Task.FromResult(_entities.Any(filtered.Compile()));
    }
}

// ============================================================================
// Test repository implementations for SqlRepositoryBase (NonSoftDeletable)
// ============================================================================

/// <summary>
/// Concrete SqlRepositoryBase implementation for NonSoftDeletableTestEntity.
/// Used to verify that soft delete throws InvalidOperationException when the entity
/// does not implement ISoftDelete.
/// </summary>
public class TestNonSoftDeletableSqlRepository : SqlRepositoryBase<NonSoftDeletableTestEntity>
{
    private readonly List<NonSoftDeletableTestEntity> _entities = new();

    public TestNonSoftDeletableSqlRepository(
        IDataStoreFactory dataStoreFactory,
        ILoggerFactory loggerFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, loggerFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    public override Task AddAsync(NonSoftDeletableTestEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<NonSoftDeletableTestEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonSoftDeletableTestEntity entity, CancellationToken token = default)
    {
        // Auto-detect: ISoftDelete check returns false, so physical delete
        if (SoftDeleteHelper.IsSoftDeletable<NonSoftDeletableTestEntity>())
        {
            SoftDeleteHelper.MarkAsDeleted(entity);
            return UpdateAsync(entity, token);
        }

        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonSoftDeletableTestEntity entity, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            _entities.Remove(entity);
            return Task.CompletedTask;
        }

        SoftDeleteHelper.EnsureSoftDeletable<NonSoftDeletableTestEntity>();
        SoftDeleteHelper.MarkAsDeleted(entity);
        return UpdateAsync(entity, token);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
    {
        // Auto-detect: ISoftDelete check returns false, so physical delete
        if (SoftDeleteHelper.IsSoftDeletable<NonSoftDeletableTestEntity>())
        {
            return DeleteManyAsync(expression, isSoftDelete: true, token);
        }

        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
    {
        if (!isSoftDelete)
        {
            // Bypass auto-detection — force a physical delete
            var hardMatches = _entities.Where(expression.Compile()).ToList();
            foreach (var e in hardMatches) _entities.Remove(e);
            return Task.FromResult(hardMatches.Count);
        }

        SoftDeleteHelper.EnsureSoftDeletable<NonSoftDeletableTestEntity>();
        return Task.FromResult(0);
    }

    public override Task<int> DeleteManyAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<NonSoftDeletableTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, isSoftDelete, token);

    public override Task UpdateAsync(NonSoftDeletableTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    public override Task<ICollection<NonSoftDeletableTestEntity>> FindAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<NonSoftDeletableTestEntity>>(_entities.Where(specification.Predicate.Compile()).ToList());

    public override Task<ICollection<NonSoftDeletableTestEntity>> FindAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<NonSoftDeletableTestEntity>>(_entities.Where(expression.Compile()).ToList());

    public override Task<NonSoftDeletableTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<NonSoftDeletableTestEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(selectSpec.Predicate.Compile()));

    public override Task<long> GetCountAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(expression.Compile()));

    public override Task<NonSoftDeletableTestEntity> FindSingleOrDefaultAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(expression.Compile())!);

    public override Task<NonSoftDeletableTestEntity> FindSingleOrDefaultAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(specification.Predicate.Compile())!);

    public override Task<bool> AnyAsync(Expression<Func<NonSoftDeletableTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.Any(expression.Compile()));

    public override Task<bool> AnyAsync(ISpecification<NonSoftDeletableTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.Any(specification.Predicate.Compile()));
}
