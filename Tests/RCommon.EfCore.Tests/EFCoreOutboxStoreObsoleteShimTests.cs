using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Outbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.EfCore.Tests;

public class EFCoreOutboxStoreObsoleteShimTests : IDisposable
{
    private readonly TestOutboxDbContext _dbContext;

    public EFCoreOutboxStoreObsoleteShimTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new TestOutboxDbContext(dbOptions);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void EFCoreOutboxStoreOfT_IsMarkedObsolete_WithNonEmptyMessage()
    {
        // Arrange / Act
        var attr = typeof(EFCoreOutboxStore<TestOutboxDbContext>)
            .GetCustomAttribute<ObsoleteAttribute>();

        // Assert
        attr.Should().NotBeNull("EFCoreOutboxStore<TContext> must carry [Obsolete]");
        attr!.Message.Should().NotBeNullOrWhiteSpace(
            "the [Obsolete] message must provide migration guidance");
    }

    [Fact]
    public void EFCoreOutboxStoreOfT_ImplementsIOutboxStore()
    {
        typeof(EFCoreOutboxStore<TestOutboxDbContext>)
            .Should().Implement<IOutboxStore>(
                "EFCoreOutboxStore<TContext> must satisfy the IOutboxStore contract");
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [Fact]
    public async Task EFCoreOutboxStoreOfT_SaveAsync_DelegatesToBase_AndPersists()
    {
        // Arrange — wire the factory mock to return our in-memory context
        var factoryMock = new Mock<IDataStoreFactory>();
        factoryMock.Setup(f => f.Resolve<RCommonDbContext>(It.IsAny<string>()))
            .Returns(_dbContext);

        var outboxOpts = Options.Create(new OutboxOptions { MaxRetries = 3 });

        var shimStore = new EFCoreOutboxStore<TestOutboxDbContext>(
            factoryMock.Object, outboxOpts);

        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "Shim.Test.Event",
            EventPayload = "{\"shim\":true}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // Act
        await shimStore.SaveAsync(msg, "test");

        // Assert — message must be persisted via the base implementation
        var count = await _dbContext.Set<OutboxMessage>().CountAsync();
        count.Should().Be(1, "SaveAsync on the shim must delegate to the base and persist the message");
    }
#pragma warning restore CS0618 // Type or member is obsolete

    public void Dispose() => _dbContext.Dispose();
}
