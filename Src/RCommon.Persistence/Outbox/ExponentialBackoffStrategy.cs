using System;

namespace RCommon.Persistence.Outbox;

public class ExponentialBackoffStrategy : IBackoffStrategy
{
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly double _multiplier;

    public ExponentialBackoffStrategy(TimeSpan baseDelay, TimeSpan maxDelay, double multiplier = 2.0)
    {
        if (baseDelay <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(baseDelay), "Base delay must be positive.");
        if (maxDelay < baseDelay)
            throw new ArgumentOutOfRangeException(nameof(maxDelay), "Max delay must be greater than or equal to base delay.");
        if (multiplier <= 1.0)
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Multiplier must be greater than 1.0 for exponential growth.");

        _baseDelay = baseDelay;
        _maxDelay = maxDelay;
        _multiplier = multiplier;
    }

    public TimeSpan ComputeDelay(int retryCount)
        => TimeSpan.FromSeconds(
            Math.Min(
                _baseDelay.TotalSeconds * Math.Pow(_multiplier, retryCount),
                _maxDelay.TotalSeconds));
}
