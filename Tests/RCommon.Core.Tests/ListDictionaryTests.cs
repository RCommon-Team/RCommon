using FluentAssertions;
using RCommon.Collections;
using Xunit;

namespace RCommon.Core.Tests;

public class ListDictionaryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesEmptyDictionary()
    {
        // Arrange & Act
        var dict = new ListDictionary<string, int>();

        // Assert
        dict.Count.Should().Be(0);
    }

    #endregion

    #region Add Key Tests

    [Fact]
    public void Add_WithKeyOnly_CreatesEmptyList()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        dict.Add("key1");

        // Assert
        dict.Count.Should().Be(1);
        dict["key1"].Should().BeEmpty();
    }

    [Fact]
    public void Add_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        var act = () => dict.Add(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Add Key-Value Tests

    [Fact]
    public void Add_WithKeyAndValue_AddsValueToList()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        dict.Add("key1", 42);

        // Assert
        dict["key1"].Should().Contain(42);
    }

    [Fact]
    public void Add_MultipleValuesToSameKey_AddsAllValues()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        dict.Add("key1", 1);
        dict.Add("key1", 2);
        dict.Add("key1", 3);

        // Assert
        dict["key1"].Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Add_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var dict = new ListDictionary<string, string>();

        // Act
        var act = () => dict.Add("key1", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Indexer_Get_WithNonExistentKey_CreatesAndReturnsEmptyList()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        var list = dict["newKey"];

        // Assert
        list.Should().NotBeNull();
        list.Should().BeEmpty();
        dict.Count.Should().Be(1);
    }

    [Fact]
    public void Indexer_Get_WithExistingKey_ReturnsList()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 10);

        // Act
        var list = dict["key1"];

        // Assert
        list.Should().Contain(10);
    }

    [Fact]
    public void Indexer_Set_ReplacesExistingList()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key1", 2);

        // Act
        dict["key1"] = new List<int> { 100, 200 };

        // Assert
        dict["key1"].Should().BeEquivalentTo(new[] { 100, 200 });
    }

    #endregion

    #region Keys Property Tests

    [Fact]
    public void Keys_ReturnsAllKeys()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);
        dict.Add("key3", 3);

        // Act
        var keys = dict.Keys;

        // Assert
        keys.Should().BeEquivalentTo(new[] { "key1", "key2", "key3" });
    }

    #endregion

    #region Values Property Tests

    [Fact]
    public void Values_ReturnsAllValuesFlattened()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key1", 2);
        dict.Add("key2", 3);

        // Act
        var values = dict.Values;

        // Assert
        values.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Values_WithEmptyDictionary_ReturnsEmptyList()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        var values = dict.Values;

        // Assert
        values.Should().BeEmpty();
    }

    #endregion

    #region ContainsKey Tests

    [Fact]
    public void ContainsKey_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);

        // Act
        var result = dict.ContainsKey("key1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsKey_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        var result = dict.ContainsKey("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsKey_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        var act = () => dict.ContainsKey(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region ContainsValue Tests

    [Fact]
    public void ContainsValue_WithExistingValue_ReturnsTrue()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 42);

        // Act
        var result = dict.ContainsValue(42);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsValue_WithNonExistingValue_ReturnsFalse()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 42);

        // Act
        var result = dict.ContainsValue(999);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_ByKey_RemovesEntireEntry()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key1", 2);

        // Act
        var result = dict.Remove("key1");

        // Assert
        result.Should().BeTrue();
        dict.ContainsKey("key1").Should().BeFalse();
    }

    [Fact]
    public void Remove_ByKeyAndValue_RemovesOnlySpecificValue()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key1", 2);
        dict.Add("key1", 3);

        // Act
        dict.Remove("key1", 2);

        // Assert
        dict["key1"].Should().BeEquivalentTo(new[] { 1, 3 });
    }

    [Fact]
    public void Remove_ByValueOnly_RemovesValueFromAllKeys()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key1", 2);
        dict.Add("key2", 2);
        dict.Add("key2", 3);

        // Act
        dict.Remove(2);

        // Assert
        dict["key1"].Should().BeEquivalentTo(new[] { 1 });
        dict["key2"].Should().BeEquivalentTo(new[] { 3 });
    }

    [Fact]
    public void Remove_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();

        // Act
        var result = dict.Remove("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);
        dict.Add("key3", 3);

        // Act
        dict.Clear();

        // Assert
        dict.Count.Should().Be(0);
    }

    #endregion

    #region FindByKey Tests

    [Fact]
    public void FindByKey_WithMatchingKeys_ReturnsAllValuesForMatchingKeys()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("apple", 1);
        dict.Add("apricot", 2);
        dict.Add("banana", 3);

        // Act
        var result = dict.FindByKey(key => key.StartsWith("ap")).ToList();

        // Assert
        result.Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public void FindByKey_WithNoMatchingKeys_ReturnsEmpty()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("apple", 1);
        dict.Add("banana", 2);

        // Act
        var result = dict.FindByKey(key => key.StartsWith("z")).ToList();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region FindByValue Tests

    [Fact]
    public void FindByValue_WithMatchingValues_ReturnsMatchingValues()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 10);
        dict.Add("key1", 20);
        dict.Add("key2", 30);

        // Act
        var result = dict.FindByValue(value => value > 15).ToList();

        // Assert
        result.Should().BeEquivalentTo(new[] { 20, 30 });
    }

    #endregion

    #region FindByKeyAndValue Tests

    [Fact]
    public void FindByKeyAndValue_WithMatchingCriteria_ReturnsMatchingValues()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("group1", 10);
        dict.Add("group1", 50);
        dict.Add("group2", 30);
        dict.Add("group2", 70);

        // Act
        var result = dict.FindByKeyAndValue(
            key => key == "group1",
            value => value > 20).ToList();

        // Assert
        result.Should().BeEquivalentTo(new[] { 50 });
    }

    #endregion

    #region IEnumerable Tests

    [Fact]
    public void GetEnumerator_EnumeratesAllKeyValuePairs()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);

        // Act
        var pairs = dict.ToList();

        // Assert
        pairs.Should().HaveCount(2);
        pairs.Should().Contain(p => p.Key == "key1" && p.Value.Contains(1));
        pairs.Should().Contain(p => p.Key == "key2" && p.Value.Contains(2));
    }

    [Fact]
    public void ForeachLoop_IteratesAllEntries()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key2", 2);
        var keys = new List<string>();

        // Act
        foreach (var pair in dict)
        {
            keys.Add(pair.Key);
        }

        // Assert
        keys.Should().BeEquivalentTo(new[] { "key1", "key2" });
    }

    #endregion

    #region Count Property Tests

    [Fact]
    public void Count_ReturnsNumberOfKeys()
    {
        // Arrange
        var dict = new ListDictionary<string, int>();
        dict.Add("key1", 1);
        dict.Add("key1", 2); // Same key, different value
        dict.Add("key2", 3);

        // Act
        var count = dict.Count;

        // Assert
        count.Should().Be(2); // Only 2 keys
    }

    #endregion
}
