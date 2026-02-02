using RCommon.Entities;
using RCommon.Models.Events;

namespace RCommon.Persistence.Caching.MemoryCache.Tests.TestHelpers;

/// <summary>
/// A test entity implementation for unit testing purposes.
/// </summary>
public class TestEntity : IBusinessEntity<Guid>
{
    private readonly List<ISerializableEvent> _localEvents = new();

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public bool AllowEventTracking { get; set; } = false;

    public IReadOnlyCollection<ISerializableEvent> LocalEvents => _localEvents.AsReadOnly();

    public void AddLocalEvent(ISerializableEvent eventItem)
    {
        _localEvents.Add(eventItem);
    }

    public void ClearLocalEvents()
    {
        _localEvents.Clear();
    }

    public bool EntityEquals(IBusinessEntity other)
    {
        if (other is TestEntity entity)
        {
            return Id == entity.Id;
        }
        return false;
    }

    public object[] GetKeys()
    {
        return new object[] { Id };
    }

    public void RemoveLocalEvent(ISerializableEvent eventItem)
    {
        _localEvents.Remove(eventItem);
    }
}
