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
    private readonly int _maxRetries;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreOutboxStore"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDbContext"/> for a given data store name per call.</param>
    /// <param name="outboxOptions">Options configuring outbox behavior such as maximum retries.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public EFCoreOutboxStore(
        IDataStoreFactory dataStoreFactory,
        IOptions<OutboxOptions> outboxOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _maxRetries = outboxOptions?.Value?.MaxRetries ?? 5;
        _tableName = outboxOptions?.Value?.TableName ?? "__OutboxMessages";
    }

    /// <summary>
    /// Resolves the <see cref="RCommonDbContext"/> for the named data store through the <see cref="IDataStoreFactory"/>.
    /// </summary>
    private RCommonDbContext Context(string name) => _dataStoreFactory.Resolve<RCommonDbContext>(name);

    /// <inheritdoc />
    public async Task SaveAsync(IOutboxMessage message, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
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
    public async Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
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
    public async Task MarkProcessedAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.ProcessedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
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
    public async Task MarkDeadLetteredAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.DeadLetteredAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var results = await Context(dataStoreName).Set<OutboxMessage>()
            .Where(m => m.DeadLetteredAtUtc != null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return results
            .OrderByDescending(m => m.DeadLetteredAtUtc)
            .Skip(offset)
            .Take(batchSize)
            .ToList();
    }

    /// <inheritdoc />
    public async Task ReplayDeadLetterAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
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
    public async Task DeleteProcessedAsync(TimeSpan olderThan, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        // Filter the null-check in the database, then compare DateTimeOffset client-side. The SQLite
        // provider cannot translate ordering comparisons on DateTimeOffset; this mirrors the same
        // approach used by ClaimAsync and stays correct on SQL Server / PostgreSQL.
        var candidates = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAtUtc != null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var old = candidates.Where(m => m.ProcessedAtUtc < cutoff).ToList();
        dbContext.Set<OutboxMessage>().RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteDeadLetteredAsync(TimeSpan olderThan, string dataStoreName, CancellationToken cancellationToken = default)
    {
        var dbContext = Context(dataStoreName);
        var cutoff = DateTimeOffset.UtcNow - olderThan;
        // See DeleteProcessedAsync: DateTimeOffset comparison is evaluated client-side for SQLite
        // compatibility while the null-check runs in the database.
        var candidates = await dbContext.Set<OutboxMessage>()
            .Where(m => m.DeadLetteredAtUtc != null)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var old = candidates.Where(m => m.DeadLetteredAtUtc < cutoff).ToList();
        dbContext.Set<OutboxMessage>().RemoveRange(old);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
