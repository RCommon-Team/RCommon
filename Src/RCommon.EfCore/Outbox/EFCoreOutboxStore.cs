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
                TenantId = message.TenantId
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        // Filter server-side (uses composite index), then order and limit client-side.
        // OrderBy(DateTimeOffset) is not supported by all EF Core providers (e.g. SQLite),
        // and the result set is bounded by the unprocessed message count which is typically small.
        var results = await DbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedAtUtc == null && m.DeadLetteredAtUtc == null && m.RetryCount < _maxRetries)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return results
            .OrderBy(m => m.CreatedAtUtc)
            .Take(batchSize)
            .ToList();
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
    public async Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        var dbContext = DbContext;
        var message = await dbContext.Set<OutboxMessage>()
            .FindAsync(new object[] { messageId }, cancellationToken).ConfigureAwait(false);
        if (message != null)
        {
            message.ErrorMessage = error;
            message.RetryCount++;
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
