using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Outbox;

namespace RCommon.Persistence.EFCore.Outbox;

/// <summary>
/// An EF Core implementation of <see cref="IOutboxStore"/> that persists outbox messages
/// using a <see cref="RCommonDbContext"/> resolved through the <see cref="IDataStoreFactory"/>.
/// </summary>
public class EFCoreOutboxStore : IOutboxStore
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly string _dataStoreName;
    private readonly int _maxRetries;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreOutboxStore"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDbContext"/> for the configured data store.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <param name="outboxOptions">Options configuring outbox behavior such as maximum retries.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public EFCoreOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _dataStoreName = defaultDataStoreOptions?.Value?.DefaultDataStoreName
            ?? throw new ArgumentNullException(nameof(defaultDataStoreOptions));
        _maxRetries = outboxOptions?.Value?.MaxRetries ?? 5;
        _tableName = outboxOptions?.Value?.TableName ?? "__OutboxMessages";
    }

    /// <summary>
    /// Gets the <see cref="RCommonDbContext"/> for the configured data store, resolved through the <see cref="IDataStoreFactory"/>.
    /// </summary>
    private RCommonDbContext DbContext => _dataStoreFactory.Resolve<RCommonDbContext>(_dataStoreName);

    /// <inheritdoc />
    public async Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        if (message is OutboxMessage entity)
        {
            dbContext.Set<OutboxMessage>().Add(entity);
        }
        else
        {
            dbContext.Set<OutboxMessage>().Add(new OutboxMessage
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
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var now = DateTimeOffset.UtcNow;
        var lockUntil = now + lockDuration;

        // For SQL Server and PostgreSQL, raw SQL would be used here (CTE + OUTPUT / FOR UPDATE SKIP LOCKED).
        // For SQLite and other providers, use a LINQ-based fallback (not safe for concurrent production use).
        var maxRetries = _maxRetries;
        // Broad server-side filter on non-nullable fields + simple nullable null checks.
        // Nullable DateTimeOffset comparisons (e.g. <= now) are evaluated client-side
        // since SQLite EF Core provider cannot translate Nullable<DateTimeOffset> comparisons.
        var candidates = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAtUtc == null
                && m.DeadLetteredAtUtc == null
                && m.RetryCount < maxRetries)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var pending = candidates
            .Where(m => (m.NextRetryAtUtc == null || m.NextRetryAtUtc <= now)
                && (m.LockedUntilUtc == null || m.LockedUntilUtc <= now))
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToList();

        foreach (var m in pending)
        {
            m.LockedByInstanceId = instanceId;
            m.LockedUntilUtc = lockUntil;
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return pending;
    }

    /// <inheritdoc />
    public async Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.ProcessedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.ErrorMessage = error;
            message.RetryCount++;
            message.NextRetryAtUtc = nextRetryAtUtc;
            message.LockedByInstanceId = null;
            message.LockedUntilUtc = null;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.DeadLetteredAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset = 0, CancellationToken cancellationToken = default)
    {
        var results = await DbContext.Set<OutboxMessage>()
            .Where(m => m.DeadLetteredAtUtc != null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return results
            .OrderByDescending(m => m.DeadLetteredAtUtc)
            .Skip(offset)
            .Take(batchSize)
            .ToList();
    }

    /// <inheritdoc />
    public async Task ReplayDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken).ConfigureAwait(false);

        if (message == null || message.DeadLetteredAtUtc == null)
        {
            throw new InvalidOperationException($"Message {messageId} does not exist or is not dead-lettered.");
        }

        message.DeadLetteredAtUtc = null;
        message.ProcessedAtUtc = null;
        message.ErrorMessage = null;
        message.RetryCount = 0;
        message.NextRetryAtUtc = null;
        message.LockedByInstanceId = null;
        message.LockedUntilUtc = null;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var old = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAtUtc != null && m.ProcessedAtUtc < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Set<OutboxMessage>().RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        var old = await dbContext.Set<OutboxMessage>()
            .Where(m => m.DeadLetteredAtUtc != null && m.DeadLetteredAtUtc < cutoff)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        dbContext.Set<OutboxMessage>().RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
