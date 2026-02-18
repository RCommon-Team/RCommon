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
/// Tests multitenancy behavior through a Linq-based repository (FilteredRepositoryQuery pattern).
/// Verifies tenant stamping on add and automatic tenant filtering on reads.
/// </summary>
public class MultiTenantLinqRepositoryTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public MultiTenantLinqRepositoryTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        _defaultOptions = new DefaultDataStoreOptions();

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
    }

    // --- AddAsync tenant stamping ---

    [Fact]
    public async Task AddAsync_WithTenantConfigured_SetsTenantId()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        var entity = new MultiTenantTestEntity { Name = "Test", TenantId = null };

        // Act
        await repository.AddAsync(entity);

        // Assert
        entity.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public async Task AddAsync_WithNoTenantConfigured_DoesNotSetTenantId()
    {
        // Arrange — NullTenantIdAccessor returns null
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns((string?)null);
        var repository = CreateMultiTenantRepository();
        var entity = new MultiTenantTestEntity { Name = "Test", TenantId = null };

        // Act
        await repository.AddAsync(entity);

        // Assert
        entity.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task AddRangeAsync_WithTenantConfigured_SetsTenantIdOnAllEntities()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        var entities = new List<MultiTenantTestEntity>
        {
            new() { Name = "A" },
            new() { Name = "B" }
        };

        // Act
        await repository.AddRangeAsync(entities);

        // Assert
        entities.Should().OnlyContain(e => e.TenantId == "tenant-1");
    }

    [Fact]
    public async Task AddAsync_OnNonMultiTenantEntity_DoesNotThrow()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateNonMultiTenantRepository();
        var entity = new NonMultiTenantTestEntity { Name = "Test" };

        // Act — should not throw even when tenant is configured
        await repository.AddAsync(entity);

        // Assert
        entity.Name.Should().Be("Test");
    }

    // --- Read filtering ---

    [Fact]
    public async Task FindAsync_WithTenantConfigured_FiltersToCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T1", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T2", TenantId = "tenant-2" });

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert — only tenant-1 entities returned
        results.Should().HaveCount(1);
        results.Should().OnlyContain(e => e.TenantId == "tenant-1");
    }

    [Fact]
    public async Task FindAsync_WithNoTenantConfigured_ReturnsAllEntities()
    {
        // Arrange — NullTenantIdAccessor returns null
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns((string?)null);
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T1", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T2", TenantId = "tenant-2" });

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert — all entities returned when no tenant context
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnyAsync_WithTenantConfigured_FiltersToCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "Test", TenantId = "tenant-2" });

        // Act
        var any = await repository.AnyAsync(e => e.Name == "Test");

        // Assert — entity belongs to tenant-2, should not be visible to tenant-1
        any.Should().BeFalse();
    }

    [Fact]
    public async Task GetCountAsync_WithTenantConfigured_CountsOnlyCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "A", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "B", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "C", TenantId = "tenant-2" });

        // Act
        var count = await repository.GetCountAsync(e => true);

        // Assert — only tenant-1 entities counted
        count.Should().Be(2);
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithTenantConfigured_FiltersToCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "Target", TenantId = "tenant-2" });

        // Act
        var result = await repository.FindSingleOrDefaultAsync(e => e.Name == "Target");

        // Assert — entity belongs to tenant-2, should not be found by tenant-1
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindQuery_WithTenantConfigured_FiltersToCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T1", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T2", TenantId = "tenant-2" });

        // Act
        var results = repository.FindQuery(e => true).ToList();

        // Assert
        results.Should().HaveCount(1);
        results.Should().OnlyContain(e => e.TenantId == "tenant-1");
    }

    [Fact]
    public async Task FindAsync_OnNonMultiTenantEntity_ReturnsAllEntities()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateNonMultiTenantRepository();
        await repository.AddAsync(new NonMultiTenantTestEntity { Name = "A" });
        await repository.AddAsync(new NonMultiTenantTestEntity { Name = "B" });

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert — non-IMultiTenant entities are never filtered
        results.Should().HaveCount(2);
    }

    // --- Helper methods ---

    private TestMultiTenantLinqRepository CreateMultiTenantRepository()
    {
        return new TestMultiTenantLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }

    private TestNonMultiTenantLinqRepository CreateNonMultiTenantRepository()
    {
        return new TestNonMultiTenantLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }
}

/// <summary>
/// Tests multitenancy behavior through a SQL-based repository (CombineWithTenantFilter pattern).
/// </summary>
public class MultiTenantSqlRepositoryTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public MultiTenantSqlRepositoryTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        _defaultOptions = new DefaultDataStoreOptions();

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
    }

    // --- AddAsync tenant stamping ---

    [Fact]
    public async Task AddAsync_WithTenantConfigured_SetsTenantId()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        var entity = new MultiTenantTestEntity { Name = "Test", TenantId = null };

        // Act
        await repository.AddAsync(entity);

        // Assert
        entity.TenantId.Should().Be("tenant-1");
    }

    [Fact]
    public async Task AddAsync_WithNoTenantConfigured_DoesNotSetTenantId()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns((string?)null);
        var repository = CreateMultiTenantRepository();
        var entity = new MultiTenantTestEntity { Name = "Test", TenantId = null };

        // Act
        await repository.AddAsync(entity);

        // Assert
        entity.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task AddRangeAsync_WithTenantConfigured_SetsTenantIdOnAllEntities()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        var entities = new List<MultiTenantTestEntity>
        {
            new() { Name = "A" },
            new() { Name = "B" }
        };

        // Act
        await repository.AddRangeAsync(entities);

        // Assert
        entities.Should().OnlyContain(e => e.TenantId == "tenant-1");
    }

    // --- Read filtering ---

    [Fact]
    public async Task FindAsync_WithTenantConfigured_FiltersToCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T1", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T2", TenantId = "tenant-2" });

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert
        results.Should().HaveCount(1);
        results.Should().OnlyContain(e => e.TenantId == "tenant-1");
    }

    [Fact]
    public async Task FindAsync_WithNoTenantConfigured_ReturnsAllEntities()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns((string?)null);
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T1", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "T2", TenantId = "tenant-2" });

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task AnyAsync_WithTenantConfigured_FiltersToCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "Test", TenantId = "tenant-2" });

        // Act
        var any = await repository.AnyAsync(e => e.Name == "Test");

        // Assert
        any.Should().BeFalse();
    }

    [Fact]
    public async Task GetCountAsync_WithTenantConfigured_CountsOnlyCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "A", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "B", TenantId = "tenant-1" });
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "C", TenantId = "tenant-2" });

        // Act
        var count = await repository.GetCountAsync(e => true);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task FindSingleOrDefaultAsync_WithTenantConfigured_FiltersToCurrentTenant()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateMultiTenantRepository();
        await repository.AddEntityDirectly(new MultiTenantTestEntity { Name = "Target", TenantId = "tenant-2" });

        // Act
        var result = await repository.FindSingleOrDefaultAsync(e => e.Name == "Target");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_OnNonMultiTenantEntity_ReturnsAllEntities()
    {
        // Arrange
        _mockTenantIdAccessor.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var repository = CreateNonMultiTenantRepository();
        await repository.AddAsync(new NonMultiTenantTestEntity { Name = "A" });
        await repository.AddAsync(new NonMultiTenantTestEntity { Name = "B" });

        // Act
        var results = await repository.FindAsync(e => true);

        // Assert
        results.Should().HaveCount(2);
    }

    // --- Helper methods ---

    private TestMultiTenantSqlRepository CreateMultiTenantRepository()
    {
        return new TestMultiTenantSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }

    private TestNonMultiTenantSqlRepository CreateNonMultiTenantRepository()
    {
        return new TestNonMultiTenantSqlRepository(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }
}

// ============================================================================
// Test repository implementations for MultiTenantTestEntity (Linq-based)
// ============================================================================

/// <summary>
/// Concrete LinqRepositoryBase implementation for MultiTenantTestEntity.
/// Mimics tenant stamping and FilteredRepositoryQuery filtering used by EFCoreRepository.
/// </summary>
public class TestMultiTenantLinqRepository : LinqRepositoryBase<MultiTenantTestEntity>
{
    private readonly List<MultiTenantTestEntity> _entities = new();

    public TestMultiTenantLinqRepository(
        IDataStoreFactory dataStoreFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    protected override IQueryable<MultiTenantTestEntity> RepositoryQuery => _entities.AsQueryable();

    /// <summary>
    /// Adds an entity directly without tenant stamping — used to seed test data with specific TenantIds.
    /// </summary>
    public Task AddEntityDirectly(MultiTenantTestEntity entity)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddAsync(MultiTenantTestEntity entity, CancellationToken token = default)
    {
        MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<MultiTenantTestEntity> entities, CancellationToken token = default)
    {
        var tenantId = _tenantIdAccessor.GetTenantId();
        foreach (var entity in entities)
        {
            MultiTenantHelper.SetTenantIdIfApplicable(entity, tenantId);
            _entities.Add(entity);
        }
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(MultiTenantTestEntity entity, CancellationToken token = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(MultiTenantTestEntity entity, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<MultiTenantTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task UpdateAsync(MultiTenantTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    // Read methods use FilteredRepositoryQuery to automatically exclude other tenants' entities.

    public override Task<ICollection<MultiTenantTestEntity>> FindAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<MultiTenantTestEntity>>(FilteredRepositoryQuery.Where(specification.Predicate).ToList());

    public override Task<ICollection<MultiTenantTestEntity>> FindAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<MultiTenantTestEntity>>(FilteredRepositoryQuery.Where(expression).ToList());

    public override Task<MultiTenantTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<MultiTenantTestEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)FilteredRepositoryQuery.Where(selectSpec.Predicate).Count());

    public override Task<long> GetCountAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)FilteredRepositoryQuery.Where(expression).Count());

    public override Task<MultiTenantTestEntity> FindSingleOrDefaultAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(expression).SingleOrDefault()!);

    public override Task<MultiTenantTestEntity> FindSingleOrDefaultAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(specification.Predicate).SingleOrDefault()!);

    public override Task<bool> AnyAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(expression).Any());

    public override Task<bool> AnyAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(FilteredRepositoryQuery.Where(specification.Predicate).Any());

    public override IQueryable<MultiTenantTestEntity> FindQuery(ISpecification<MultiTenantTestEntity> specification)
        => FilteredRepositoryQuery.Where(specification.Predicate);

    public override IQueryable<MultiTenantTestEntity> FindQuery(Expression<Func<MultiTenantTestEntity, bool>> expression)
        => FilteredRepositoryQuery.Where(expression);

    public override IQueryable<MultiTenantTestEntity> FindQuery(Expression<Func<MultiTenantTestEntity, bool>> expression,
        Expression<Func<MultiTenantTestEntity, object>> orderByExpression, bool orderByAscending)
        => FilteredRepositoryQuery.Where(expression);

    public override Task<IPaginatedList<MultiTenantTestEntity>> FindAsync(Expression<Func<MultiTenantTestEntity, bool>> expression,
        Expression<Func<MultiTenantTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1,
        int pageSize = 0, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<MultiTenantTestEntity>>(null!);

    public override Task<IPaginatedList<MultiTenantTestEntity>> FindAsync(IPagedSpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<MultiTenantTestEntity>>(null!);

    public override IQueryable<MultiTenantTestEntity> FindQuery(Expression<Func<MultiTenantTestEntity, bool>> expression,
        Expression<Func<MultiTenantTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        => FilteredRepositoryQuery.Where(expression);

    public override IQueryable<MultiTenantTestEntity> FindQuery(IPagedSpecification<MultiTenantTestEntity> specification)
        => FilteredRepositoryQuery;

    public override IEagerLoadableQueryable<MultiTenantTestEntity> Include(Expression<Func<MultiTenantTestEntity, object>> path)
        => null!;

    public override IEagerLoadableQueryable<MultiTenantTestEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        => null!;
}

// ============================================================================
// Test repository implementations for NonMultiTenantTestEntity (Linq-based)
// ============================================================================

public class TestNonMultiTenantLinqRepository : LinqRepositoryBase<NonMultiTenantTestEntity>
{
    private readonly List<NonMultiTenantTestEntity> _entities = new();

    public TestNonMultiTenantLinqRepository(
        IDataStoreFactory dataStoreFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    protected override IQueryable<NonMultiTenantTestEntity> RepositoryQuery => _entities.AsQueryable();

    public override Task AddAsync(NonMultiTenantTestEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<NonMultiTenantTestEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonMultiTenantTestEntity entity, CancellationToken token = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonMultiTenantTestEntity entity, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<NonMultiTenantTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task UpdateAsync(NonMultiTenantTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    public override Task<ICollection<NonMultiTenantTestEntity>> FindAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<NonMultiTenantTestEntity>>(_entities.Where(specification.Predicate.Compile()).ToList());

    public override Task<ICollection<NonMultiTenantTestEntity>> FindAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<NonMultiTenantTestEntity>>(_entities.Where(expression.Compile()).ToList());

    public override Task<NonMultiTenantTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<NonMultiTenantTestEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(selectSpec.Predicate.Compile()));

    public override Task<long> GetCountAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(expression.Compile()));

    public override Task<NonMultiTenantTestEntity> FindSingleOrDefaultAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(expression.Compile())!);

    public override Task<NonMultiTenantTestEntity> FindSingleOrDefaultAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(specification.Predicate.Compile())!);

    public override Task<bool> AnyAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.Any(expression.Compile()));

    public override Task<bool> AnyAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.Any(specification.Predicate.Compile()));

    public override IQueryable<NonMultiTenantTestEntity> FindQuery(ISpecification<NonMultiTenantTestEntity> specification)
        => _entities.AsQueryable().Where(specification.Predicate);

    public override IQueryable<NonMultiTenantTestEntity> FindQuery(Expression<Func<NonMultiTenantTestEntity, bool>> expression)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<NonMultiTenantTestEntity> FindQuery(Expression<Func<NonMultiTenantTestEntity, bool>> expression,
        Expression<Func<NonMultiTenantTestEntity, object>> orderByExpression, bool orderByAscending)
        => _entities.AsQueryable().Where(expression);

    public override Task<IPaginatedList<NonMultiTenantTestEntity>> FindAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression,
        Expression<Func<NonMultiTenantTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1,
        int pageSize = 0, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<NonMultiTenantTestEntity>>(null!);

    public override Task<IPaginatedList<NonMultiTenantTestEntity>> FindAsync(IPagedSpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<NonMultiTenantTestEntity>>(null!);

    public override IQueryable<NonMultiTenantTestEntity> FindQuery(Expression<Func<NonMultiTenantTestEntity, bool>> expression,
        Expression<Func<NonMultiTenantTestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<NonMultiTenantTestEntity> FindQuery(IPagedSpecification<NonMultiTenantTestEntity> specification)
        => _entities.AsQueryable();

    public override IEagerLoadableQueryable<NonMultiTenantTestEntity> Include(Expression<Func<NonMultiTenantTestEntity, object>> path)
        => null!;

    public override IEagerLoadableQueryable<NonMultiTenantTestEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        => null!;
}

// ============================================================================
// Test repository implementations for MultiTenantTestEntity (SQL-based)
// ============================================================================

/// <summary>
/// Concrete SqlRepositoryBase implementation for MultiTenantTestEntity.
/// Mimics tenant stamping and CombineWithTenantFilter used by DapperRepository.
/// </summary>
public class TestMultiTenantSqlRepository : SqlRepositoryBase<MultiTenantTestEntity>
{
    private readonly List<MultiTenantTestEntity> _entities = new();

    public TestMultiTenantSqlRepository(
        IDataStoreFactory dataStoreFactory,
        ILoggerFactory loggerFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, loggerFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    /// <summary>
    /// Adds an entity directly without tenant stamping — used to seed test data with specific TenantIds.
    /// </summary>
    public Task AddEntityDirectly(MultiTenantTestEntity entity)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddAsync(MultiTenantTestEntity entity, CancellationToken token = default)
    {
        MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<MultiTenantTestEntity> entities, CancellationToken token = default)
    {
        var tenantId = _tenantIdAccessor.GetTenantId();
        foreach (var entity in entities)
        {
            MultiTenantHelper.SetTenantIdIfApplicable(entity, tenantId);
            _entities.Add(entity);
        }
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(MultiTenantTestEntity entity, CancellationToken token = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(MultiTenantTestEntity entity, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<MultiTenantTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task UpdateAsync(MultiTenantTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    // Read methods wrap expressions with CombineWithTenantFilter to automatically
    // exclude other tenants' entities, mirroring the real DapperRepository behavior.

    public override Task<ICollection<MultiTenantTestEntity>> FindAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(specification.Predicate, _tenantIdAccessor.GetTenantId());
        return Task.FromResult<ICollection<MultiTenantTestEntity>>(_entities.Where(filtered.Compile()).ToList());
    }

    public override Task<ICollection<MultiTenantTestEntity>> FindAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(expression, _tenantIdAccessor.GetTenantId());
        return Task.FromResult<ICollection<MultiTenantTestEntity>>(_entities.Where(filtered.Compile()).ToList());
    }

    public override Task<MultiTenantTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
    {
        var entity = _entities.FirstOrDefault();
        var currentTenantId = _tenantIdAccessor.GetTenantId();
        if (entity != null && MultiTenantHelper.IsMultiTenant<MultiTenantTestEntity>()
            && !string.IsNullOrEmpty(currentTenantId)
            && ((IMultiTenant)entity).TenantId != currentTenantId)
        {
            return Task.FromResult<MultiTenantTestEntity>(default!);
        }
        return Task.FromResult(entity!);
    }

    public override Task<long> GetCountAsync(ISpecification<MultiTenantTestEntity> selectSpec, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(selectSpec.Predicate, _tenantIdAccessor.GetTenantId());
        return Task.FromResult((long)_entities.Count(filtered.Compile()));
    }

    public override Task<long> GetCountAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(expression, _tenantIdAccessor.GetTenantId());
        return Task.FromResult((long)_entities.Count(filtered.Compile()));
    }

    public override Task<MultiTenantTestEntity> FindSingleOrDefaultAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(expression, _tenantIdAccessor.GetTenantId());
        return Task.FromResult(_entities.SingleOrDefault(filtered.Compile())!);
    }

    public override Task<MultiTenantTestEntity> FindSingleOrDefaultAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(specification.Predicate, _tenantIdAccessor.GetTenantId());
        return Task.FromResult(_entities.SingleOrDefault(filtered.Compile())!);
    }

    public override Task<bool> AnyAsync(Expression<Func<MultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(expression, _tenantIdAccessor.GetTenantId());
        return Task.FromResult(_entities.Any(filtered.Compile()));
    }

    public override Task<bool> AnyAsync(ISpecification<MultiTenantTestEntity> specification, CancellationToken token = default)
    {
        var filtered = MultiTenantHelper.CombineWithTenantFilter(specification.Predicate, _tenantIdAccessor.GetTenantId());
        return Task.FromResult(_entities.Any(filtered.Compile()));
    }
}

// ============================================================================
// Test repository implementations for NonMultiTenantTestEntity (SQL-based)
// ============================================================================

public class TestNonMultiTenantSqlRepository : SqlRepositoryBase<NonMultiTenantTestEntity>
{
    private readonly List<NonMultiTenantTestEntity> _entities = new();

    public TestNonMultiTenantSqlRepository(
        IDataStoreFactory dataStoreFactory,
        ILoggerFactory loggerFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        ITenantIdAccessor tenantIdAccessor)
        : base(dataStoreFactory, loggerFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
    {
    }

    public override Task AddAsync(NonMultiTenantTestEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<NonMultiTenantTestEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonMultiTenantTestEntity entity, CancellationToken token = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(NonMultiTenantTestEntity entity, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
    {
        var matches = _entities.Where(expression.Compile()).ToList();
        foreach (var e in matches) _entities.Remove(e);
        return Task.FromResult(matches.Count);
    }

    public override Task<int> DeleteManyAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => DeleteManyAsync(specification.Predicate, token);

    public override Task<int> DeleteManyAsync(ISpecification<NonMultiTenantTestEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task UpdateAsync(NonMultiTenantTestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    public override Task<ICollection<NonMultiTenantTestEntity>> FindAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<NonMultiTenantTestEntity>>(_entities.Where(specification.Predicate.Compile()).ToList());

    public override Task<ICollection<NonMultiTenantTestEntity>> FindAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<NonMultiTenantTestEntity>>(_entities.Where(expression.Compile()).ToList());

    public override Task<NonMultiTenantTestEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<NonMultiTenantTestEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(selectSpec.Predicate.Compile()));

    public override Task<long> GetCountAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(expression.Compile()));

    public override Task<NonMultiTenantTestEntity> FindSingleOrDefaultAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(expression.Compile())!);

    public override Task<NonMultiTenantTestEntity> FindSingleOrDefaultAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(specification.Predicate.Compile())!);

    public override Task<bool> AnyAsync(Expression<Func<NonMultiTenantTestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.Any(expression.Compile()));

    public override Task<bool> AnyAsync(ISpecification<NonMultiTenantTestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.Any(specification.Predicate.Compile()));
}
