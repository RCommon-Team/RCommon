using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
/// End-to-end coverage for docs/specs/multi-tenancy/multi-tenancy.md's TenantScope.Bypass()
/// primitive against a real EFCoreRepository/RCommonDbContext, per Testing Strategy items 4 and 5:
/// a query executed inside a bypass scope returns rows across all tenants, and AddAsync executed
/// inside a bypass scope does not overwrite an aggregate's already-set TenantId (the tenant
/// bootstrap scenario).
/// </summary>
public class MultiTenantBypassTests : IDisposable
{
    private readonly Mock<IDataStoreFactory> _mockDataStoreFactory;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IEntityEventTracker> _mockEventTracker;
    private readonly Mock<IOptions<DefaultDataStoreOptions>> _mockDefaultDataStoreOptions;
    private readonly StubTenantIdAccessor _stubInnerAccessor;
    private readonly TenantScopeAwareTenantIdAccessor _tenantIdAccessor;
    private readonly BypassTestDbContext _dbContext;
    private readonly string _dataStoreName = "TestDataStore";

    public MultiTenantBypassTests()
    {
        _mockDataStoreFactory = new Mock<IDataStoreFactory>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger>();
        _mockEventTracker = new Mock<IEntityEventTracker>();
        _mockDefaultDataStoreOptions = new Mock<IOptions<DefaultDataStoreOptions>>();
        _stubInnerAccessor = new StubTenantIdAccessor { TenantId = "tenant-1" };
        _tenantIdAccessor = new TenantScopeAwareTenantIdAccessor(_stubInnerAccessor);

        var defaultOptions = new DefaultDataStoreOptions { DefaultDataStoreName = _dataStoreName };
        _mockDefaultDataStoreOptions.Setup(x => x.Value).Returns(defaultOptions);
        _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);

        var options = new DbContextOptionsBuilder<BypassTestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new BypassTestDbContext(options);

        _mockDataStoreFactory
            .Setup(x => x.Resolve<RCommonDbContext>(_dataStoreName))
            .Returns(_dbContext);
    }

    public void Dispose() => _dbContext?.Dispose();

    private EFCoreRepository<BypassTestEntity> CreateRepository()
        => new EFCoreRepository<BypassTestEntity>(
            _mockDataStoreFactory.Object,
            _mockLoggerFactory.Object,
            _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object,
            _tenantIdAccessor);

    [Fact]
    public async Task FindAsync_InsideBypassScope_ReturnsRowsAcrossAllTenants()
    {
        // Arrange -- seed rows for two different tenants using an unfiltered repository instance
        var seedRepository = new EFCoreRepository<BypassTestEntity>(
            _mockDataStoreFactory.Object, _mockLoggerFactory.Object, _mockEventTracker.Object,
            _mockDefaultDataStoreOptions.Object, new StubTenantIdAccessor { TenantId = null });
        await seedRepository.AddAsync(new BypassTestEntity { Name = "T1", TenantId = "tenant-1" });
        await seedRepository.AddAsync(new BypassTestEntity { Name = "T2", TenantId = "tenant-2" });

        var repository = CreateRepository(); // ambient tenant is "tenant-1"

        // Act
        var withoutBypass = await repository.FindAsync(e => true);

        ICollection<BypassTestEntity> withBypass;
        using (TenantScope.Bypass())
        {
            withBypass = await repository.FindAsync(e => true);
        }

        // Assert
        withoutBypass.Should().HaveCount(1);
        withoutBypass.Should().OnlyContain(e => e.TenantId == "tenant-1");

        withBypass.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddAsync_InsideBypassScope_DoesNotOverwriteAlreadySetTenantId()
    {
        // Arrange -- the tenant-bootstrap scenario: an explicitly-set TenantId must survive
        // AddAsync inside a bypass scope, since the bypass suspends stamping entirely rather
        // than stamping null or the ambient tenant.
        var repository = CreateRepository(); // ambient tenant is "tenant-1"
        var newTenantRow = new BypassTestEntity { Name = "Brand New Tenant", TenantId = "brand-new-tenant" };

        // Act
        using (TenantScope.Bypass())
        {
            await repository.AddAsync(newTenantRow);
        }

        // Assert -- not overwritten with "tenant-1" (the ambient ID) and not nulled out
        newTenantRow.TenantId.Should().Be("brand-new-tenant");
    }

    private class StubTenantIdAccessor : ITenantIdAccessor
    {
        public string? TenantId { get; set; }
        public string? GetTenantId() => TenantId;
    }

    public class BypassTestEntity : BusinessEntity<Guid>, IMultiTenant
    {
        public string Name { get; set; } = string.Empty;
        public string? TenantId { get; set; }
    }

    public class BypassTestDbContext : RCommonDbContext
    {
        public BypassTestDbContext(DbContextOptions<BypassTestDbContext> options) : base(options) { }

        public DbSet<BypassTestEntity> Entities => Set<BypassTestEntity>();
    }
}
