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
/// AC-8 regression: EFCoreAggregateRepository must pass its DataStoreName to the event tracker
/// at every call site, not the null/default sentinel.
/// </summary>
public class EFCoreAggregateRepositoryDataStoreNameTests : IDisposable
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly TestDbContext _dbContext;
    private const string DataStoreName = "B";

    public EFCoreAggregateRepositoryDataStoreNameTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _mockTenantIdAccessor = new Mock<ITenantIdAccessor>();

        _mockDefaultDataStoreOptions
            .Setup(x => x.Value)
            .Returns(new DefaultDataStoreOptions { DefaultDataStoreName = DataStoreName });
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TestDbContext(options);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDbContext>(DataStoreName))
            .Returns(_dbContext);
    }

    public void Dispose() => _dbContext?.Dispose();

    private EFCoreAggregateRepository<TestOrderAggregate, Guid> CreateRepository()
    {
        return new EFCoreAggregateRepository<TestOrderAggregate, Guid>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _mockTenantIdAccessor.Object);
    }

    // ── AC-8: AddAsync passes DataStoreName ──────────────────────────────────

    [Fact]
    public async Task AddAsync_PassesDataStoreNameToTracker()
    {
        var repository = CreateRepository();
        var aggregate = new TestOrderAggregate(Guid.NewGuid(), "Alice");

        await repository.AddAsync(aggregate);

        _mockEventTracker.Verify(
            x => x.AddEntity(aggregate, DataStoreName),
            Times.Once,
            "AddAsync must call AddEntity(entity, DataStoreName), not the null-datastore overload");
    }

    [Fact]
    public async Task AddAsync_DoesNotCallSingleArgOverload()
    {
        var repository = CreateRepository();
        var aggregate = new TestOrderAggregate(Guid.NewGuid(), "Bob");

        await repository.AddAsync(aggregate);

        _mockEventTracker.Verify(
            x => x.AddEntity(aggregate),
            Times.Never,
            "AddAsync must not fall back to the null-datastore single-arg overload");
    }

    // ── AC-8: UpdateAsync passes DataStoreName ───────────────────────────────

    [Fact]
    public async Task UpdateAsync_PassesDataStoreNameToTracker()
    {
        var repository = CreateRepository();
        var aggregate = new TestOrderAggregate(Guid.NewGuid(), "Carol");
        await repository.AddAsync(aggregate);

        _mockEventTracker.Invocations.Clear();

        await repository.UpdateAsync(aggregate);

        _mockEventTracker.Verify(
            x => x.AddEntity(aggregate, DataStoreName),
            Times.Once,
            "UpdateAsync must call AddEntity(entity, DataStoreName)");
    }

    // ── AC-8: DeleteAsync passes DataStoreName ───────────────────────────────

    [Fact]
    public async Task DeleteAsync_PassesDataStoreNameToTracker()
    {
        var repository = CreateRepository();
        var aggregate = new TestOrderAggregate(Guid.NewGuid(), "Dana");
        _dbContext.Set<TestOrderAggregate>().Add(aggregate);
        await _dbContext.SaveChangesAsync();

        _mockEventTracker.Invocations.Clear();

        await repository.DeleteAsync(aggregate);

        _mockEventTracker.Verify(
            x => x.AddEntity(aggregate, DataStoreName),
            Times.Once,
            "DeleteAsync must call AddEntity(entity, DataStoreName)");
    }

    // ── AC-8: IAggregateRepository explicit interface AddAsync ───────────────

    [Fact]
    public async Task IAggregateRepository_AddAsync_PassesDataStoreNameToTracker()
    {
        IAggregateRepository<TestOrderAggregate, Guid> repository = CreateRepository();
        var aggregate = new TestOrderAggregate(Guid.NewGuid(), "Erin");

        await repository.AddAsync(aggregate);

        _mockEventTracker.Verify(
            x => x.AddEntity(aggregate, DataStoreName),
            Times.Once,
            "IAggregateRepository.AddAsync (explicit interface) must pass DataStoreName to tracker");
    }
}
