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

public class LinqRepositoryBaseTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly DefaultDataStoreOptions _defaultOptions;

    public LinqRepositoryBaseTests()
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
        var repository = new TestLinqRepository(
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
        var action = () => new TestLinqRepository(
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
        var action = () => new TestLinqRepository(
            _mockDataStoreFactory.Object,
            null!,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventTracker");
    }

    [Fact]
    public void Constructor_WithNullDefaultDataStoreOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("defaultDataStoreOptions");
    }

    [Fact]
    public void Constructor_WithDefaultDataStoreName_SetsDataStoreName()
    {
        // Arrange
        var expectedName = "TestDataStore";
        _defaultOptions.DefaultDataStoreName = expectedName;

        // Act
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void DataStoreName_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        var expectedName = "CustomDataStore";

        // Act
        repository.DataStoreName = expectedName;

        // Assert
        repository.DataStoreName.Should().Be(expectedName);
    }

    [Fact]
    public void EventTracker_ReturnsInjectedEventTracker()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var eventTracker = repository.EventTracker;

        // Assert
        eventTracker.Should().Be(_mockEventTracker.Object);
    }

    [Fact]
    public void Logger_CanBeSetAndGet()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        var mockLogger = new Mock<ILogger>();

        // Act
        repository.Logger = mockLogger.Object;

        // Assert
        repository.Logger.Should().Be(mockLogger.Object);
    }

    [Fact]
    public void GetEnumerator_ReturnsRepositoryQueryEnumerator()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var enumerator = repository.GetEnumerator();

        // Assert
        enumerator.Should().NotBeNull();
    }

    [Fact]
    public void Expression_ReturnsRepositoryQueryExpression()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var expression = repository.Expression;

        // Assert
        expression.Should().NotBeNull();
    }

    [Fact]
    public void ElementType_ReturnsEntityType()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var elementType = repository.ElementType;

        // Assert
        elementType.Should().Be(typeof(TestEntity));
    }

    [Fact]
    public void Provider_ReturnsRepositoryQueryProvider()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var provider = repository.Provider;

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Query_WithSpecification_ReturnsFilteredResults()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        mockSpecification.Setup(x => x.Predicate).Returns(e => e.Id == Guid.Empty);

        // Act
        var result = repository.Query(mockSpecification.Object);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Repository_ImplementsILinqRepository()
    {
        // Arrange & Act
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.Should().BeAssignableTo<ILinqRepository<TestEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIEnumerable()
    {
        // Arrange & Act
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.Should().BeAssignableTo<IEnumerable<TestEntity>>();
    }

    [Fact]
    public void Repository_ImplementsIQueryable()
    {
        // Arrange & Act
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Assert
        repository.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public void Dispose_CanBeCalledSafely()
    {
        // Arrange
        var repository = new TestLinqRepository(
            _mockDataStoreFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object);

        // Act
        var action = () => repository.Dispose();

        // Assert
        action.Should().NotThrow();
    }
}

// Test entity for repository tests
public class TestEntity : BusinessEntity<Guid>
{
    public string? Name { get; set; }

    public TestEntity() : base()
    {
        Id = Guid.NewGuid();
    }

    public TestEntity(Guid id) : base(id)
    {
    }
}

// Concrete test implementation of LinqRepositoryBase
public class TestLinqRepository : LinqRepositoryBase<TestEntity>
{
    private readonly List<TestEntity> _entities = new();

    public TestLinqRepository(
        IDataStoreFactory dataStoreFactory,
        IEntityEventTracker eventTracker,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
        : base(dataStoreFactory, eventTracker, defaultDataStoreOptions)
    {
    }

    protected override IQueryable<TestEntity> RepositoryQuery => _entities.AsQueryable();

    public override Task AddAsync(TestEntity entity, CancellationToken token = default)
    {
        _entities.Add(entity);
        return Task.CompletedTask;
    }

    public override Task AddRangeAsync(IEnumerable<TestEntity> entities, CancellationToken token = default)
    {
        _entities.AddRange(entities);
        return Task.CompletedTask;
    }

    public override Task DeleteAsync(TestEntity entity, CancellationToken token = default)
    {
        _entities.Remove(entity);
        return Task.CompletedTask;
    }

    public override Task<int> DeleteManyAsync(Expression<Func<TestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(0);

    public override Task<int> DeleteManyAsync(ISpecification<TestEntity> specification, CancellationToken token = default)
        => Task.FromResult(0);

    public override Task UpdateAsync(TestEntity entity, CancellationToken token = default)
        => Task.CompletedTask;

    public override Task<ICollection<TestEntity>> FindAsync(ISpecification<TestEntity> specification, CancellationToken token = default)
        => Task.FromResult<ICollection<TestEntity>>(_entities.Where(specification.Predicate.Compile()).ToList());

    public override Task<ICollection<TestEntity>> FindAsync(Expression<Func<TestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult<ICollection<TestEntity>>(_entities.Where(expression.Compile()).ToList());

    public override Task<TestEntity> FindAsync(object primaryKey, CancellationToken token = default)
        => Task.FromResult(_entities.FirstOrDefault()!);

    public override Task<long> GetCountAsync(ISpecification<TestEntity> selectSpec, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count);

    public override Task<long> GetCountAsync(Expression<Func<TestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult((long)_entities.Count(expression.Compile()));

    public override Task<TestEntity> FindSingleOrDefaultAsync(Expression<Func<TestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(expression.Compile())!);

    public override Task<TestEntity> FindSingleOrDefaultAsync(ISpecification<TestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.SingleOrDefault(specification.Predicate.Compile())!);

    public override Task<bool> AnyAsync(Expression<Func<TestEntity, bool>> expression, CancellationToken token = default)
        => Task.FromResult(_entities.Any(expression.Compile()));

    public override Task<bool> AnyAsync(ISpecification<TestEntity> specification, CancellationToken token = default)
        => Task.FromResult(_entities.Any(specification.Predicate.Compile()));

    public override IQueryable<TestEntity> FindQuery(ISpecification<TestEntity> specification)
        => _entities.AsQueryable().Where(specification.Predicate);

    public override IQueryable<TestEntity> FindQuery(Expression<Func<TestEntity, bool>> expression)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<TestEntity> FindQuery(Expression<Func<TestEntity, bool>> expression,
        Expression<Func<TestEntity, object>> orderByExpression, bool orderByAscending)
        => _entities.AsQueryable().Where(expression);

    public override Task<IPaginatedList<TestEntity>> FindAsync(Expression<Func<TestEntity, bool>> expression,
        Expression<Func<TestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1,
        int pageSize = 0, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<TestEntity>>(null!);

    public override Task<IPaginatedList<TestEntity>> FindAsync(IPagedSpecification<TestEntity> specification, CancellationToken token = default)
        => Task.FromResult<IPaginatedList<TestEntity>>(null!);

    public override IQueryable<TestEntity> FindQuery(Expression<Func<TestEntity, bool>> expression,
        Expression<Func<TestEntity, object>> orderByExpression, bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        => _entities.AsQueryable().Where(expression);

    public override IQueryable<TestEntity> FindQuery(IPagedSpecification<TestEntity> specification)
        => _entities.AsQueryable();

    public override IEagerLoadableQueryable<TestEntity> Include(Expression<Func<TestEntity, object>> path)
        => null!;

    public override IEagerLoadableQueryable<TestEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        => null!;
}
