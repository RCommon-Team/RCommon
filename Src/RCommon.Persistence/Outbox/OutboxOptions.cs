using System;

namespace RCommon.Persistence.Outbox;

public class OutboxOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 5;
    public TimeSpan CleanupAge { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    public string TableName { get; set; } = "__OutboxMessages";
    public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan BackoffBaseDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan BackoffMaxDelay { get; set; } = TimeSpan.FromMinutes(30);
    public double BackoffMultiplier { get; set; } = 2.0;
    public string InboxTableName { get; set; } = "__InboxMessages";
}

