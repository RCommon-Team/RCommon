using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using Xunit;

namespace RCommon.EfCore.Tests;

public class RCommonDbContextTests
{
    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        context.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new TestDbContextWithNullOptions();

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RCommonDbContext_ImplementsIDataStore()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        context.Should().BeAssignableTo<IDataStore>();
    }

    [Fact]
    public void RCommonDbContext_ImplementsDbContext()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        using var context = new TestDbContext(options);

        // Assert
        context.Should().BeAssignableTo<DbContext>();
    }

    [Fact(Skip = "InMemory provider does not support relational operations like GetDbConnection")]
    public void GetDbConnection_ReturnsDbConnection()
    {
        // Note: This test requires a relational database provider (SQL Server, PostgreSQL, etc.)
        // InMemory provider throws: "Relational-specific methods can only be used when the context is using a relational database provider"
    }

    [Fact]
    public void DbSet_TestEntities_CanBeAccessed()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var dbSet = context.TestEntities;

        // Assert
        dbSet.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithNewEntity_PersistsEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        var entity = new TestEntity { Name = "Test" };

        // Act
        context.TestEntities.Add(entity);
        var result = await context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        context.TestEntities.Should().ContainSingle();
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_PersistsAllEntities()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        var entities = new[]
        {
            new TestEntity { Name = "Test1" },
            new TestEntity { Name = "Test2" },
            new TestEntity { Name = "Test3" }
        };

        // Act
        context.TestEntities.AddRange(entities);
        var result = await context.SaveChangesAsync();

        // Assert
        result.Should().Be(3);
        context.TestEntities.Should().HaveCount(3);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledSafely()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);

        // Act
        var action = async () => await context.DisposeAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public void Database_Property_ReturnsNonNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var database = context.Database;

        // Assert
        database.Should().NotBeNull();
    }

    [Fact]
    public void ChangeTracker_Property_ReturnsNonNull()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var changeTracker = context.ChangeTracker;

        // Assert
        changeTracker.Should().NotBeNull();
    }

    [Fact]
    public void Set_WithValidEntity_ReturnsDbSet()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        // Act
        var dbSet = context.Set<TestEntity>();

        // Assert
        dbSet.Should().NotBeNull();
    }
}

/// <summary>
/// Test helper class to verify null options handling.
/// </summary>
public class TestDbContextWithNullOptions : RCommonDbContext
{
    public TestDbContextWithNullOptions() : base(null!)
    {
    }
}
