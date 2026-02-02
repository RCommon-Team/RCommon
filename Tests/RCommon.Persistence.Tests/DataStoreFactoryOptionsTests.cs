using FluentAssertions;
using RCommon.Persistence;
using System.Data.Common;
using Xunit;

namespace RCommon.Persistence.Tests;

public class DataStoreFactoryOptionsTests
{
    [Fact]
    public void Constructor_CreatesEmptyValuesCollection()
    {
        // Arrange & Act
        var options = new DataStoreFactoryOptions();

        // Assert
        options.Values.Should().NotBeNull();
        options.Values.Should().BeEmpty();
    }

    [Fact]
    public void Register_WithValidTypes_AddsDataStoreValue()
    {
        // Arrange
        var options = new DataStoreFactoryOptions();
        var dataStoreName = "TestDataStore";

        // Act
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>(dataStoreName);

        // Assert
        options.Values.Should().HaveCount(1);
        options.Values.Should().Contain(x =>
            x.Name == dataStoreName &&
            x.BaseType == typeof(TestDataStoreForOptionsBase) &&
            x.ConcreteType == typeof(TestDataStoreForOptions));
    }

    [Fact]
    public void Register_WithSameNameAndBaseType_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var options = new DataStoreFactoryOptions();
        var dataStoreName = "TestDataStore";
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>(dataStoreName);

        // Act
        var action = () => options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>(dataStoreName);

        // Assert
        action.Should().Throw<UnsupportedDataStoreException>()
            .WithMessage($"*{dataStoreName}*");
    }

    [Fact]
    public void Register_WithSameNameButDifferentBaseType_SuccessfullyAdds()
    {
        // Arrange
        var options = new DataStoreFactoryOptions();
        var dataStoreName = "TestDataStore";
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>(dataStoreName);

        // Act
        options.Register<AnotherDataStoreForOptionsBase, AnotherDataStoreForOptions>(dataStoreName);

        // Assert
        options.Values.Should().HaveCount(2);
    }

    [Fact]
    public void Register_WithDifferentNamesAndSameBaseType_SuccessfullyAddsBoth()
    {
        // Arrange
        var options = new DataStoreFactoryOptions();

        // Act
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>("DataStore1");
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>("DataStore2");

        // Assert
        options.Values.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("my-data-store")]
    [InlineData("DataStore_123")]
    public void Register_WithVariousNames_SuccessfullyAdds(string dataStoreName)
    {
        // Arrange
        var options = new DataStoreFactoryOptions();

        // Act
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>(dataStoreName);

        // Assert
        options.Values.Should().HaveCount(1);
        options.Values.Should().Contain(x => x.Name == dataStoreName);
    }

    [Fact]
    public void Register_MultipleDataStores_AllAreAccessible()
    {
        // Arrange
        var options = new DataStoreFactoryOptions();

        // Act
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>("DataStore1");
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>("DataStore2");
        options.Register<TestDataStoreForOptionsBase, TestDataStoreForOptions>("DataStore3");

        // Assert
        options.Values.Should().HaveCount(3);
        options.Values.Should().Contain(x => x.Name == "DataStore1");
        options.Values.Should().Contain(x => x.Name == "DataStore2");
        options.Values.Should().Contain(x => x.Name == "DataStore3");
    }

    [Fact]
    public void Values_IsConcurrentBag_SupportsThreadSafeOperations()
    {
        // Arrange
        var options = new DataStoreFactoryOptions();

        // Act & Assert - Verify it's a ConcurrentBag by checking type
        options.Values.Should().BeOfType<System.Collections.Concurrent.ConcurrentBag<DataStoreValue>>();
    }
}

// Test helper classes - must use class inheritance (not interfaces)
// because DataStoreValue checks concreteType.BaseType == baseType
public abstract class TestDataStoreForOptionsBase : IDataStore
{
    public virtual DbConnection GetDbConnection() => null!;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class TestDataStoreForOptions : TestDataStoreForOptionsBase
{
    public override DbConnection GetDbConnection() => null!;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public abstract class AnotherDataStoreForOptionsBase : IDataStore
{
    public virtual DbConnection GetDbConnection() => null!;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class AnotherDataStoreForOptions : AnotherDataStoreForOptionsBase
{
    public override DbConnection GetDbConnection() => null!;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
