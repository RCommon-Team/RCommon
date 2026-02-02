using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RCommon.Json;
using RCommon.JsonNet;
using Xunit;

namespace RCommon.JsonNet.Tests;

public class JsonNetSerializerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());

        // Act
        var act = () => new JsonNetSerializer(options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_CreatesInstanceOfIJsonSerializer()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());

        // Act
        var serializer = new JsonNetSerializer(options);

        // Assert
        serializer.Should().BeAssignableTo<IJsonSerializer>();
    }

    [Fact]
    public void Constructor_WithMockedOptions_SetsSettings()
    {
        // Arrange
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };
        var mockOptions = new Mock<IOptions<JsonSerializerSettings>>();
        mockOptions.Setup(x => x.Value).Returns(settings);

        // Act
        var serializer = new JsonNetSerializer(mockOptions.Object);

        // Assert
        serializer.Should().NotBeNull();
    }

    #endregion

    #region Serialize Tests (object overload)

    [Fact]
    public void Serialize_WithSimpleObject_ReturnsValidJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };

        // Act
        var result = serializer.Serialize(testObject);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Name");
        result.Should().Contain("John");
        result.Should().Contain("Age");
        result.Should().Contain("30");
    }

    [Fact]
    public void Serialize_WithNullOptions_ReturnsValidJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "Jane", Age = 25 };

        // Act
        var result = serializer.Serialize(testObject, null);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Jane");
    }

    [Fact]
    public void Serialize_WithCamelCaseOption_UsesCamelCasePropertyNames()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };
        var serializeOptions = new JsonSerializeOptions { CamelCase = true };

        // Act
        var result = serializer.Serialize(testObject, serializeOptions);

        // Assert
        result.Should().Contain("name");
        result.Should().Contain("age");
    }

    [Fact]
    public void Serialize_WithIndentedOption_ReturnsFormattedJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };
        var serializeOptions = new JsonSerializeOptions { Indented = true };

        // Act
        var result = serializer.Serialize(testObject, serializeOptions);

        // Assert
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void Serialize_WithCamelCaseAndIndentedOptions_AppliesBothOptions()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };
        var serializeOptions = new JsonSerializeOptions { CamelCase = true, Indented = true };

        // Act
        var result = serializer.Serialize(testObject, serializeOptions);

        // Assert
        result.Should().Contain("name");
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void Serialize_WithNestedObject_ReturnsValidJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestOrder
        {
            OrderId = 1,
            Customer = new TestPerson { Name = "John", Age = 30 }
        };

        // Act
        var result = serializer.Serialize(testObject);

        // Assert
        result.Should().Contain("OrderId");
        result.Should().Contain("Customer");
        result.Should().Contain("John");
    }

    [Fact]
    public void Serialize_WithCollection_ReturnsValidJsonArray()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testList = new List<TestPerson>
        {
            new TestPerson { Name = "John", Age = 30 },
            new TestPerson { Name = "Jane", Age = 25 }
        };

        // Act
        var result = serializer.Serialize(testList);

        // Assert
        result.Should().StartWith("[");
        result.Should().EndWith("]");
        result.Should().Contain("John");
        result.Should().Contain("Jane");
    }

    [Fact]
    public void Serialize_WithEmptyCollection_ReturnsEmptyArray()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testList = new List<TestPerson>();

        // Act
        var result = serializer.Serialize(testList);

        // Assert
        result.Should().Be("[]");
    }

    [Fact]
    public void Serialize_WithNullObject_ReturnsNullString()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        object? testObject = null;

        // Act
        var result = serializer.Serialize(testObject!);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void Serialize_WithDictionary_ReturnsValidJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testDict = new Dictionary<string, int>
        {
            { "one", 1 },
            { "two", 2 }
        };

        // Act
        var result = serializer.Serialize(testDict);

        // Assert
        result.Should().Contain("one");
        result.Should().Contain("two");
        result.Should().Contain("1");
        result.Should().Contain("2");
    }

    #endregion

    #region Serialize Tests (object, Type overload)

    [Fact]
    public void Serialize_WithType_ReturnsValidJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };

        // Act
        var result = serializer.Serialize(testObject, typeof(TestPerson));

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Name");
        result.Should().Contain("John");
    }

    [Fact]
    public void Serialize_WithTypeAndCamelCaseOption_UsesCamelCasePropertyNames()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };
        var serializeOptions = new JsonSerializeOptions { CamelCase = true };

        // Act
        var result = serializer.Serialize(testObject, typeof(TestPerson), serializeOptions);

        // Assert
        result.Should().Contain("name");
        result.Should().Contain("age");
    }

    [Fact]
    public void Serialize_WithTypeAndIndentedOption_ReturnsFormattedJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };
        var serializeOptions = new JsonSerializeOptions { Indented = true };

        // Act
        var result = serializer.Serialize(testObject, typeof(TestPerson), serializeOptions);

        // Assert
        result.Should().Contain(Environment.NewLine);
    }

    [Fact]
    public void Serialize_WithTypeAndNullOptions_ReturnsValidJson()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "Jane", Age = 25 };

        // Act
        var result = serializer.Serialize(testObject, typeof(TestPerson), null);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Jane");
    }

    [Fact]
    public void Serialize_WithBaseTypeAndDerivedObject_SerializesBaseProperties()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        object testObject = new TestPerson { Name = "John", Age = 30 };

        // Act
        var result = serializer.Serialize(testObject, typeof(object));

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region Deserialize<T> Tests

    [Fact]
    public void Deserialize_WithValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"Name\":\"John\",\"Age\":30}";

        // Act
        var result = serializer.Deserialize<TestPerson>(json);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("John");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_WithNullOptions_ReturnsDeserializedObject()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"Name\":\"Jane\",\"Age\":25}";

        // Act
        var result = serializer.Deserialize<TestPerson>(json, null);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Jane");
        result.Age.Should().Be(25);
    }

    [Fact]
    public void Deserialize_WithCamelCaseJson_ReturnsDeserializedObject()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"name\":\"John\",\"age\":30}";
        var deserializeOptions = new JsonDeserializeOptions { CamelCase = true };

        // Act
        var result = serializer.Deserialize<TestPerson>(json, deserializeOptions);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("John");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_WithNestedObject_ReturnsDeserializedObject()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"OrderId\":1,\"Customer\":{\"Name\":\"John\",\"Age\":30}}";

        // Act
        var result = serializer.Deserialize<TestOrder>(json);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(1);
        result.Customer.Should().NotBeNull();
        result.Customer!.Name.Should().Be("John");
        result.Customer.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_WithCollection_ReturnsDeserializedList()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "[{\"Name\":\"John\",\"Age\":30},{\"Name\":\"Jane\",\"Age\":25}]";

        // Act
        var result = serializer.Deserialize<List<TestPerson>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("John");
        result[1].Name.Should().Be("Jane");
    }

    [Fact]
    public void Deserialize_WithEmptyArray_ReturnsEmptyList()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "[]";

        // Act
        var result = serializer.Deserialize<List<TestPerson>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_WithNullJson_ReturnsNull()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "null";

        // Act
        var result = serializer.Deserialize<TestPerson>(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithDictionary_ReturnsDeserializedDictionary()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"one\":1,\"two\":2}";

        // Act
        var result = serializer.Deserialize<Dictionary<string, int>>(json);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result["one"].Should().Be(1);
        result["two"].Should().Be(2);
    }

    [Fact]
    public void Deserialize_WithPrimitiveType_ReturnsValue()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "42";

        // Act
        var result = serializer.Deserialize<int>(json);

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void Deserialize_WithStringType_ReturnsString()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "\"hello world\"";

        // Act
        var result = serializer.Deserialize<string>(json);

        // Assert
        result.Should().Be("hello world");
    }

    [Fact]
    public void Deserialize_WithBooleanType_ReturnsBool()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "true";

        // Act
        var result = serializer.Deserialize<bool>(json);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Deserialize (Type) Tests

    [Fact]
    public void Deserialize_WithType_ReturnsDeserializedObject()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"Name\":\"John\",\"Age\":30}";

        // Act
        var result = serializer.Deserialize(json, typeof(TestPerson));

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TestPerson>();
        var person = (TestPerson)result;
        person.Name.Should().Be("John");
        person.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_WithTypeAndNullOptions_ReturnsDeserializedObject()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"Name\":\"Jane\",\"Age\":25}";

        // Act
        var result = serializer.Deserialize(json, typeof(TestPerson), null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TestPerson>();
    }

    [Fact]
    public void Deserialize_WithTypeAndCamelCaseOption_ReturnsDeserializedObject()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "{\"name\":\"John\",\"age\":30}";
        var deserializeOptions = new JsonDeserializeOptions { CamelCase = true };

        // Act
        var result = serializer.Deserialize(json, typeof(TestPerson), deserializeOptions);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<TestPerson>();
        var person = (TestPerson)result;
        person.Name.Should().Be("John");
        person.Age.Should().Be(30);
    }

    [Fact]
    public void Deserialize_WithListType_ReturnsDeserializedList()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var json = "[{\"Name\":\"John\",\"Age\":30}]";

        // Act
        var result = serializer.Deserialize(json, typeof(List<TestPerson>));

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<List<TestPerson>>();
        var list = (List<TestPerson>)result;
        list.Should().HaveCount(1);
    }

    #endregion

    #region Round Trip Tests

    [Fact]
    public void SerializeAndDeserialize_WithSimpleObject_PreservesData()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var original = new TestPerson { Name = "John", Age = 30 };

        // Act
        var json = serializer.Serialize(original);
        var result = serializer.Deserialize<TestPerson>(json);

        // Assert
        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void SerializeAndDeserialize_WithNestedObject_PreservesData()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var original = new TestOrder
        {
            OrderId = 1,
            Customer = new TestPerson { Name = "John", Age = 30 }
        };

        // Act
        var json = serializer.Serialize(original);
        var result = serializer.Deserialize<TestOrder>(json);

        // Assert
        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void SerializeAndDeserialize_WithCollection_PreservesData()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var original = new List<TestPerson>
        {
            new TestPerson { Name = "John", Age = 30 },
            new TestPerson { Name = "Jane", Age = 25 }
        };

        // Act
        var json = serializer.Serialize(original);
        var result = serializer.Deserialize<List<TestPerson>>(json);

        // Assert
        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void SerializeAndDeserialize_WithComplexTypes_PreservesData()
    {
        // Arrange
        var options = Options.Create(new JsonSerializerSettings());
        var serializer = new JsonNetSerializer(options);
        var original = new TestComplexData
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Tags = new List<string> { "tag1", "tag2" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            }
        };

        // Act
        var json = serializer.Serialize(original);
        var result = serializer.Deserialize<TestComplexData>(json);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(original.Id);
        result.Tags.Should().BeEquivalentTo(original.Tags);
    }

    #endregion

    #region Custom Settings Tests

    [Fact]
    public void Serialize_WithCustomDateFormatSettings_UsesCustomFormat()
    {
        // Arrange
        var settings = new JsonSerializerSettings
        {
            DateFormatString = "yyyy-MM-dd"
        };
        var options = Options.Create(settings);
        var serializer = new JsonNetSerializer(options);
        var testObject = new { Date = new DateTime(2024, 1, 15) };

        // Act
        var result = serializer.Serialize(testObject);

        // Assert
        result.Should().Contain("2024-01-15");
    }

    [Fact]
    public void Serialize_WithNullValueHandling_OmitsNullValues()
    {
        // Arrange
        var settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };
        var options = Options.Create(settings);
        var serializer = new JsonNetSerializer(options);
        var testObject = new TestPerson { Name = "John", Age = 30 };
        testObject.Name = null!;

        // Act
        var result = serializer.Serialize(testObject);

        // Assert
        result.Should().NotContain("Name");
    }

    #endregion

    #region Test Helper Classes

    private class TestPerson
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    private class TestOrder
    {
        public int OrderId { get; set; }
        public TestPerson? Customer { get; set; }
    }

    private class TestComplexData
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    #endregion
}
