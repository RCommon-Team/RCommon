using RCommon.Models.Events;

namespace RCommon.Wolverine.Tests;

/// <summary>
/// Test event for Wolverine producer/handler tests.
/// </summary>
public class TestEvent : ISerializableEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
