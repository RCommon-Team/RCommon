using FluentAssertions;
using RCommon;
using Xunit;

namespace RCommon.Persistence.Tests;

public class DefaultDataStoreOptionsTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Arrange & Act
        var options = new DefaultDataStoreOptions();

        // Assert
        options.Should().NotBeNull();
        options.DefaultDataStoreName.Should().BeNull();
    }

    [Fact]
    public void DefaultDataStoreName_CanBeSetAndGet()
    {
        // Arrange
        var options = new DefaultDataStoreOptions();
        var expectedName = "MyDefaultDataStore";

        // Act
        options.DefaultDataStoreName = expectedName;

        // Assert
        options.DefaultDataStoreName.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("DataStore1")]
    [InlineData("my-data-store")]
    [InlineData("DataStore_123")]
    public void DefaultDataStoreName_CanBeSetToVariousValues(string name)
    {
        // Arrange
        var options = new DefaultDataStoreOptions();

        // Act
        options.DefaultDataStoreName = name;

        // Assert
        options.DefaultDataStoreName.Should().Be(name);
    }

    [Fact]
    public void DefaultDataStoreName_CanBeSetToNull()
    {
        // Arrange
        var options = new DefaultDataStoreOptions();
        options.DefaultDataStoreName = "SomeName";

        // Act
        options.DefaultDataStoreName = null!;

        // Assert
        options.DefaultDataStoreName.Should().BeNull();
    }

    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange
        var options1 = new DefaultDataStoreOptions();
        var options2 = new DefaultDataStoreOptions();

        // Act
        options1.DefaultDataStoreName = "DataStore1";

        // Assert
        options1.DefaultDataStoreName.Should().Be("DataStore1");
        options2.DefaultDataStoreName.Should().BeNull();
    }

    [Fact]
    public void DefaultDataStoreName_PropertyGetterAndSetter_WorkCorrectly()
    {
        // Arrange
        var options = new DefaultDataStoreOptions();
        var expectedValue = "TestDataStore";

        // Act
        options.DefaultDataStoreName = expectedValue;
        var actualValue = options.DefaultDataStoreName;

        // Assert
        actualValue.Should().Be(expectedValue);
    }
}
