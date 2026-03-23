using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Inbox;

public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid messageId, string? consumerType = null, CancellationToken cancellationToken = default);
    Task RecordAsync(IInboxMessage message, CancellationToken cancellationToken = default);
    Task CleanupAsync(TimeSpan olderThan, CancellationToken cancellationToken = default);
}
