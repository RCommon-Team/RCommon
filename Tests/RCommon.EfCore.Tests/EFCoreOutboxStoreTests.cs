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
    public async Task GetPendingAsync_ExcludesProcessedDeadLetteredAndMaxRetries()
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
            RetryCount = 3
        };
        _dbContext.Set<OutboxMessage>().AddRange(pending, processed, deadLettered, maxedOut);
        await _dbContext.SaveChangesAsync();

        var result = await _store.GetPendingAsync(100);
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(pending.Id);
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
            RetryCount = 1
        };
        _dbContext.Set<OutboxMessage>().Add(msg);
        await _dbContext.SaveChangesAsync();

        await _store.MarkFailedAsync(msg.Id, "error");

        var updated = await _dbContext.Set<OutboxMessage>().FindAsync(msg.Id);
        updated!.RetryCount.Should().Be(2);
        updated.ErrorMessage.Should().Be("error");
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

    public void Dispose() => _dbContext.Dispose();
}
