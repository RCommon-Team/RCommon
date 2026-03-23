using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Outbox;

public interface IOutboxStore
{
    Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, CancellationToken cancellationToken = default);
    Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
    Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset = 0, CancellationToken cancellationToken = default);
    Task ReplayDeadLetterAsync(Guid messageId, CancellationToken cancellationToken = default);
}
