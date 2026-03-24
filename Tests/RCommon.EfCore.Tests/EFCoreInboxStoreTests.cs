using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Inbox;
using RCommon.Persistence.Inbox;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.EfCore.Tests;

public class EFCoreInboxStoreTests : IDisposable
{
    private readonly TestOutboxDbContext _dbContext;
    private readonly EFCoreInboxStore _store;

    public EFCoreInboxStoreTests()
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

        var defaultOptions = Options.Create(new DefaultDataStoreOptions { DefaultDataStoreName = "test" });
        var outboxOptions = Options.Create(new OutboxOptions());

        _store = new EFCoreInboxStore(factoryMock.Object, defaultOptions, outboxOptions);
    }

    [Fact]
    public async Task ExistsAsync_NoRecord_ReturnsFalse()
    {
        var result = await _store.ExistsAsync(Guid.NewGuid(), "TestConsumer");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RecordAsync_ThenExistsAsync_ReturnsTrue()
    {
        var messageId = Guid.NewGuid();
        await _store.RecordAsync(new InboxMessage
        {
            MessageId = messageId,
            EventType = "TestEvent",
            ConsumerType = "TestConsumer",
            ReceivedAtUtc = DateTimeOffset.UtcNow
        });

        var result = await _store.ExistsAsync(messageId, "TestConsumer");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_DifferentConsumer_ReturnsFalse()
    {
        var messageId = Guid.NewGuid();
        await _store.RecordAsync(new InboxMessage
        {
            MessageId = messageId,
            EventType = "TestEvent",
            ConsumerType = "ConsumerA",
            ReceivedAtUtc = DateTimeOffset.UtcNow
        });

        var result = await _store.ExistsAsync(messageId, "ConsumerB");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupAsync_RemovesOldEntries()
    {
        var old = new InboxMessage
        {
            MessageId = Guid.NewGuid(),
            EventType = "TestEvent",
            ConsumerType = "TestConsumer",
            ReceivedAtUtc = DateTimeOffset.UtcNow.AddDays(-10)
        };
        await _store.RecordAsync(old);

        var recent = new InboxMessage
        {
            MessageId = Guid.NewGuid(),
            EventType = "TestEvent",
            ConsumerType = "TestConsumer",
            ReceivedAtUtc = DateTimeOffset.UtcNow
        };
        await _store.RecordAsync(recent);

        await _store.CleanupAsync(TimeSpan.FromDays(7));

        (await _store.ExistsAsync(old.MessageId, "TestConsumer")).Should().BeFalse();
        (await _store.ExistsAsync(recent.MessageId, "TestConsumer")).Should().BeTrue();
    }

    public void Dispose() => _dbContext?.Dispose();
}
