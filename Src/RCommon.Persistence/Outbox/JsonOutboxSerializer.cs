using System;
using System.Text.Json;
using RCommon;
using RCommon.Models.Events;

namespace RCommon.Persistence.Outbox;

public class JsonOutboxSerializer : IOutboxSerializer
{
    public string Serialize(ISerializableEvent @event)
    {
        Guard.IsNotNull(@event, nameof(@event));
        return JsonSerializer.Serialize(@event, @event.GetType());
    }

    public string GetEventTypeName(ISerializableEvent @event)
    {
        Guard.IsNotNull(@event, nameof(@event));
        var type = @event.GetType();
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }

    public ISerializableEvent Deserialize(string eventType, string payload)
    {
        Guard.IsNotNull(eventType, nameof(eventType));
        Guard.IsNotNull(payload, nameof(payload));

        var type = Type.GetType(eventType)
            ?? throw new InvalidOperationException($"Cannot resolve type '{eventType}'.");

        if (!typeof(ISerializableEvent).IsAssignableFrom(type))
        {
            throw new InvalidOperationException(
                $"Type '{eventType}' does not implement ISerializableEvent.");
        }

        var result = JsonSerializer.Deserialize(payload, type)
            ?? throw new InvalidOperationException(
                $"Deserialization of '{eventType}' returned null.");

        return (ISerializableEvent)result;
    }
}
