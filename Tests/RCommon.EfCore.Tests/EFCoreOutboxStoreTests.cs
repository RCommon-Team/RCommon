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

public class TestOutboxDbContext : RCommonDbContext
{
    public TestOutboxDbContext(DbContextOptions options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddOutboxMessages();
        modelBuilder.AddInboxMessages();
    }
}

public class EFCoreOutboxStoreTests : IDisposable
{
    private readonly TestOutboxDbContext _dbContext;
    private readonly EFCoreOutboxStore _store;

    public EFCoreOutboxStoreTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TestOutboxDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _dbContext = new TestOutboxDbContext(dbOptions);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();

        var factoryMock = new Mock<IDataStoreFactory>();
        factoryMock.Setup(f => f.Resolve<RCommonDbContext>(It.IsAny<string>()))
            .Returns(_dbContext);
        var defaultOpts = Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" });
        var outboxOpts = Options.Create(new OutboxOptions { MaxRetries = 3 });

        _store = new EFCoreOutboxStore(factoryMock.Object, defaultOpts, outboxOpts);
    }

    [Fact]
    public async Task SaveAsync_PersistsMessage()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "Test.Event",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        await _store.SaveAsync(msg);
        var count = await _dbContext.Set<OutboxMessage>().CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task MarkProcessedAsync_SetsProcessedAtUtc()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        await _store.MarkProcessedAsync(msg.Id);

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_IncrementsRetryCountAndSetsError()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 1,
            LockedByInstanceId = "instance-1",
            LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        var nextRetry = DateTimeOffset.UtcNow.AddMinutes(10);
        await _store.MarkFailedAsync(msg.Id, "error", nextRetry);

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.RetryCount.Should().Be(2);
        updated.ErrorMessage.Should().Be("error");
        updated.NextRetryAtUtc.Should().NotBeNull();
        updated.NextRetryAtUtc!.Value.Should().BeCloseTo(nextRetry, TimeSpan.FromSeconds(1));
        updated.LockedByInstanceId.Should().BeNull();
        updated.LockedUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task MarkDeadLetteredAsync_SetsDeadLetteredAtUtc()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        await _store.MarkDeadLetteredAsync(msg.Id);

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.DeadLetteredAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ClaimAsync_FiltersCorrectly()
    {
        var pending = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0
        };
        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        };
        var deadLettered = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DeadLetteredAtUtc = DateTimeOffset.UtcNow
        };
        var maxedOut = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 3  // equals MaxRetries = 3, so excluded
        };
        _dbContext.Set<OutboxMessage>().AddRange(pending, processed, deadLettered, maxedOut);
        await _dbContext.SaveChangesAsync();

        var result = await _store.ClaimAsync("instance-1", 100, TimeSpan.FromMinutes(5));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(pending.Id);
        result[0].LockedByInstanceId.Should().Be("instance-1");
        result[0].LockedUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ClaimAsync_RespectsNextRetryAtUtc()
    {
        var futureRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 1,
            NextRetryAtUtc = DateTimeOffset.UtcNow.AddMinutes(10)  // in the future — should NOT be claimed
        };
        var readyRetry = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 1,
            NextRetryAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1)  // in the past — should be claimed
        };
        _dbContext.Set<OutboxMessage>().AddRange(futureRetry, readyRetry);
        await _dbContext.SaveChangesAsync();

        var result = await _store.ClaimAsync("instance-1", 100, TimeSpan.FromMinutes(5));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(readyRetry.Id);
    }

    [Fact]
    public async Task ClaimAsync_RespectsLockedUntilUtc()
    {
        var locked = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0,
            LockedByInstanceId = "other-instance",
            LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(5)  // lock not expired — should NOT be claimed
        };
        var expiredLock = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RetryCount = 0,
            LockedByInstanceId = "other-instance",
            LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(-1)  // lock expired — should be claimed
        };
        _dbContext.Set<OutboxMessage>().AddRange(locked, expiredLock);
        await _dbContext.SaveChangesAsync();

        var result = await _store.ClaimAsync("instance-1", 100, TimeSpan.FromMinutes(5));

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(expiredLock.Id);
        result[0].LockedByInstanceId.Should().Be("instance-1");
    }

    [Fact]
    public async Task GetDeadLettersAsync_ReturnsOnlyDeadLettered()
    {
        var deadLettered = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DeadLetteredAtUtc = DateTimeOffset.UtcNow
        };
        var pending = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var processed = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ProcessedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.Set<OutboxMessage>().AddRange(deadLettered, pending, processed);
        await _dbContext.SaveChangesAsync();

        var result = await _store.GetDeadLettersAsync(100);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(deadLettered.Id);
    }

    [Fact]
    public async Task GetDeadLettersAsync_PaginatesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        var messages = Enumerable.Range(0, 5).Select(i => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = now,
            DeadLetteredAtUtc = now.AddMinutes(-i)  // different times so ordering is deterministic
        }).ToList();

        _dbContext.Set<OutboxMessage>().AddRange(messages);
        await _dbContext.SaveChangesAsync();

        // Ordered descending by DeadLetteredAtUtc, skip 2, take 2
        var result = await _store.GetDeadLettersAsync(batchSize: 2, offset: 2);

        result.Should().HaveCount(2);
        // The 3rd and 4th most recently dead-lettered messages (index 2 and 3 in descending order)
        result[0].Id.Should().Be(messages[2].Id);
        result[1].Id.Should().Be(messages[3].Id);
    }

    [Fact]
    public async Task ReplayDeadLetterAsync_ResetsAllFields()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            DeadLetteredAtUtc = DateTimeOffset.UtcNow,
            ProcessedAtUtc = DateTimeOffset.UtcNow,
            ErrorMessage = "some error",
            RetryCount = 3,
            NextRetryAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            LockedByInstanceId = "instance-1",
            LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        await _store.ReplayDeadLetterAsync(msg.Id);

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.DeadLetteredAtUtc.Should().BeNull();
        updated.ProcessedAtUtc.Should().BeNull();
        updated.ErrorMessage.Should().BeNull();
        updated.RetryCount.Should().Be(0);
        updated.NextRetryAtUtc.Should().BeNull();
        updated.LockedByInstanceId.Should().BeNull();
        updated.LockedUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task ReplayDeadLetterAsync_ThrowsForNonExistent()
    {
        var nonExistentId = Guid.NewGuid();

        var act = async () => await _store.ReplayDeadLetterAsync(nonExistentId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistentId}*");
    }

    [Fact]
    public async Task ReplayDeadLetterAsync_ThrowsForNonDeadLettered()
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "T",
            EventPayload = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow
            // DeadLetteredAtUtc is null — not dead-lettered
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        var act = async () => await _store.ReplayDeadLetterAsync(msg.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{msg.Id}*");
    }

    public void Dispose() => _dbContext.Dispose();
}
