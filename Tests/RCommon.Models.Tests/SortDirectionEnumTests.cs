using FluentAssertions;
using RCommon.Models;
using Xunit;

namespace RCommon.Models.Tests;

/// <summary>
/// Tests for the SortDirectionEnum enumeration.
/// </summary>
public class SortDirectionEnumTests
{
    [Fact]
    public void Ascending_ShouldHaveValue1()
    {
        // Arrange & Act
        var value = (byte)SortDirectionEnum.Ascending;

        // Assert
        value.Should().Be(1);
    }

    [Fact]
    public void Descending_ShouldHaveValue2()
    {
        // Arrange & Act
        var value = (byte)SortDirectionEnum.Descending;

        // Assert
        value.Should().Be(2);
    }

    [Fact]
    public void None_ShouldHaveValue3()
    {
        // Arrange & Act
        var value = (byte)SortDirectionEnum.None;

        // Assert
        value.Should().Be(3);
    }

    [Fact]
    public void ShouldHaveThreeMembers()
    {
        // Arrange & Act
        var values = Enum.GetValues<SortDirectionEnum>();

        // Assert
        values.Should().HaveCount(3);
    }

    [Theory]
    [InlineData(SortDirectionEnum.Ascending, "Ascending")]
    [InlineData(SortDirectionEnum.Descending, "Descending")]
    [InlineData(SortDirectionEnum.None, "None")]
    public void ToString_ShouldReturnCorrectName(SortDirectionEnum enumValue, string expectedName)
    {
        // Arrange & Act
        var result = enumValue.ToString();

        // Assert
        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("Ascending", SortDirectionEnum.Ascending)]
    [InlineData("Descending", SortDirectionEnum.Descending)]
    [InlineData("None", SortDirectionEnum.None)]
    public void Parse_ShouldReturnCorrectValue(string name, SortDirectionEnum expectedValue)
    {
        // Arrange & Act
        var result = Enum.Parse<SortDirectionEnum>(name);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData((byte)1, true)]
    [InlineData((byte)2, true)]
    [InlineData((byte)3, true)]
    [InlineData((byte)0, false)]
    [InlineData((byte)4, false)]
    public void IsDefined_ShouldReturnCorrectResult(byte value, bool expectedResult)
    {
        // Arrange & Act
        var result = Enum.IsDefined(typeof(SortDirectionEnum), value);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void ShouldBeByteType()
    {
        // Arrange & Act
        var underlyingType = Enum.GetUnderlyingType(typeof(SortDirectionEnum));

        // Assert
        underlyingType.Should().Be(typeof(byte));
    }
}
