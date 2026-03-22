using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db.Outbox;

/// <summary>
/// A Linq2Db implementation of <see cref="IOutboxStore"/> that persists outbox messages
/// using a <see cref="RCommonDataConnection"/> resolved through the <see cref="IDataStoreFactory"/>.
/// </summary>
public class Linq2DbOutboxStore : IOutboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly string _tableName;
    private readonly int _maxRetries;

    /// <summary>
    /// Initializes a new instance of <see cref="Linq2DbOutboxStore"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDataConnection"/> for the configured data store.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <param name="outboxOptions">Options for outbox behaviour such as table name and max retries.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c> or yields a null value.</exception>
    public Linq2DbOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _tableName = outboxOptions?.Value?.TableName ?? "__OutboxMessages";
        _maxRetries = outboxOptions?.Value?.MaxRetries ?? 5;
    }

    /// <summary>
    /// Gets the <see cref="RCommonDataConnection"/> for the configured data store, resolved through the <see cref="IDataStoreFactory"/>.
    /// </summary>
    private RCommonDataConnection DataConnection
        => _dataStoreFactory.Resolve<RCommonDataConnection>(_dataStoreName);

    /// <summary>
    /// Gets the Linq2Db <see cref="ITable{OutboxMessage}"/> scoped to the configured table name.
    /// </summary>
    private ITable<OutboxMessage> Table
        => DataConnection.GetTable<OutboxMessage>().TableName(_tableName);

    /// <inheritdoc />
    public async Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = message as OutboxMessage ?? new OutboxMessage
        {
            Id = message.Id,
            EventType = message.EventType,
            EventPayload = message.EventPayload,
            CreatedAtUtc = message.CreatedAtUtc,
            ProcessedAtUtc = message.ProcessedAtUtc,
            DeadLetteredAtUtc = message.DeadLetteredAtUtc,
            ErrorMessage = message.ErrorMessage,
            RetryCount = message.RetryCount,
            CorrelationId = message.CorrelationId,
            TenantId = message.TenantId
        };
        await DataConnection.InsertAsync(entity, _tableName, token: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await Table
            .Where(m => m.ProcessedAtUtc == null
                && m.DeadLetteredAtUtc == null
                && m.RetryCount < _maxRetries)
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await Table
            .Where(m => m.Id == messageId)
            .Set(m => m.ProcessedAtUtc, DateTimeOffset.UtcNow)
            .UpdateAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        await Table
            .Where(m => m.Id == messageId)
            .Set(m => m.ErrorMessage, error)
            .Set(m => m.RetryCount, m => m.RetryCount + 1)
            .UpdateAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await Table
            .Where(m => m.Id == messageId)
            .Set(m => m.DeadLetteredAtUtc, DateTimeOffset.UtcNow)
            .UpdateAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        await Table
            .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < cutoff)
            .DeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        await Table
            .Where(m => m.DeadLetteredAtUtc != null && m.DeadLetteredAtUtc < cutoff)
            .DeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
