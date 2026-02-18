using FluentAssertions;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon;
using RCommon.Collections;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Security.Claims;
using RCommon.Persistence.Linq2Db;
using RCommon.Persistence.Linq2Db.Crud;
using System.Linq.Expressions;
using Xunit;

namespace RCommon.Linq2Db.Tests;

public class Linq2DbRepositoryTests
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly IOptions<DefaultDataStoreOptions> _defaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;

    public Linq2DbRepositoryTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        _defaultDataStoreOptions = Options.Create(new DefaultDataStoreOptions
        {
            DefaultDataStoreName = "TestStore"
        });

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);
    }

    private DataOptions CreateSQLiteOptions()
    {
        return new DataOptions()
            .UseSQLite("Data Source=:memory:");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullDataStoreFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new Linq2DbRepository<TestEntity>(
            null!,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dataStoreFactory");
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            null!,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullEventTracker_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            null!,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventTracker");
    }

    [Fact]
    public void Constructor_WithNullDefaultDataStoreOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            null!,
            _mockTenantIdAccessor.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("defaultDataStoreOptions");
    }

    [Fact]
    public void Linq2DbRepository_ImplementsILinqRepository()
    {
        // Arrange & Act
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        repository.Should().BeAssignableTo<ILinqRepository<TestEntity>>();
    }

    [Fact]
    public void Linq2DbRepository_ImplementsIReadOnlyRepository()
    {
        // Arrange & Act
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        repository.Should().BeAssignableTo<IReadOnlyRepository<TestEntity>>();
    }

    [Fact]
    public void Linq2DbRepository_ImplementsIWriteOnlyRepository()
    {
        // Arrange & Act
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        repository.Should().BeAssignableTo<IWriteOnlyRepository<TestEntity>>();
    }

    #endregion

    #region DataStoreName Tests

    [Fact]
    public void DataStoreName_WithDefaultOptions_ReturnsDefaultStoreName()
    {
        // Arrange
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var dataStoreName = repository.DataStoreName;

        // Assert
        dataStoreName.Should().Be("TestStore");
    }

    [Fact]
    public void DataStoreName_CanBeSet()
    {
        // Arrange
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        repository.DataStoreName = "CustomStore";

        // Assert
        repository.DataStoreName.Should().Be("CustomStore");
    }

    #endregion

    #region EventTracker Tests

    [Fact]
    public void EventTracker_ReturnsInjectedEventTracker()
    {
        // Arrange
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var eventTracker = repository.EventTracker;

        // Assert
        eventTracker.Should().BeSameAs(_mockEventTracker.Object);
    }

    #endregion

    #region FindAsync (primaryKey) Tests

    [Fact]
    public async Task FindAsync_ByPrimaryKey_ThrowsNotImplementedException()
    {
        // Arrange
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var act = async () => await repository.FindAsync(1);

        // Assert
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    #endregion

    #region AddRangeAsync Tests

    [Fact]
    public async Task AddRangeAsync_WithNullEntities_ThrowsArgumentNullException()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var act = async () => await repository.AddRangeAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entities");
    }

    #endregion

    #region Include Tests

    [Fact]
    public void Include_ReturnsEagerLoadableQueryable()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var result = repository.Include(x => x.Name);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEagerLoadableQueryable<TestEntity>>();
    }

    #endregion

    #region Logger Tests

    [Fact]
    public void Logger_IsCreatedFromLoggerFactory()
    {
        // Arrange & Act
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Assert
        _mockLoggerFactory.Verify(x => x.CreateLogger(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Logger_CanBeAccessed()
    {
        // Arrange
        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var logger = repository.Logger;

        // Assert
        logger.Should().NotBeNull();
    }

    #endregion

    #region FindQuery Tests

    [Fact]
    public void FindQuery_WithExpression_ReturnsQueryable()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var result = repository.FindQuery(x => x.Id == 1);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public void FindQuery_WithSpecification_ReturnsQueryable()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        mockSpecification.Setup(x => x.Predicate).Returns(x => x.Id == 1);

        // Act
        var result = repository.FindQuery(mockSpecification.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public void FindQuery_WithExpressionAndOrderBy_ReturnsQueryable()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var result = repository.FindQuery(
            x => x.Id > 0,
            x => x.Name,
            orderByAscending: true);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public void FindQuery_WithPaging_ReturnsQueryable()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var result = repository.FindQuery(
            x => x.Id > 0,
            x => x.Name,
            orderByAscending: true,
            pageNumber: 1,
            pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public void FindQuery_WithPagedSpecification_ReturnsQueryable()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        var mockSpecification = new Mock<IPagedSpecification<TestEntity>>();
        mockSpecification.Setup(x => x.Predicate).Returns(x => x.Id > 0);
        mockSpecification.Setup(x => x.OrderByExpression).Returns(x => x.Name);
        mockSpecification.Setup(x => x.OrderByAscending).Returns(true);
        mockSpecification.Setup(x => x.PageNumber).Returns(1);
        mockSpecification.Setup(x => x.PageSize).Returns(10);

        // Act
        var result = repository.FindQuery(mockSpecification.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    [Fact]
    public void FindQuery_WithDescendingOrder_ReturnsQueryable()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var result = repository.FindQuery(
            x => x.Id > 0,
            x => x.Name,
            orderByAscending: false);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IQueryable<TestEntity>>();
    }

    #endregion

    #region IQueryable Implementation Tests

    [Fact]
    public void Expression_ReturnsQueryableExpression()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var expression = repository.Expression;

        // Assert
        expression.Should().NotBeNull();
    }

    [Fact]
    public void ElementType_ReturnsEntityType()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var elementType = repository.ElementType;

        // Assert
        elementType.Should().Be(typeof(TestEntity));
    }

    [Fact]
    public void Provider_ReturnsQueryProvider()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var provider = repository.Provider;

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void GetEnumerator_ReturnsEnumerator()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        // Act
        var enumerator = repository.GetEnumerator();

        // Assert
        enumerator.Should().NotBeNull();
    }

    #endregion

    #region Query Tests

    [Fact]
    public void Query_WithSpecification_ReturnsFilteredResults()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var testConnection = new TestDataConnection(dataOptions);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDataConnection>(It.IsAny<string>()))
            .Returns(testConnection);

        var repository = new Linq2DbRepository<TestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _defaultDataStoreOptions,
            _mockTenantIdAccessor.Object);

        var mockSpecification = new Mock<ISpecification<TestEntity>>();
        mockSpecification.Setup(x => x.Predicate).Returns(x => x.Id > 0);

        // Act
        var result = repository.Query(mockSpecification.Object);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEnumerable<TestEntity>>();
    }

    #endregion

    #region Helper Classes

    [Table("TestEntities")]
    public class TestEntity : BusinessEntity<int>
    {
        [Column("Id"), PrimaryKey]
        public override int Id { get; protected set; }

        [Column("Name")]
        public string Name { get; set; } = string.Empty;

        [Column("Description")]
        public string? Description { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }
    }

    public class TestDataConnection : RCommonDataConnection
    {
        public TestDataConnection(DataOptions dataOptions) : base(dataOptions)
        {
        }

        public ITable<TestEntity> TestEntities => this.GetTable<TestEntity>();
    }

    #endregion
}
