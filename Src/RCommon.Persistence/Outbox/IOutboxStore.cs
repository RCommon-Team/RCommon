using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Outbox;

public interface IOutboxStore
{
    Task SaveAsync(IOutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
    Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task DeleteProcessedAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
    Task DeleteDeadLetteredAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
