using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
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
    private readonly ILockStatementProvider _lockProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="Linq2DbOutboxStore"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDataConnection"/> for the configured data store.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <param name="outboxOptions">Options for outbox behaviour such as table name and max retries.</param>
    /// <param name="lockStatementProvider">Provider that determines the SQL locking dialect to use for <see cref="ClaimAsync"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c> or yields a null value.</exception>
    public Linq2DbOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions,
        ILockStatementProvider lockStatementProvider)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _tableName = outboxOptions?.Value?.TableName ?? "__OutboxMessages";
        _maxRetries = outboxOptions?.Value?.MaxRetries ?? 5;
        _lockProvider = lockStatementProvider ?? throw new ArgumentNullException(nameof(lockStatementProvider));
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
            TenantId = message.TenantId,
            NextRetryAtUtc = message.NextRetryAtUtc,
            LockedByInstanceId = message.LockedByInstanceId,
            LockedUntilUtc = message.LockedUntilUtc
        };
        await DataConnection.InsertAsync(entity, _tableName, token: cancellationToken).ConfigureAwait(false);
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
    public async Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, CancellationToken cancellationToken = default)
    {
        await Table
            .Where(m => m.Id == messageId)
            .Set(m => m.ErrorMessage, error)
            .Set(m => m.RetryCount, m => m.RetryCount + 1)
            .Set(m => m.NextRetryAtUtc, nextRetryAtUtc)
            .Set(m => m.LockedByInstanceId, (string?)null)
            .Set(m => m.LockedUntilUtc, (DateTimeOffset?)null)
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

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var lockUntil = now + lockDuration;
        var dc = DataConnection;

        string sql;
        if (_lockProvider.ProviderName == "PostgreSql")
        {
            sql = $@"
                UPDATE ""{_tableName}"" o
                SET ""LockedByInstanceId"" = @InstanceId, ""LockedUntilUtc"" = @LockUntil
                FROM (
                    SELECT ""Id"" FROM ""{_tableName}""
                    WHERE ""ProcessedAtUtc"" IS NULL
                      AND ""DeadLetteredAtUtc"" IS NULL
                      AND ""RetryCount"" < @MaxRetries
                      AND (""NextRetryAtUtc"" IS NULL OR ""NextRetryAtUtc"" <= @Now)
                      AND (""LockedUntilUtc"" IS NULL OR ""LockedUntilUtc"" <= @Now)
                    ORDER BY ""CreatedAtUtc""
                    LIMIT @BatchSize
                    FOR UPDATE SKIP LOCKED
                ) AS batch
                WHERE o.""Id"" = batch.""Id""
                RETURNING o.*";
        }
        else // SQL Server
        {
            sql = $@"
                WITH batch AS (
                    SELECT TOP (@BatchSize) Id
                    FROM [{_tableName}] WITH (UPDLOCK, ROWLOCK, READPAST)
                    WHERE ProcessedAtUtc IS NULL
                      AND DeadLetteredAtUtc IS NULL
                      AND RetryCount < @MaxRetries
                      AND (NextRetryAtUtc IS NULL OR NextRetryAtUtc <= @Now)
                      AND (LockedUntilUtc IS NULL OR LockedUntilUtc <= @Now)
                    ORDER BY CreatedAtUtc
                )
                UPDATE o
                SET o.LockedByInstanceId = @InstanceId, o.LockedUntilUtc = @LockUntil
                OUTPUT INSERTED.*
                FROM [{_tableName}] o
                INNER JOIN batch ON o.Id = batch.Id";
        }

        var result = await dc.QueryToListAsync<OutboxMessage>(
            sql,
            new { BatchSize = batchSize, MaxRetries = _maxRetries, Now = now, InstanceId = instanceId, LockUntil = lockUntil },
            cancellationToken).ConfigureAwait(false);
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset = 0, CancellationToken cancellationToken = default)
    {
        return await Table
            .Where(m => m.DeadLetteredAtUtc != null)
            .OrderByDescending(m => m.DeadLetteredAtUtc)
            .Skip(offset)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReplayDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var rows = await Table
            .Where(m => m.Id == messageId && m.DeadLetteredAtUtc != null)
            .Set(m => m.DeadLetteredAtUtc, (DateTimeOffset?)null)
            .Set(m => m.ProcessedAtUtc, (DateTimeOffset?)null)
            .Set(m => m.ErrorMessage, (string?)null)
            .Set(m => m.RetryCount, 0)
            .Set(m => m.NextRetryAtUtc, (DateTimeOffset?)null)
            .Set(m => m.LockedByInstanceId, (string?)null)
            .Set(m => m.LockedUntilUtc, (DateTimeOffset?)null)
            .UpdateAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows == 0)
        {
            throw new InvalidOperationException($"Message {messageId} does not exist or is not dead-lettered.");
        }
    }
}
