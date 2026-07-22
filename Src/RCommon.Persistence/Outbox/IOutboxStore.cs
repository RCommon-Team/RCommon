using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Outbox;

public interface IOutboxStore
{
    Task SaveAsync(IOutboxMessage message, string dataStoreName, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid messageId, string error, DateTimeOffset nextRetryAtUtc, string dataStoreName, CancellationToken cancellationToken = default);
    Task MarkDeadLetteredAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default);
    Task DeleteProcessedAsync(TimeSpan olderThan, string dataStoreName, CancellationToken cancellationToken = default);
    Task DeleteDeadLetteredAsync(TimeSpan olderThan, string dataStoreName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IOutboxMessage>> ClaimAsync(string instanceId, int batchSize, TimeSpan lockDuration, string dataStoreName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IOutboxMessage>> GetDeadLettersAsync(int batchSize, int offset, string dataStoreName, CancellationToken cancellationToken = default);
    Task ReplayDeadLetterAsync(Guid messageId, string dataStoreName, CancellationToken cancellationToken = default);
}
