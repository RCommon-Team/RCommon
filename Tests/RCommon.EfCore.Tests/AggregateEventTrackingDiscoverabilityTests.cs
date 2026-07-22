using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Crud;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.EfCore.Tests;

/// <summary>
/// Locks in the event-tracking discoverability contract from item 13 of the consumer feedback
/// review (docs/superpowers/specs/2026-03-17-aggregate-repository-design.md addendum, Design
/// Decision 3): persisting a child directly through its own repository -- the documented pattern
/// for the disconnected-graph boundary in EFCoreAggregateRepositoryUpdateAsyncTests -- never
/// touches the parent aggregate's event tracking. A consumer using that pattern must explicitly
/// call IEntityEventTracker.AddEntity(aggregateRoot) themselves for the aggregate's domain events
/// to be dispatched; this is not a bug, but it is easy to miss, hence the explicit test.
/// </summary>
public class AggregateEventTrackingDiscoverabilityTests : IDisposable
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly TestDbContext _dbContext;
    private readonly string _dataStoreName = "TestDataStore";

    public AggregateEventTrackingDiscoverabilityTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        var defaultOptions = new DefaultDataStoreOptions { DefaultDataStoreName = _dataStoreName };

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(defaultOptions);
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TestDbContext(options);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDbContext>(_dataStoreName))
            .Returns(_dbContext);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    [Fact]
    public async Task AddAsync_OnChildRepository_NeverRegistersParentAggregateForEventTracking()
    {
        // Arrange -- an order already exists; a new line item is persisted through its own
        // repository directly (the documented workaround for the disconnected-graph boundary),
        // not through the parent order's UpdateAsync.
        var order = new TestOrderAggregate(Guid.NewGuid(), "Alice");
        var lineItemRepository = new EFCoreRepository<TestOrderLineItem>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
        var lineItem = new TestOrderLineItem(Guid.NewGuid(), "Widget") { OrderId = order.Id };

        // Act
        await lineItemRepository.AddAsync(lineItem);

        // Assert -- the child itself is registered for event tracking with its datastore name
        // (AC-8: repository now passes DataStoreName to the tracker)...
        _mockEventTracker.Verify(t => t.AddEntity(lineItem, _dataStoreName), Times.Once);

        // ...but the parent aggregate is never touched -- if it raised a domain event before this
        // call (e.g. order.AddLineItem(lineItem) internally calling AddDomainEvent), that event
        // will never be dispatched unless the caller explicitly calls
        // IEntityEventTracker.AddEntity(order) themselves, per the documented contract.
        _mockEventTracker.Verify(t => t.AddEntity(order, It.IsAny<string?>()), Times.Never);
        _mockEventTracker.Verify(t => t.AddEntity(It.Is<IBusinessEntity>(e => e == (IBusinessEntity)order), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public void ManuallyCallingAddEntity_RegistersTheAggregateForEventTracking()
    {
        // Documents the supported fix: the caller closes the gap above with one explicit call.
        var order = new TestOrderAggregate(Guid.NewGuid(), "Bob");

        _mockEventTracker.Object.AddEntity(order);

        _mockEventTracker.Verify(t => t.AddEntity(order), Times.Once);
    }
}
