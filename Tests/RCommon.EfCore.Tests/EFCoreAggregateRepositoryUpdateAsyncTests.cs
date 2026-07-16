using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Crud;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.EfCore.Tests;

/// <summary>
/// Regression tests for the UpdateAsync new-child misclassification bug and the change-tracking
/// fix that replaces it. See docs/superpowers/specs/2026-03-17-aggregate-repository-design.md,
/// addendum "UpdateAsync Change-Tracking Fix" for the full design rationale.
/// </summary>
public class EFCoreAggregateRepositoryUpdateAsyncTests : IDisposable
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly DefaultDataStoreOptions _defaultOptions;
    private readonly TestDbContext _dbContext;
    private readonly string _dataStoreName = "TestDataStore";

    public EFCoreAggregateRepositoryUpdateAsyncTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();
        _defaultOptions = new DefaultDataStoreOptions { DefaultDataStoreName = _dataStoreName };

        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(_defaultOptions);
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

    private IAggregateRepository<TestOrderAggregate, Guid> CreateRepository()
    {
        return new EFCoreAggregateRepository<TestOrderAggregate, Guid>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }

    [Fact]
    public async Task UpdateAsync_AddingNewLineItemToExistingOrder_PersistsWithoutThrowing()
    {
        // Arrange -- create and persist an order with no line items, in the same repository
        // (and therefore same DbContext) instance/scope that will later call UpdateAsync.
        var repository = CreateRepository();
        var order = new TestOrderAggregate(Guid.NewGuid(), "Alice");
        await repository.AddAsync(order);

        // Simulate RCommon's own recommended client-generated (sequential GUID) key strategy:
        // the new child already has a non-default Id before it is ever saved.
        var newLineItem = new TestOrderLineItem(Guid.NewGuid(), "Widget");
        order.AddLineItem(newLineItem);

        // Act
        Func<Task> act = async () => await repository.UpdateAsync(order);

        // Assert -- previously threw DbUpdateConcurrencyException because EF's default Update()
        // graph walk marked the new, non-default-keyed child as Modified instead of Added.
        await act.Should().NotThrowAsync();

        var persisted = await _dbContext.Set<TestOrderLineItem>()
            .FirstOrDefaultAsync(li => li.Id == newLineItem.Id);
        persisted.Should().NotBeNull();
        persisted!.ProductName.Should().Be("Widget");
    }

    [Fact]
    public async Task UpdateAsync_MutatingExistingLineItemLoadedViaInclude_PersistsPropertyChange()
    {
        // Arrange -- persist an order with one line item, then load it back (with Include) in
        // the same DbContext scope so the existing child is tracked before mutation. This locks
        // in that the UpdateAsync fix does not regress updates to already-tracked existing
        // children -- only genuinely untracked nodes are affected by the fix.
        var repository = CreateRepository();
        var order = new TestOrderAggregate(Guid.NewGuid(), "Bob");
        var lineItem = new TestOrderLineItem(Guid.NewGuid(), "Original Name");
        order.AddLineItem(lineItem);
        await repository.AddAsync(order);

        var loaded = await repository
            .Include(o => o.LineItems)
            .GetByIdAsync(order.Id);
        loaded.Should().NotBeNull();
        var trackedLineItem = loaded!.LineItems.Single();
        trackedLineItem.ProductName = "Updated Name";

        // Act
        await repository.UpdateAsync(loaded);

        // Assert
        var persisted = await _dbContext.Set<TestOrderLineItem>()
            .FirstOrDefaultAsync(li => li.Id == lineItem.Id);
        persisted.Should().NotBeNull();
        persisted!.ProductName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateAsync_AddingNewLineItemAndMutatingExistingLineItem_PersistsBoth()
    {
        // Arrange -- combines both supported cases in one call: a new child alongside a mutated
        // existing (Include-loaded) child.
        var repository = CreateRepository();
        var order = new TestOrderAggregate(Guid.NewGuid(), "Dana");
        var existingItem = new TestOrderLineItem(Guid.NewGuid(), "Existing");
        order.AddLineItem(existingItem);
        await repository.AddAsync(order);

        var loaded = await repository.Include(o => o.LineItems).GetByIdAsync(order.Id);
        loaded.Should().NotBeNull();
        loaded!.LineItems.Single().ProductName = "Existing Updated";
        var newItem = new TestOrderLineItem(Guid.NewGuid(), "Brand New");
        loaded.AddLineItem(newItem);

        // Act
        await repository.UpdateAsync(loaded);

        // Assert
        var persistedExisting = await _dbContext.Set<TestOrderLineItem>()
            .FirstOrDefaultAsync(li => li.Id == existingItem.Id);
        var persistedNew = await _dbContext.Set<TestOrderLineItem>()
            .FirstOrDefaultAsync(li => li.Id == newItem.Id);

        persistedExisting.Should().NotBeNull();
        persistedExisting!.ProductName.Should().Be("Existing Updated");
        persistedNew.Should().NotBeNull();
        persistedNew!.ProductName.Should().Be("Brand New");
    }

    [Fact]
    public async Task UpdateAsync_FullyDisconnectedGraphWithExistingChild_ThrowsLoudly()
    {
        // Documents the one boundary the fix does not cover: an aggregate whose existing child was
        // never tracked in *this* DbContext instance (e.g. deserialized from a message, or loaded in
        // a separate request/process) looks, to this method, identical to a genuinely new child --
        // both are Detached. This asserts the documented failure mode (a loud, distinct exception),
        // not silent data loss -- the supported answer for this scenario remains persisting the new
        // child through its own repository plus IEntityEventTracker.AddEntity, as documented.
        var repository = CreateRepository();
        var order = new TestOrderAggregate(Guid.NewGuid(), "Erin");
        var existingItem = new TestOrderLineItem(Guid.NewGuid(), "Pre-existing");
        order.AddLineItem(existingItem);
        await repository.AddAsync(order);

        // Clear the change tracker so the objects constructed below are genuinely never-tracked by
        // this DbContext, even though rows with the same keys already exist in the store -- this
        // simulates a fully disconnected graph (e.g. deserialized from a message, or loaded in a
        // separate request/process) without needing a second DbContext instance.
        _dbContext.ChangeTracker.Clear();

        var disconnectedOrder = new TestOrderAggregate(order.Id, "Erin");
        disconnectedOrder.AddLineItem(new TestOrderLineItem(existingItem.Id, "Pre-existing"));

        Func<Task> act = async () => await repository.UpdateAsync(disconnectedOrder);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Include_WithCollectionNavigation_PopulatesLineItems()
    {
        // Arrange
        var repository = CreateRepository();
        var order = new TestOrderAggregate(Guid.NewGuid(), "Carol");
        order.AddLineItem(new TestOrderLineItem(Guid.NewGuid(), "Gadget"));
        order.AddLineItem(new TestOrderLineItem(Guid.NewGuid(), "Gizmo"));
        await repository.AddAsync(order);

        // Clear the change tracker so the "without Include" query below is a genuinely fresh load,
        // not auto-populated via EF's identity-map fixup from the AddAsync call above in the same
        // DbContext instance.
        _dbContext.ChangeTracker.Clear();

        // Act -- GetByIdAsync alone does not eager-load by default (documented, expected);
        // Include must be chained explicitly.
        var withoutInclude = await CreateRepository().GetByIdAsync(order.Id);
        _dbContext.ChangeTracker.Clear();
        var withInclude = await repository.Include(o => o.LineItems).GetByIdAsync(order.Id);

        // Assert
        withoutInclude.Should().NotBeNull();
        withoutInclude!.LineItems.Should().BeEmpty();

        withInclude.Should().NotBeNull();
        withInclude!.LineItems.Should().HaveCount(2);
    }
}
