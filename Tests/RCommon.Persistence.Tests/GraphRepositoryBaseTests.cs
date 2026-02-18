using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Collections;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using System.Data.Common;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class GraphRepositoryBaseTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public GraphRepositoryBaseTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _defaultOptions = new DefaultDataStoreOptions();

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullDataStoreFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestGraphRepository(
            null!,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("dataStoreFactory");
    }

    [Fact]
    public void Constructor_WithNullEventTracker_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestGraphRepository(
            _mockDataStoreFactory.Object,
            null!,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventTracker");
    }

    [Fact]
    public void Constructor_WithDefaultDataStoreName_SetsDataStoreName()
    {
        // Arrange
        var expectedName = "GraphTestDataStore";
        _defaultOptions.DefaultDataStoreName = expectedName;

        // Act
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void Tracking_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        repository.Tracking = true;

        // Assert
        repository.Tracking.Should().BeTrue();
    }

    [Fact]
    public void Tracking_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.Tracking.Should().BeFalse();
    }

    [Fact]
    public void Repository_InheritsFromLinqRepositoryBase()
    {
        // Arrange & Act
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.Should().BeAssignableTo<LinqRepositoryBase<TestGraphEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIGraphRepository()
    {
        // Arrange & Act
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.Should().BeAssignableTo<IGraphRepository<TestGraphEntity>>();
    }

    [Fact]
    public void EventTracker_ReturnsInjectedEventTracker()
    {
        // Arrange
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var eventTracker = repository.EventTracker;

        // Assert
        eventTracker.Should().Be(_mockEventTracker.Object);
    }

    [Fact]
    public void DataStoreName_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        var expectedName = "CustomGraphDataStore";

        // Act
        repository.DataStoreName = expectedName;

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void Logger_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        var mockLogger = new Mock<ILogger>();

        // Act
        repository.Logger = mockLogger.Object;

        // Assert
        repository.Logger.Should().Be(mockLogger.Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Tracking_CanBeSetToEitherValue(bool trackingValue)
    {
        // Arrange
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        repository.Tracking = trackingValue;

        // Assert
        repository.Tracking.Should().Be(trackingValue);
    }

    [Fact]
    public void Dispose_CanBeCalledSafely()
    {
        // Arrange
        var repository = new TestGraphRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var action = () => repository.Dispose();

        // Assert
        action.Should().NotThrow();
    }
}

// Test entity for graph repository tests
public class TestGraphEntity : BusinessEntity<Guid>
{
    public string? Name { get; set; }
    public List<TestGraphEntity>? Children { get; set; }

    public TestGraphEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public TestGraphEntity(Guid id) : base(id)
    {
    }
}

// Concrete test implementation of GraphRepositoryBase
public class TestGraphRepository : GraphRepositoryBase<TestGraphEntity>
{
    private readonly List<TestGraphEntity> _entities = new();
    private bool _tracking = false;

    public TestGraphRepository(
        IDataStoreFactory dataStoreFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
        : base(dataStoreFactory, eventTracker, defaultDataStoreOptions)
    {
    }

    protected override IQueryable<TestGraphEntity> RepositoryQuery => _entities.AsQueryable();

    public override bool Tracking
    {
        get => _tracking;
        set => _tracking = value;
    }

    public override Task AddAsync(TestGraphEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<TestGraphEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(TestGraphEntity entity, CancellationToken token = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(TestGraphEntity entity, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(Expression<Func<TestGraphEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(0);

    public override Task<int> DeleteManyAsync(Expression<Func<TestGraphEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task<int> DeleteManyAsync(ISpecification<TestGraphEntity> specification, CancellationToken token = default)
        => Task.FromResult(0);

    public override Task<int> DeleteManyAsync(ISpecification<TestGraphEntity> specification, bool isSoftDelete, CancellationToken token = default)
        => throw new NotImplementedException();

    public override Task UpdateAsync(TestGraphEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    public override Task<ICollection<TestGraphEntity>> FindAsync(ISpecification<TestGraphEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<TestGraphEntity>>(_entities.Where(specification.Predicate.Compile()).ToList());

    public override Task<ICollection<TestGraphEntity>> FindAsync(Expression<Func<TestGraphEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<TestGraphEntity>>(_entities.Where(expression.Compile()).ToList());

    public override Task<TestGraphEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<TestGraphEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count);

    public override Task<long> GetCountAsync(Expression<Func<TestGraphEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(expression.Compile()));

    public override Task<TestGraphEntity> FindSingleOrDefaultAsync(Expression<Func<TestGraphEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(expression.Compile())!);

    public override Task<TestGraphEntity> FindSingleOrDefaultAsync(ISpecification<TestGraphEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(specification.Predicate.Compile())!);

    public override Task<bool> AnyAsync(Expression<Func<TestGraphEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.Any(expression.Compile()));

    public override Task<bool> AnyAsync(ISpecification<TestGraphEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.Any(specification.Predicate.Compile()));

    public override IQueryable<TestGraphEntity> FindQuery(ISpecification<TestGraphEntity> specification)
        => _entities.AsQueryable().Where(specification.Predicate);

    public override IQueryable<TestGraphEntity> FindQuery(Expression<Func<TestGraphEntity, bool>> expression)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<TestGraphEntity> FindQuery(Expression<Func<TestGraphEntity, bool>> expression,
        Expression<Func<TestGraphEntity, object>> orderByExpression, bool orderByAscending)
        => _entities.AsQueryable().Where(expression);

    public override Task<IPaginatedList<TestGraphEntity>> FindAsync(Expression<Func<TestGraphEntity, bool>> expression,
        Expression<Func<TestGraphEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1,
        int pageSize = 0, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<TestGraphEntity>>(null!);

    public override Task<IPaginatedList<TestGraphEntity>> FindAsync(IPagedSpecification<TestGraphEntity> specification, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<TestGraphEntity>>(null!);

    public override IQueryable<TestGraphEntity> FindQuery(Expression<Func<TestGraphEntity, bool>> expression,
        Expression<Func<TestGraphEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<TestGraphEntity> FindQuery(IPagedSpecification<TestGraphEntity> specification)
        => _entities.AsQueryable();

    public override IEagerLoadableQueryable<TestGraphEntity> Include(Expression<Func<TestGraphEntity, object>> path)
        => null!;

    public override IEagerLoadableQueryable<TestGraphEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        => null!;
}
