using System;

namespace RCommon.Persistence.Outbox;

public interface IBackoffStrategy
{
    TimeSpan ComputeDelay(int retryCount);
}
