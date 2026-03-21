using RCommon.Models.Events;

namespace RCommon.Persistence.Outbox;

public interface IOutboxSerializer
{
    string Serialize(ISerializableEvent @event);
    string GetEventTypeName(ISerializableEvent @event);
    ISerializableEvent Deserialize(string eventType, string payload);
}

