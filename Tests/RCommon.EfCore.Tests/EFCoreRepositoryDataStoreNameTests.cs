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
/// AC-8 regression: repositories must pass their DataStoreName to the event tracker,
/// not the null/default sentinel, so that multi-datastore outbox delivery routes events
/// to the correct store.
/// </summary>
public class EFCoreRepositoryDataStoreNameTests : IDisposable
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly Mock<ITenantIdAccessor> _mockTenantIdAccessor;
    private readonly TestDbContext _dbContext;
    private const string DataStoreName = "B";

    public EFCoreRepositoryDataStoreNameTests()
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

    private EFCoreRepository<TestEntity> CreateRepository()
    {
        return new EFCoreRepository<TestEntity>(
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
        var entity = new TestEntity { Name = "AC-8 Test" };

        await repository.AddAsync(entity);

        _mockEventTracker.Verify(
            x => x.AddEntity(entity, DataStoreName),
            Times.Once,
            "AddAsync must call AddEntity(entity, DataStoreName), not the single-arg overload");
    }

    [Fact]
    public async Task AddAsync_DoesNotCallSingleArgOverload()
    {
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "AC-8 Test" };

        await repository.AddAsync(entity);

        _mockEventTracker.Verify(
            x => x.AddEntity(entity),
            Times.Never,
            "AddAsync must not fall back to the null-datastore single-arg overload");
    }

    // ── AC-8: DeleteAsync passes DataStoreName ───────────────────────────────

    [Fact]
    public async Task DeleteAsync_PassesDataStoreNameToTracker()
    {
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "AC-8 Delete Test" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        await repository.DeleteAsync(entity);

        _mockEventTracker.Verify(
            x => x.AddEntity(entity, DataStoreName),
            Times.Once,
            "DeleteAsync must call AddEntity(entity, DataStoreName)");
    }

    // ── AC-8: UpdateAsync passes DataStoreName ───────────────────────────────

    [Fact]
    public async Task UpdateAsync_PassesDataStoreNameToTracker()
    {
        var repository = CreateRepository();
        var entity = new TestEntity { Name = "AC-8 Update Test" };
        _dbContext.TestEntities.Add(entity);
        await _dbContext.SaveChangesAsync();

        entity.Name = "Updated";
        await repository.UpdateAsync(entity);

        _mockEventTracker.Verify(
            x => x.AddEntity(entity, DataStoreName),
            Times.Once,
            "UpdateAsync must call AddEntity(entity, DataStoreName)");
    }

    // ── AC-8: AddRangeAsync passes DataStoreName for each entity ─────────────

    [Fact]
    public async Task AddRangeAsync_PassesDataStoreNameForEachEntity()
    {
        var repository = CreateRepository();
        var entities = new[]
        {
            new TestEntity { Name = "AC-8 Range 1" },
            new TestEntity { Name = "AC-8 Range 2" }
        };

        await repository.AddRangeAsync(entities);

        _mockEventTracker.Verify(
            x => x.AddEntity(It.IsAny<TestEntity>(), DataStoreName),
            Times.Exactly(2),
            "AddRangeAsync must call AddEntity(entity, DataStoreName) for each entity");
    }

    // ── AC-8: Explicit DataStoreName set on repository is threaded ────────────

    [Fact]
    public async Task AddAsync_WithExplicitDataStoreName_PassesThatNameToTracker()
    {
        const string explicitName = "ExplicitStore";
        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDbContext>(explicitName))
            .Returns(_dbContext);

        var repository = CreateRepository();
        repository.DataStoreName = explicitName;
        var entity = new TestEntity { Name = "AC-8 Explicit" };

        await repository.AddAsync(entity);

        _mockEventTracker.Verify(
            x => x.AddEntity(entity, explicitName),
            Times.Once,
            "When DataStoreName is explicitly set, that name must reach the tracker");
    }
}
