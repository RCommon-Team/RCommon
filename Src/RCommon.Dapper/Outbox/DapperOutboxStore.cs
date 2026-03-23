using Dapper;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper.Outbox;

public class DapperOutboxStore : IOutboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly string _tableName;
    private readonly int _maxRetries;
    private readonly ILockStatementProvider _lockProvider;

    public DapperOutboxStore(
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

    private async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var dataStore = _dataStoreFactory.Resolve<RDbConnection>(_dataStoreName);
        var connection = dataStore.GetDbConnection();
        if (connection.State == ConnectionState.Closed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        return connection;
    }

    public async Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $@"INSERT INTO [{_tableName}] (Id, EventType, EventPayload, CreatedAtUtc, ProcessedAtUtc, DeadLetteredAtUtc, ErrorMessage, RetryCount, CorrelationId, TenantId, NextRetryAtUtc, LockedByInstanceId, LockedUntilUtc)
                     VALUES (@Id, @EventType, @EventPayload, @CreatedAtUtc, @ProcessedAtUtc, @DeadLetteredAtUtc, @ErrorMessage, @RetryCount, @CorrelationId, @TenantId, @NextRetryAtUtc, @LockedByInstanceId, @LockedUntilUtc)";
        await db.ExecuteAsync(new CommandDefinition(sql, message, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET ProcessedAtUtc = @Now WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Now = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET ErrorMessage = @Error, RetryCount = RetryCount + 1, NextRetryAtUtc = @NextRetryAtUtc, LockedByInstanceId = NULL, LockedUntilUtc = NULL WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Error = error, NextRetryAtUtc = nextRetryAtUtc },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET DeadLetteredAtUtc = @Now WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Now = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var sql = $"DELETE FROM [{_tableName}] WHERE ProcessedAtUtc IS NOT NULL AND ProcessedAtUtc < @Cutoff";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Cutoff = cutoff },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var sql = $"DELETE FROM [{_tableName}] WHERE DeadLetteredAtUtc IS NOT NULL AND DeadLetteredAtUtc < @Cutoff";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Cutoff = cutoff },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var lockUntil = now + lockDuration;

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
        else // Default: SQL Server
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

        var result = await db.QueryAsync<OutboxMessage>(
            new CommandDefinition(sql,
                new { BatchSize = batchSize, MaxRetries = _maxRetries, Now = now, InstanceId = instanceId, LockUntil = lockUntil },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return result.ToList();
    }

    public async Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset = 0, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        string sql;
        if (_lockProvider.ProviderName == "PostgreSql")
        {
            sql = $@"SELECT * FROM ""{_tableName}"" WHERE ""DeadLetteredAtUtc"" IS NOT NULL ORDER BY ""DeadLetteredAtUtc"" DESC LIMIT @BatchSize OFFSET @Offset";
        }
        else
        {
            sql = $@"SELECT * FROM [{_tableName}] WHERE DeadLetteredAtUtc IS NOT NULL ORDER BY DeadLetteredAtUtc DESC OFFSET @Offset ROWS FETCH NEXT @BatchSize ROWS ONLY";
        }
        var result = await db.QueryAsync<OutboxMessage>(
            new CommandDefinition(sql, new { BatchSize = batchSize, Offset = offset },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return result.ToList();
    }

    public async Task ReplayDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $@"UPDATE [{_tableName}] SET DeadLetteredAtUtc = NULL, ProcessedAtUtc = NULL, ErrorMessage = NULL, RetryCount = 0, NextRetryAtUtc = NULL, LockedByInstanceId = NULL, LockedUntilUtc = NULL
                     WHERE Id = @Id AND DeadLetteredAtUtc IS NOT NULL";
        var rows = await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (rows == 0)
        {
            throw new InvalidOperationException($"Message {messageId} does not exist or is not dead-lettered.");
        }
    }
}
