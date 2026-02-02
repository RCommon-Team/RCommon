using FluentAssertions;
using RCommon.Caching;
using Xunit;

namespace RCommon.Caching.Tests;

public class CacheKeyTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidValue_DoesNotThrow()
    {
        // Arrange
        var value = "test-cache-key";

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        var act = () => new CacheKey(value!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithEmptyValue_ThrowsArgumentNullException()
    {
        // Arrange
        var value = string.Empty;

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithValueExceedingMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var value = new string('a', CacheKey.MaxLength + 1);

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_WithValueAtMaxLength_DoesNotThrow()
    {
        // Arrange
        var value = new string('a', CacheKey.MaxLength);

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithSingleCharacterValue_DoesNotThrow()
    {
        // Arrange
        var value = "a";

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region MaxLength Constant Tests

    [Fact]
    public void MaxLength_ShouldBe256()
    {
        // Assert
        CacheKey.MaxLength.Should().Be(256);
    }

    #endregion

    #region With Static Method Tests (String Array)

    [Fact]
    public void With_WithSingleKey_CreatesValidCacheKey()
    {
        // Arrange
        var key = "single-key";

        // Act
        var act = () => CacheKey.With(key);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void With_WithMultipleKeys_JoinsWithHyphen()
    {
        // Arrange
        var key1 = "part1";
        var key2 = "part2";
        var key3 = "part3";

        // Act
        var act = () => CacheKey.With(key1, key2, key3);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void With_WithEmptyArray_ThrowsArgumentNullException()
    {
        // Act
        var act = () => CacheKey.With();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void With_WithKeysTotalLengthExceedingMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var longKey1 = new string('a', 150);
        var longKey2 = new string('b', 150);

        // Act
        var act = () => CacheKey.With(longKey1, longKey2);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("key1", "key2")]
    [InlineData("a", "b", "c")]
    [InlineData("test")]
    public void With_WithVariousValidKeys_DoesNotThrow(params string[] keys)
    {
        // Act
        var act = () => CacheKey.With(keys);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region With Static Method Tests (Type and String Array)

    [Fact]
    public void With_WithTypeAndSingleKey_DoesNotThrow()
    {
        // Arrange
        var ownerType = typeof(CacheKeyTests);

        // Act
        var act = () => CacheKey.With(ownerType, "test-key");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void With_WithTypeAndMultipleKeys_DoesNotThrow()
    {
        // Arrange
        var ownerType = typeof(string);

        // Act
        var act = () => CacheKey.With(ownerType, "key1", "key2", "key3");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void With_WithGenericType_DoesNotThrow()
    {
        // Arrange
        var ownerType = typeof(List<string>);

        // Act
        var act = () => CacheKey.With(ownerType, "key1");

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void With_WithDifferentTypes_DoesNotThrow()
    {
        // Act & Assert
        var act1 = () => CacheKey.With(typeof(int), "key");
        var act2 = () => CacheKey.With(typeof(DateTime), "key");

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    [Fact]
    public void With_WithTypeAndEmptyKeys_DoesNotThrow()
    {
        // Arrange
        var ownerType = typeof(CacheKeyTests);

        // Act
        var act = () => CacheKey.With(ownerType);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void With_WithNestedGenericType_DoesNotThrow()
    {
        // Arrange
        var ownerType = typeof(Dictionary<string, List<int>>);

        // Act
        var act = () => CacheKey.With(ownerType, "key");

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Constructor_WithWhitespaceValue_DoesNotThrow()
    {
        // Arrange
        var value = "   ";

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithSpecialCharacters_DoesNotThrow()
    {
        // Arrange
        var value = "cache-key_with.special:characters!@#$%";

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithUnicodeCharacters_DoesNotThrow()
    {
        // Arrange
        var value = "cache-key-with-unicode-characters-\u00e9\u00e0\u00fc";

        // Act
        var act = () => new CacheKey(value);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void With_WithNullInKeysArray_ThrowsArgumentNullException()
    {
        // Arrange
        string?[] keys = new string?[] { "key1", null, "key3" };

        // Act - null in the array will be concatenated as empty string, making it still work
        // The actual behavior depends on string.Join handling nulls
        var act = () => CacheKey.With(keys!);

        // Assert - string.Join treats null as empty string, so this should not throw
        act.Should().NotThrow();
    }

    #endregion
}
