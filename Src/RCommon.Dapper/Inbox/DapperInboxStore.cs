using Dapper;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Inbox;
using RCommon.Persistence.Outbox;
using RCommon.Persistence.Sql;
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper.Inbox;

public class DapperInboxStore : IInboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly string _tableName;

    public DapperInboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _tableName = outboxOptions?.Value?.InboxTableName ?? "__InboxMessages";
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

    public async Task<bool> ExistsAsync(Guid messageId, string? consumerType = null, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var ct = consumerType ?? "";
        var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM [{_tableName}] WHERE MessageId = @MessageId AND ConsumerType = @ConsumerType) THEN 1 ELSE 0 END";
        return await db.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { MessageId = messageId, ConsumerType = ct },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task RecordAsync(IInboxMessage message, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var sql = $"INSERT INTO [{_tableName}] (MessageId, EventType, ConsumerType, ReceivedAtUtc) VALUES (@MessageId, @EventType, @ConsumerType, @ReceivedAtUtc)";
        await db.ExecuteAsync(new CommandDefinition(sql,
            new { message.MessageId, message.EventType, ConsumerType = message.ConsumerType ?? "", message.ReceivedAtUtc },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    public async Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        await using var db = await GetOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var sql = $"DELETE FROM [{_tableName}] WHERE ReceivedAtUtc < @Cutoff";
        await db.ExecuteAsync(new CommandDefinition(sql, new { Cutoff = cutoff },
            cancellationToken: cancellationToken)).ConfigureAwait(false);
    }
}
