using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Crud;
using Xunit;

namespace RCommon.EfCore.Tests;

/// <summary>
/// Integration tests that verify the complete EF Core setup and DI registration.
/// </summary>
public class EFCoreIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dataStoreName = "TestDataStore";

    public EFCoreIntegrationTests()
    {
        var services = new ServiceCollection();

        // Register logging
        services.AddLogging(builder => builder.AddDebug());

        // Register entity event tracker (mock implementation for testing)
        services.AddSingleton<IEntityEventTracker, TestEntityEventTracker>();

        // Use the EF Core persistence builder
        var builder = new EFCorePerisistenceBuilder(services);
        builder.AddDbContext<TestDbContext>(_dataStoreName, options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        builder.SetDefaultDataStore(options =>
            options.DefaultDataStoreName = _dataStoreName);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public void ServiceProvider_CanResolveTestDbContext()
    {
        // Act
        var dbContext = _serviceProvider.GetService<TestDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void ServiceProvider_CanResolveDataStoreFactory()
    {
        // Act
        var factory = _serviceProvider.GetService<IDataStoreFactory>();

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void ServiceProvider_CanResolveIReadOnlyRepository()
    {
        // Act
        var repository = _serviceProvider.GetService<IReadOnlyRepository<TestEntity>>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<EFCoreRepository<TestEntity>>();
    }

    [Fact]
    public void ServiceProvider_CanResolveIWriteOnlyRepository()
    {
        // Act
        var repository = _serviceProvider.GetService<IWriteOnlyRepository<TestEntity>>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<EFCoreRepository<TestEntity>>();
    }

    [Fact]
    public void ServiceProvider_CanResolveILinqRepository()
    {
        // Act
        var repository = _serviceProvider.GetService<ILinqRepository<TestEntity>>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<EFCoreRepository<TestEntity>>();
    }

    [Fact]
    public void ServiceProvider_CanResolveIGraphRepository()
    {
        // Act
        var repository = _serviceProvider.GetService<IGraphRepository<TestEntity>>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<EFCoreRepository<TestEntity>>();
    }

    [Fact]
    public async Task FullPipeline_CanAddAndRetrieveEntity()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ILinqRepository<TestEntity>>();
        var entity = new TestEntity { Name = "Integration Test Entity" };

        // Act
        await repository.AddAsync(entity);
        var result = await repository.FindAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Integration Test Entity");
    }

    [Fact]
    public async Task FullPipeline_CanQueryEntities()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ILinqRepository<TestEntity>>();
        await repository.AddAsync(new TestEntity { Name = "Query Test 1" });
        await repository.AddAsync(new TestEntity { Name = "Query Test 2" });
        await repository.AddAsync(new TestEntity { Name = "Other" });

        // Act
        var results = await repository.FindAsync(e => e.Name!.StartsWith("Query"));

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task FullPipeline_CanUpdateEntity()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ILinqRepository<TestEntity>>();
        var entity = new TestEntity { Name = "Original" };
        await repository.AddAsync(entity);

        // Act
        entity.Name = "Updated";
        await repository.UpdateAsync(entity);
        var result = await repository.FindAsync(entity.Id);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task FullPipeline_CanDeleteEntity()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ILinqRepository<TestEntity>>();
        var entity = new TestEntity { Name = "To Delete" };
        await repository.AddAsync(entity);

        // Act
        await repository.DeleteAsync(entity);
        var result = await repository.FindAsync(entity.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DataStoreFactory_CanResolveDbContext()
    {
        // Arrange
        var factory = _serviceProvider.GetRequiredService<IDataStoreFactory>();

        // Act
        var dbContext = factory.Resolve<RCommonDbContext>(_dataStoreName);

        // Assert
        dbContext.Should().NotBeNull();
        dbContext.Should().BeOfType<TestDbContext>();
    }

    [Fact]
    public void DefaultDataStoreOptions_IsConfigured()
    {
        // Arrange
        var options = _serviceProvider.GetRequiredService<IOptions<DefaultDataStoreOptions>>();

        // Act
        var value = options.Value;

        // Assert
        value.Should().NotBeNull();
        value.DefaultDataStoreName.Should().Be(_dataStoreName);
    }

    [Fact]
    public void Repository_UsesDefaultDataStoreName()
    {
        // Arrange & Act
        var repository = _serviceProvider.GetRequiredService<ILinqRepository<TestEntity>>() as EFCoreRepository<TestEntity>;

        // Assert
        repository.Should().NotBeNull();
        repository!.DataStoreName.Should().Be(_dataStoreName);
    }

    [Fact]
    public void NewScope_GetsNewDbContext()
    {
        // Arrange
        TestDbContext? context1;
        TestDbContext? context2;

        using (var scope1 = _serviceProvider.CreateScope())
        {
            context1 = scope1.ServiceProvider.GetService<TestDbContext>();
        }

        using (var scope2 = _serviceProvider.CreateScope())
        {
            context2 = scope2.ServiceProvider.GetService<TestDbContext>();
        }

        // Assert
        context1.Should().NotBeNull();
        context2.Should().NotBeNull();
        context1.Should().NotBeSameAs(context2);
    }

    [Fact]
    public void SameScope_GetsSameDbContext()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();

        // Act
        var context1 = scope.ServiceProvider.GetService<TestDbContext>();
        var context2 = scope.ServiceProvider.GetService<TestDbContext>();

        // Assert
        context1.Should().NotBeNull();
        context2.Should().NotBeNull();
        context1.Should().BeSameAs(context2);
    }

    [Fact]
    public async Task Repository_CanUseSpecification()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<ILinqRepository<TestEntity>>();
        await repository.AddAsync(new TestEntity { Name = "Spec Test 1", IsActive = true });
        await repository.AddAsync(new TestEntity { Name = "Spec Test 2", IsActive = false });

        var specification = new TestSpecification<TestEntity>(e => e.IsActive);

        // Act
        var results = await repository.FindAsync(specification);

        // Assert
        results.Should().HaveCount(1);
        results.First().Name.Should().Be("Spec Test 1");
    }
}

/// <summary>
/// Test implementation of IEntityEventTracker for integration tests.
/// </summary>
public class TestEntityEventTracker : IEntityEventTracker
{
    private readonly List<IBusinessEntity> _trackedEntities = new();

    public ICollection<IBusinessEntity> TrackedEntities => _trackedEntities;

    public void AddEntity(IBusinessEntity entity)
    {
        _trackedEntities.Add(entity);
    }

    public Task<bool> EmitTransactionalEventsAsync()
    {
        return Task.FromResult(true);
    }
}

/// <summary>
/// Test specification for use in integration tests.
/// </summary>
public class TestSpecification<TEntity> : ISpecification<TEntity> where TEntity : class
{
    public TestSpecification(System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate)
    {
        Predicate = predicate;
    }

    public System.Linq.Expressions.Expression<Func<TEntity, bool>> Predicate { get; }

    public bool IsSatisfiedBy(TEntity entity)
    {
        return Predicate.Compile()(entity);
    }
}
