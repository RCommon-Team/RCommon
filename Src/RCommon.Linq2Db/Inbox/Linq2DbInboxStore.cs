using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Inbox;
using RCommon.Persistence.Outbox;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db.Inbox;

/// <summary>
/// A Linq2Db implementation of <see cref="IInboxStore"/> that persists inbox messages
/// using a <see cref="RCommonDataConnection"/> resolved through the <see cref="IDataStoreFactory"/>.
/// </summary>
public class Linq2DbInboxStore : IInboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of <see cref="Linq2DbInboxStore"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDataConnection"/> for the configured data store.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <param name="outboxOptions">Options for outbox/inbox behaviour such as table name.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c> or yields a null value.</exception>
    public Linq2DbInboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _tableName = outboxOptions?.Value?.InboxTableName ?? "__InboxMessages";
    }

    /// <summary>
    /// Gets the <see cref="RCommonDataConnection"/> for the configured data store, resolved through the <see cref="IDataStoreFactory"/>.
    /// </summary>
    private RCommonDataConnection DataConnection
        => _dataStoreFactory.Resolve<RCommonDataConnection>(_dataStoreName);

    /// <summary>
    /// Gets the Linq2Db <see cref="ITable{InboxMessage}"/> scoped to the configured table name.
    /// </summary>
    private ITable<InboxMessage> Table
        => DataConnection.GetTable<InboxMessage>().TableName(_tableName);

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid messageId, string? consumerType = null, CancellationToken cancellationToken = default)
    {
        var ct = consumerType ?? "";
        return await Table
            .AnyAsync(m => m.MessageId == messageId && m.ConsumerType == ct, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RecordAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = message as InboxMessage ?? new InboxMessage
        {
            MessageId = message.MessageId,
            EventType = message.EventType,
            ConsumerType = message.ConsumerType ?? "",
            ReceivedAtUtc = message.ReceivedAtUtc
        };

        // Coalesce ConsumerType even when we reuse the original entity
        if (entity.ConsumerType is null)
        {
            entity.ConsumerType = "";
        }

        await DataConnection.InsertAsync(entity, _tableName, token: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        await Table
            .Where(m => m.ReceivedAtUtc < cutoff)
            .DeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
