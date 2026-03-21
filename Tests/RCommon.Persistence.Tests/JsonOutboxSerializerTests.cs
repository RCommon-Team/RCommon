using FluentAssertions;
using RCommon.Models.Events;
using RCommon.Persistence.Outbox;
using System.Text.Json;
using Xunit;

namespace RCommon.Persistence.Tests;

public record SerializerTestEvent(string Name, int Value) : ISerializableEvent;

public class JsonOutboxSerializerTests
{
    private readonly JsonOutboxSerializer _serializer = new();

    [Fact]
    public void Serialize_ReturnsValidJson()
    {
        var @event = new SerializerTestEvent("OrderCreated", 42);
        var json = _serializer.Serialize(@event);
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("Name").GetString().Should().Be("OrderCreated");
        doc.RootElement.GetProperty("Value").GetInt32().Should().Be(42);
    }

    [Fact]
    public void GetEventTypeName_ReturnsShortAssemblyQualifiedName()
    {
        var @event = new SerializerTestEvent("Test", 1);
        var typeName = _serializer.GetEventTypeName(@event);
        typeName.Should().Contain("SerializerTestEvent");
        typeName.Should().Contain(",");
    }

    [Fact]
    public void Deserialize_RoundTrips()
    {
        var original = new SerializerTestEvent("OrderCreated", 42);
        var json = _serializer.Serialize(original);
        var typeName = _serializer.GetEventTypeName(original);
        var deserialized = _serializer.Deserialize(typeName, json);
        deserialized.Should().BeOfType<SerializerTestEvent>();
        var typed = (SerializerTestEvent)deserialized;
        typed.Name.Should().Be("OrderCreated");
        typed.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_ThrowsForUnknownType()
    {
        var act = () => _serializer.Deserialize("NonExistent.Type, FakeAssembly", "{}");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deserialize_ThrowsForNonSerializableEventType()
    {
        var typeName = typeof(string).AssemblyQualifiedName!;
        var act = () => _serializer.Deserialize(typeName, "\"hello\"");
        act.Should().Throw<InvalidOperationException>();
    }
}
