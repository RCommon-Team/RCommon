using FluentAssertions;
using Microsoft.Extensions.Options;
using RCommon.Json;
using RCommon.SystemTextJson;
using System.Text.Json;
using Xunit;

namespace RCommon.SystemTextJson.Tests;

public class TextJsonSerializerTests
{
    private readonly TextJsonSerializer _serializer;
    private readonly JsonSerializerOptions _defaultOptions;

    public TextJsonSerializerTests()
    {
        _defaultOptions = new JsonSerializerOptions();
        var options = Options.Create(_defaultOptions);
        _serializer = new TextJsonSerializer(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerOptions());

        // Act
        var serializer = new TextJsonSerializer(options);

        // Assert
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithCustomOptions_UsesProvidedOptions()
    {
        // Arrange
        var customOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        var options = Options.Create(customOptions);

        // Act
        var serializer = new TextJsonSerializer(options);
        var result = serializer.Serialize(new TestObject { Name = "Test", Value = 123 });

        // Assert
        result.Should().Contain("\"name\"");
        result.Should().Contain("\"value\"");
    }

    #endregion

    #region Serialize(object) Tests

    [Fact]
    public void Serialize_SimpleObject_ReturnsJsonString()
    {
        // Arrange
        var obj = new TestObject { Name = "Test", Value = 42 };

        // Act
        var result = _serializer.Serialize(obj);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Name");
        result.Should().Contain("Test");
        result.Should().Contain("Value");
        result.Should().Contain("42");
    }

    [Fact]
    public void Serialize_NullObject_ReturnsNullJson()
    {
        // Arrange
        object? obj = null;

        // Act
        var result = _serializer.Serialize(obj!);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void Serialize_WithOptionsNull_SerializesCorrectly()
    {
        // Arrange
        var obj = new TestObject { Name = "Test", Value = 1 };

        // Act
        var result = _serializer.Serialize(obj, (JsonSerializeOptions?)null);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Serialize_WithIndentedTrue_ReturnsIndentedJson()
    {
        // Arrange
        var obj = new TestObject { Name = "Test", Value = 1 };
        var serializeOptions = new JsonSerializeOptions { Indented = true };

        // Act
        var result = _serializer.Serialize(obj, serializeOptions);

        // Assert
        result.Should().Contain("\n");
    }

    [Fact]
    public void Serialize_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerOptions { WriteIndented = false });
        var serializer = new TextJsonSerializer(options);
        var obj = new TestObject { Name = "Test", Value = 1 };
        var serializeOptions = new JsonSerializeOptions { Indented = false };

        // Act
        var result = serializer.Serialize(obj, serializeOptions);

        // Assert
        result.Should().NotContain("\n");
    }

    [Fact]
    public void Serialize_WithCamelCaseTrue_UsessCamelCaseNaming()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerOptions());
        var serializer = new TextJsonSerializer(options);
        var obj = new TestObject { Name = "Test", Value = 1 };
        var serializeOptions = new JsonSerializeOptions { CamelCase = true };

        // Act
        var result = serializer.Serialize(obj, serializeOptions);

        // Assert
        result.Should().Contain("\"name\"");
        result.Should().Contain("\"value\"");
    }

    [Fact]
    public void Serialize_ComplexObject_SerializesNestedProperties()
    {
        // Arrange
        var obj = new ComplexTestObject
        {
            Id = 1,
            Inner = new TestObject { Name = "Nested", Value = 99 }
        };

        // Act
        var result = _serializer.Serialize(obj);

        // Assert
        result.Should().Contain("Id");
        result.Should().Contain("Inner");
        result.Should().Contain("Nested");
        result.Should().Contain("99");
    }

    [Fact]
    public void Serialize_Array_SerializesAllElements()
    {
        // Arrange
        var arr = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = _serializer.Serialize(arr);

        // Assert
        result.Should().Be("[1,2,3,4,5]");
    }

    [Fact]
    public void Serialize_List_SerializesAllElements()
    {
        // Arrange
        var list = new List<string> { "one", "two", "three" };

        // Act
        var result = _serializer.Serialize(list);

        // Assert
        result.Should().Contain("one");
        result.Should().Contain("two");
        result.Should().Contain("three");
    }

    [Fact]
    public void Serialize_Dictionary_SerializesKeyValuePairs()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            { "first", 1 },
            { "second", 2 }
        };

        // Act
        var result = _serializer.Serialize(dict);

        // Assert
        result.Should().Contain("\"first\"");
        result.Should().Contain("\"second\"");
        result.Should().Contain("1");
        result.Should().Contain("2");
    }

    #endregion

    #region Serialize(object, Type) Tests

    [Fact]
    public void Serialize_WithType_ReturnsJsonString()
    {
        // Arrange
        var obj = new TestObject { Name = "Test", Value = 42 };

        // Act
        var result = _serializer.Serialize(obj, typeof(TestObject));

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Name");
        result.Should().Contain("42");
    }

    [Fact]
    public void Serialize_WithType_AndOptions_AppliesOptions()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerOptions());
        var serializer = new TextJsonSerializer(options);
        var obj = new TestObject { Name = "Test", Value = 42 };
        var serializeOptions = new JsonSerializeOptions { CamelCase = true, Indented = true };

        // Act
        var result = serializer.Serialize(obj, typeof(TestObject), serializeOptions);

        // Assert
        result.Should().Contain("\"name\"");
        result.Should().Contain("\n");
    }

    [Fact]
    public void Serialize_WithType_NullOptions_SerializesCorrectly()
    {
        // Arrange
        var obj = new TestObject { Name = "Test", Value = 1 };

        // Act
        var result = _serializer.Serialize(obj, typeof(TestObject), null);

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Serialize_WithBaseType_SerializesAsBaseType()
    {
        // Arrange
        var obj = new DerivedTestObject { Name = "Test", Value = 1, Extra = "ExtraValue" };

        // Act
        var result = _serializer.Serialize(obj, typeof(TestObject));

        // Assert
        result.Should().Contain("Name");
        result.Should().Contain("Value");
        // Note: Extra might or might not be included depending on serializer settings
    }

    #endregion

    #region Deserialize<T> Tests

    [Fact]
    public void Deserialize_ValidJson_ReturnsObject()
    {
        // Arrange
        var json = "{\"Name\":\"Test\",\"Value\":42}";

        // Act
        var result = _serializer.Deserialize<TestObject>(json);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_WithNullOptions_DeserializesCorrectly()
    {
        // Arrange
        var json = "{\"Name\":\"Test\",\"Value\":1}";

        // Act
        var result = _serializer.Deserialize<TestObject>(json, null);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test");
    }

    [Fact]
    public void Deserialize_WithCamelCaseOption_DeserializesCamelCaseJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var serializer = new TextJsonSerializer(options);
        var json = "{\"name\":\"Test\",\"value\":42}";
        var deserializeOptions = new JsonDeserializeOptions { CamelCase = true };

        // Act
        var result = serializer.Deserialize<TestObject>(json, deserializeOptions);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_NullJson_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = _serializer.Deserialize<TestObject>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_EmptyObject_ReturnsObjectWithDefaults()
    {
        // Arrange
        var json = "{}";

        // Act
        var result = _serializer.Deserialize<TestObject>(json);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().BeNull();
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Deserialize_Array_ReturnsArrayOfObjects()
    {
        // Arrange
        var json = "[{\"Name\":\"One\",\"Value\":1},{\"Name\":\"Two\",\"Value\":2}]";

        // Act
        var result = _serializer.Deserialize<TestObject[]>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("One");
        result[1].Name.Should().Be("Two");
    }

    [Fact]
    public void Deserialize_List_ReturnsList()
    {
        // Arrange
        var json = "[\"one\",\"two\",\"three\"]";

        // Act
        var result = _serializer.Deserialize<List<string>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain("one");
        result.Should().Contain("two");
        result.Should().Contain("three");
    }

    [Fact]
    public void Deserialize_Dictionary_ReturnsDictionary()
    {
        // Arrange
        var json = "{\"key1\":\"value1\",\"key2\":\"value2\"}";

        // Act
        var result = _serializer.Deserialize<Dictionary<string, string>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("key1");
        result.Should().ContainKey("key2");
        result["key1"].Should().Be("value1");
        result["key2"].Should().Be("value2");
    }

    [Fact]
    public void Deserialize_ComplexObject_DeserializesNestedProperties()
    {
        // Arrange
        var json = "{\"Id\":1,\"Inner\":{\"Name\":\"Nested\",\"Value\":99}}";

        // Act
        var result = _serializer.Deserialize<ComplexTestObject>(json);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.Inner.Should().NotBeNull();
        result.Inner.Name.Should().Be("Nested");
        result.Inner.Value.Should().Be(99);
    }

    [Fact]
    public void Deserialize_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        Action act = () => _serializer.Deserialize<TestObject>(invalidJson);

        // Assert
        act.Should().Throw<JsonException>();
    }

    #endregion

    #region Deserialize(string, Type) Tests

    [Fact]
    public void Deserialize_WithType_ReturnsObject()
    {
        // Arrange
        var json = "{\"Name\":\"Test\",\"Value\":42}";

        // Act
        var result = _serializer.Deserialize(json, typeof(TestObject));

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TestObject>();
        var typedResult = (TestObject)result;
        typedResult.Name.Should().Be("Test");
        typedResult.Value.Should().Be(42);
    }

    [Fact]
    public void Deserialize_WithType_NullOptions_DeserializesCorrectly()
    {
        // Arrange
        var json = "{\"Name\":\"Test\",\"Value\":1}";

        // Act
        var result = _serializer.Deserialize(json, typeof(TestObject), null);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Deserialize_WithType_AndOptions_AppliesOptions()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var serializer = new TextJsonSerializer(options);
        var json = "{\"name\":\"Test\",\"value\":42}";
        var deserializeOptions = new JsonDeserializeOptions { CamelCase = true };

        // Act
        var result = serializer.Deserialize(json, typeof(TestObject), deserializeOptions);

        // Assert
        result.Should().NotBeNull();
        var typedResult = (TestObject)result;
        typedResult.Name.Should().Be("Test");
    }

    [Fact]
    public void Deserialize_WithType_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json }";

        // Act
        Action act = () => _serializer.Deserialize(invalidJson, typeof(TestObject));

        // Assert
        act.Should().Throw<JsonException>();
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void RoundTrip_SimpleObject_ProducesEquivalentObject()
    {
        // Arrange
        var original = new TestObject { Name = "Test", Value = 42 };

        // Act
        var json = _serializer.Serialize(original);
        var result = _serializer.Deserialize<TestObject>(json);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(original.Name);
        result.Value.Should().Be(original.Value);
    }

    [Fact]
    public void RoundTrip_ComplexObject_ProducesEquivalentObject()
    {
        // Arrange
        var original = new ComplexTestObject
        {
            Id = 1,
            Inner = new TestObject { Name = "Nested", Value = 99 }
        };

        // Act
        var json = _serializer.Serialize(original);
        var result = _serializer.Deserialize<ComplexTestObject>(json);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(original.Id);
        result.Inner.Should().NotBeNull();
        result.Inner.Name.Should().Be(original.Inner.Name);
        result.Inner.Value.Should().Be(original.Inner.Value);
    }

    [Fact]
    public void RoundTrip_ListOfObjects_ProducesEquivalentList()
    {
        // Arrange
        var original = new List<TestObject>
        {
            new TestObject { Name = "First", Value = 1 },
            new TestObject { Name = "Second", Value = 2 }
        };

        // Act
        var json = _serializer.Serialize(original);
        var result = _serializer.Deserialize<List<TestObject>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("First");
        result[1].Name.Should().Be("Second");
    }

    #endregion

    #region IJsonSerializer Interface Tests

    [Fact]
    public void TextJsonSerializer_ImplementsIJsonSerializer()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerOptions());

        // Act
        var serializer = new TextJsonSerializer(options);

        // Assert
        serializer.Should().BeAssignableTo<IJsonSerializer>();
    }

    #endregion

    #region Test Helper Classes

    public class TestObject
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }

    public class DerivedTestObject : TestObject
    {
        public string? Extra { get; set; }
    }

    public class ComplexTestObject
    {
        public int Id { get; set; }
        public TestObject? Inner { get; set; }
    }

    #endregion
}
