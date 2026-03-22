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

    public DapperOutboxStore(
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
        var sql = $@"INSERT INTO [{_tableName}] (Id, EventType, EventPayload, CreatedAtUtc, ProcessedAtUtc, DeadLetteredAtUtc, ErrorMessage, RetryCount, CorrelationId, TenantId)
                     VALUES (@Id, @EventType, @EventPayload, @CreatedAtUtc, @ProcessedAtUtc, @DeadLetteredAtUtc, @ErrorMessage, @RetryCount, @CorrelationId, @TenantId)";
        await db.ExecuteAsync(new CommandDefinition(sql, message, cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $@"SELECT TOP (@BatchSize) * FROM [{_tableName}]
                     WHERE ProcessedAtUtc IS NULL AND DeadLetteredAtUtc IS NULL AND RetryCount < @MaxRetries
                     ORDER BY CreatedAtUtc ASC";
        var result = await db.QueryAsync<OutboxMessage>(
            new CommandDefinition(sql, new { BatchSize = batchSize, MaxRetries = _maxRetries },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
        return result.ToList();
    }

    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET ProcessedAtUtc = @Now WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Now = DateTimeOffset.UtcNow },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"UPDATE [{_tableName}] SET ErrorMessage = @Error, RetryCount = RetryCount + 1 WHERE Id = @Id";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { Id = messageId, Error = error },
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
}
