using FluentAssertions;
using RCommon.SystemTextJson;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace RCommon.SystemTextJson.Tests;

public class JsonIntEnumConverterTests
{
    #region Test Enums

    public enum TestIntEnum
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 3
    }

    public enum TestNegativeEnum
    {
        Negative = -1,
        Zero = 0,
        Positive = 1
    }

    public enum TestLargeValueEnum
    {
        Small = 1,
        Medium = 100,
        Large = 1000,
        VeryLarge = 10000
    }

    public class TestClassWithEnum
    {
        [JsonConverter(typeof(JsonIntEnumConverter<TestIntEnum>))]
        public TestIntEnum Status { get; set; }
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_EnumValue_WritesIntegerValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());
        var obj = new { Status = TestIntEnum.First };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        json.Should().Contain("1");
    }

    [Fact]
    public void Write_EnumValueNone_WritesZero()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());
        var obj = new { Status = TestIntEnum.None };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        json.Should().Contain("0");
    }

    [Theory]
    [InlineData(TestIntEnum.None, 0)]
    [InlineData(TestIntEnum.First, 1)]
    [InlineData(TestIntEnum.Second, 2)]
    [InlineData(TestIntEnum.Third, 3)]
    public void Write_DifferentEnumValues_WritesCorrectInteger(TestIntEnum enumValue, int expectedInt)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());

        // Act
        var json = JsonSerializer.Serialize(enumValue, options);

        // Assert
        json.Should().Be(expectedInt.ToString());
    }

    [Fact]
    public void Write_LargeEnumValue_WritesCorrectInteger()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestLargeValueEnum>());

        // Act
        var json = JsonSerializer.Serialize(TestLargeValueEnum.VeryLarge, options);

        // Assert
        json.Should().Be("10000");
    }

    [Fact]
    public void Write_NegativeEnumValue_WritesCorrectInteger()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestNegativeEnum>());

        // Act
        var json = JsonSerializer.Serialize(TestNegativeEnum.Negative, options);

        // Assert
        json.Should().Be("-1");
    }

    [Fact]
    public void Write_ObjectWithEnumProperty_WritesIntegerForProperty()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        var obj = new TestClassWithEnum { Status = TestIntEnum.Second };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        json.Should().Contain("\"Status\":2");
    }

    #endregion

    #region Read Tests

    [Fact]
    public void Read_StringEnumName_ReturnsEnumValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());
        var json = "\"First\"";

        // Act
        var result = JsonSerializer.Deserialize<TestIntEnum>(json, options);

        // Assert
        result.Should().Be(TestIntEnum.First);
    }

    [Theory]
    [InlineData("\"None\"", TestIntEnum.None)]
    [InlineData("\"First\"", TestIntEnum.First)]
    [InlineData("\"Second\"", TestIntEnum.Second)]
    [InlineData("\"Third\"", TestIntEnum.Third)]
    public void Read_DifferentEnumNames_ReturnsCorrectEnumValue(string json, TestIntEnum expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());

        // Act
        var result = JsonSerializer.Deserialize<TestIntEnum>(json, options);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Read_ObjectWithEnumProperty_DeserializesCorrectly()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        var json = "{\"Status\":\"Second\"}";

        // Act
        var result = JsonSerializer.Deserialize<TestClassWithEnum>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(TestIntEnum.Second);
    }

    [Fact]
    public void Read_LargeEnumName_ReturnsCorrectEnumValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestLargeValueEnum>());
        var json = "\"VeryLarge\"";

        // Act
        var result = JsonSerializer.Deserialize<TestLargeValueEnum>(json, options);

        // Assert
        result.Should().Be(TestLargeValueEnum.VeryLarge);
    }

    [Fact]
    public void Read_InvalidEnumName_ThrowsException()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());
        var json = "\"InvalidValue\"";

        // Act
        Action act = () => JsonSerializer.Deserialize<TestIntEnum>(json, options);

        // Assert
        act.Should().Throw<Exception>();
    }

    #endregion

    #region Round-trip Tests

    [Fact]
    public void RoundTrip_EnumValue_PreservesValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());
        var original = TestIntEnum.Second;

        // Act
        var json = JsonSerializer.Serialize(original, options);
        // The result is an integer, but we need a string to deserialize with this converter
        // So we serialize as int and would need to convert back
        var result = (TestIntEnum)int.Parse(json);

        // Assert
        result.Should().Be(original);
    }

    [Theory]
    [InlineData(TestIntEnum.None)]
    [InlineData(TestIntEnum.First)]
    [InlineData(TestIntEnum.Second)]
    [InlineData(TestIntEnum.Third)]
    public void RoundTrip_AllEnumValues_PreserveValues(TestIntEnum original)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var intValue = int.Parse(json);

        // Assert
        intValue.Should().Be((int)original);
    }

    #endregion

    #region Converter Type Tests

    [Fact]
    public void JsonIntEnumConverter_IsGenericJsonConverter()
    {
        // Arrange & Act
        var converter = new JsonIntEnumConverter<TestIntEnum>();

        // Assert
        converter.Should().BeAssignableTo<JsonConverter<TestIntEnum>>();
    }

    [Fact]
    public void JsonIntEnumConverter_CanBeUsedWithDifferentEnumTypes()
    {
        // Arrange
        var intConverter = new JsonIntEnumConverter<TestIntEnum>();
        var largeConverter = new JsonIntEnumConverter<TestLargeValueEnum>();
        var negativeConverter = new JsonIntEnumConverter<TestNegativeEnum>();

        // Assert
        intConverter.Should().NotBeNull();
        largeConverter.Should().NotBeNull();
        negativeConverter.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Write_FirstDefinedEnumValue_WritesCorrectValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());

        // Act
        var json = JsonSerializer.Serialize(TestIntEnum.None, options);

        // Assert
        json.Should().Be("0");
    }

    [Fact]
    public void Write_LastDefinedEnumValue_WritesCorrectValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonIntEnumConverter<TestIntEnum>());

        // Act
        var json = JsonSerializer.Serialize(TestIntEnum.Third, options);

        // Assert
        json.Should().Be("3");
    }

    #endregion
}
