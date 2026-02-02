using FluentAssertions;
using RCommon.SystemTextJson;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace RCommon.SystemTextJson.Tests;

public class JsonByteEnumConverterTests
{
    #region Test Enums

    public enum TestByteEnum : byte
    {
        None = 0,
        First = 1,
        Second = 2,
        Third = 3
    }

    public enum TestByteEnumLargeValues : byte
    {
        Min = 0,
        Low = 50,
        Medium = 127,
        High = 200,
        Max = 255
    }

    public enum TestRegularEnum
    {
        None = 0,
        First = 1,
        Second = 2
    }

    public class TestClassWithByteEnum
    {
        [JsonConverter(typeof(JsonByteEnumConverter<TestByteEnum>))]
        public TestByteEnum Status { get; set; }
    }

    #endregion

    #region Write Tests

    [Fact]
    public void Write_EnumValue_WritesByteValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());
        var obj = new { Status = TestByteEnum.First };

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
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());
        var obj = new { Status = TestByteEnum.None };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        json.Should().Contain("0");
    }

    [Theory]
    [InlineData(TestByteEnum.None, 0)]
    [InlineData(TestByteEnum.First, 1)]
    [InlineData(TestByteEnum.Second, 2)]
    [InlineData(TestByteEnum.Third, 3)]
    public void Write_DifferentEnumValues_WritesCorrectByte(TestByteEnum enumValue, byte expectedByte)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());

        // Act
        var json = JsonSerializer.Serialize(enumValue, options);

        // Assert
        json.Should().Be(expectedByte.ToString());
    }

    [Theory]
    [InlineData(TestByteEnumLargeValues.Min, 0)]
    [InlineData(TestByteEnumLargeValues.Low, 50)]
    [InlineData(TestByteEnumLargeValues.Medium, 127)]
    [InlineData(TestByteEnumLargeValues.High, 200)]
    [InlineData(TestByteEnumLargeValues.Max, 255)]
    public void Write_LargeByteValues_WritesCorrectValue(TestByteEnumLargeValues enumValue, byte expectedByte)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnumLargeValues>());

        // Act
        var json = JsonSerializer.Serialize(enumValue, options);

        // Assert
        json.Should().Be(expectedByte.ToString());
    }

    [Fact]
    public void Write_MaxByteValue_WritesCorrectly()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnumLargeValues>());

        // Act
        var json = JsonSerializer.Serialize(TestByteEnumLargeValues.Max, options);

        // Assert
        json.Should().Be("255");
    }

    [Fact]
    public void Write_MinByteValue_WritesZero()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnumLargeValues>());

        // Act
        var json = JsonSerializer.Serialize(TestByteEnumLargeValues.Min, options);

        // Assert
        json.Should().Be("0");
    }

    [Fact]
    public void Write_ObjectWithEnumProperty_WritesByteForProperty()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        var obj = new TestClassWithByteEnum { Status = TestByteEnum.Second };

        // Act
        var json = JsonSerializer.Serialize(obj, options);

        // Assert
        json.Should().Contain("\"Status\":2");
    }

    [Fact]
    public void Write_RegularEnumWithByteConverter_WritesByteValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestRegularEnum>());

        // Act
        var json = JsonSerializer.Serialize(TestRegularEnum.Second, options);

        // Assert
        json.Should().Be("2");
    }

    #endregion

    #region Read Tests

    [Fact]
    public void Read_StringEnumName_ReturnsEnumValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());
        var json = "\"First\"";

        // Act
        var result = JsonSerializer.Deserialize<TestByteEnum>(json, options);

        // Assert
        result.Should().Be(TestByteEnum.First);
    }

    [Theory]
    [InlineData("\"None\"", TestByteEnum.None)]
    [InlineData("\"First\"", TestByteEnum.First)]
    [InlineData("\"Second\"", TestByteEnum.Second)]
    [InlineData("\"Third\"", TestByteEnum.Third)]
    public void Read_DifferentEnumNames_ReturnsCorrectEnumValue(string json, TestByteEnum expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());

        // Act
        var result = JsonSerializer.Deserialize<TestByteEnum>(json, options);

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
        var result = JsonSerializer.Deserialize<TestClassWithByteEnum>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(TestByteEnum.Second);
    }

    [Theory]
    [InlineData("\"Min\"", TestByteEnumLargeValues.Min)]
    [InlineData("\"Low\"", TestByteEnumLargeValues.Low)]
    [InlineData("\"Medium\"", TestByteEnumLargeValues.Medium)]
    [InlineData("\"High\"", TestByteEnumLargeValues.High)]
    [InlineData("\"Max\"", TestByteEnumLargeValues.Max)]
    public void Read_LargeByteEnumNames_ReturnsCorrectEnumValue(string json, TestByteEnumLargeValues expected)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnumLargeValues>());

        // Act
        var result = JsonSerializer.Deserialize<TestByteEnumLargeValues>(json, options);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Read_InvalidEnumName_ThrowsException()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());
        var json = "\"InvalidValue\"";

        // Act
        Action act = () => JsonSerializer.Deserialize<TestByteEnum>(json, options);

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
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());
        var original = TestByteEnum.Second;

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var byteValue = byte.Parse(json);

        // Assert
        byteValue.Should().Be((byte)original);
    }

    [Theory]
    [InlineData(TestByteEnum.None)]
    [InlineData(TestByteEnum.First)]
    [InlineData(TestByteEnum.Second)]
    [InlineData(TestByteEnum.Third)]
    public void RoundTrip_AllEnumValues_PreserveValues(TestByteEnum original)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var byteValue = byte.Parse(json);

        // Assert
        byteValue.Should().Be((byte)original);
    }

    [Theory]
    [InlineData(TestByteEnumLargeValues.Min)]
    [InlineData(TestByteEnumLargeValues.Low)]
    [InlineData(TestByteEnumLargeValues.Medium)]
    [InlineData(TestByteEnumLargeValues.High)]
    [InlineData(TestByteEnumLargeValues.Max)]
    public void RoundTrip_LargeByteValues_PreserveValues(TestByteEnumLargeValues original)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnumLargeValues>());

        // Act
        var json = JsonSerializer.Serialize(original, options);
        var byteValue = byte.Parse(json);

        // Assert
        byteValue.Should().Be((byte)original);
    }

    #endregion

    #region Converter Type Tests

    [Fact]
    public void JsonByteEnumConverter_IsGenericJsonConverter()
    {
        // Arrange & Act
        var converter = new JsonByteEnumConverter<TestByteEnum>();

        // Assert
        converter.Should().BeAssignableTo<JsonConverter<TestByteEnum>>();
    }

    [Fact]
    public void JsonByteEnumConverter_CanBeUsedWithDifferentEnumTypes()
    {
        // Arrange
        var byteEnumConverter = new JsonByteEnumConverter<TestByteEnum>();
        var largeValueConverter = new JsonByteEnumConverter<TestByteEnumLargeValues>();
        var regularEnumConverter = new JsonByteEnumConverter<TestRegularEnum>();

        // Assert
        byteEnumConverter.Should().NotBeNull();
        largeValueConverter.Should().NotBeNull();
        regularEnumConverter.Should().NotBeNull();
    }

    #endregion

    #region Difference from Int Converter Tests

    [Fact]
    public void Write_ByteVsIntConverter_BothProduceSameOutputForSmallValues()
    {
        // Arrange
        var byteOptions = new JsonSerializerOptions();
        byteOptions.Converters.Add(new JsonByteEnumConverter<TestRegularEnum>());

        var intOptions = new JsonSerializerOptions();
        intOptions.Converters.Add(new JsonIntEnumConverter<TestRegularEnum>());

        // Act
        var byteJson = JsonSerializer.Serialize(TestRegularEnum.Second, byteOptions);
        var intJson = JsonSerializer.Serialize(TestRegularEnum.Second, intOptions);

        // Assert
        byteJson.Should().Be(intJson);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Write_FirstDefinedEnumValue_WritesCorrectValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());

        // Act
        var json = JsonSerializer.Serialize(TestByteEnum.None, options);

        // Assert
        json.Should().Be("0");
    }

    [Fact]
    public void Write_LastDefinedEnumValue_WritesCorrectValue()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnum>());

        // Act
        var json = JsonSerializer.Serialize(TestByteEnum.Third, options);

        // Assert
        json.Should().Be("3");
    }

    [Fact]
    public void Write_ByteEnumMaxValue255_WritesCorrectly()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnumLargeValues>());

        // Act
        var json = JsonSerializer.Serialize(TestByteEnumLargeValues.Max, options);

        // Assert
        json.Should().Be("255");
        byte.Parse(json).Should().Be(byte.MaxValue);
    }

    [Fact]
    public void Write_ByteEnumMinValue0_WritesCorrectly()
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonByteEnumConverter<TestByteEnumLargeValues>());

        // Act
        var json = JsonSerializer.Serialize(TestByteEnumLargeValues.Min, options);

        // Assert
        json.Should().Be("0");
        byte.Parse(json).Should().Be(byte.MinValue);
    }

    #endregion
}
