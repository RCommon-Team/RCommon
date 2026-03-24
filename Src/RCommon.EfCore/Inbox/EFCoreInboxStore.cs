using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.Inbox;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Inbox;

public class EFCoreInboxStore : IInboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;

    public EFCoreInboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
    }

    private RCommonDbContext DbContext => _dataStoreFactory.Resolve<RCommonDbContext>(_dataStoreName);

    public async Task<bool> ExistsAsync(Guid messageId, string? consumerType = null, CancellationToken cancellationToken = default)
    {
        var ct = consumerType ?? "";
        return await DbContext.Set<InboxMessage>()
            .AnyAsync(m => m.MessageId == messageId && m.ConsumerType == ct, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RecordAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = new InboxMessage
        {
            MessageId = message.MessageId,
            EventType = message.EventType,
            ConsumerType = message.ConsumerType ?? "",
            ReceivedAtUtc = message.ReceivedAtUtc
        };

        DbContext.Set<InboxMessage>().Add(entity);
        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        // DateTimeOffset comparisons cannot be translated by all EF Core providers (e.g. SQLite).
        // Load all candidates and apply the filter client-side.
        var all = await DbContext.Set<InboxMessage>()
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var old = all.Where(m => m.ReceivedAtUtc < cutoff).ToList();

        DbContext.Set<InboxMessage>().RemoveRange(old);
        await DbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
