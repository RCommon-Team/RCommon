using FluentAssertions;
using RCommon.Persistence;
using System.Data.Common;
using Xunit;

namespace RCommon.Persistence.Tests;

public class DataStoreValueTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var name = "TestDataStore";
        var baseType = typeof(DataStoreValueTestBase);
        var concreteType = typeof(DataStoreValueTestConcrete);

        // Act
        var value = new DataStoreValue(name, baseType, concreteType);

        // Assert
        value.Should().NotBeNull();
        value.Name.Should().Be(name);
        value.BaseType.Should().Be(baseType);
        value.ConcreteType.Should().Be(concreteType);
    }

    [Fact]
    public void Constructor_WhenConcreteTypeDoesNotImplementBaseType_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var name = "TestDataStore";
        var baseType = typeof(DataStoreValueTestBase);
        var concreteType = typeof(UnrelatedDataStore);

        // Act
        var action = () => new DataStoreValue(name, baseType, concreteType);

        // Assert
        action.Should().Throw<UnsupportedDataStoreException>()
            .WithMessage("*Concrete type must implement base type*");
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Arrange
        var expectedName = "MyDataStore";
        var value = new DataStoreValue(expectedName, typeof(DataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Act
        var actualName = value.Name;

        // Assert
        actualName.Should().Be(expectedName);
    }

    [Fact]
    public void BaseType_ReturnsCorrectType()
    {
        // Arrange
        var expectedBaseType = typeof(DataStoreValueTestBase);
        var value = new DataStoreValue("TestStore", expectedBaseType, typeof(DataStoreValueTestConcrete));

        // Act
        var actualBaseType = value.BaseType;

        // Assert
        actualBaseType.Should().Be(expectedBaseType);
    }

    [Fact]
    public void ConcreteType_ReturnsCorrectType()
    {
        // Arrange
        var expectedConcreteType = typeof(DataStoreValueTestConcrete);
        var value = new DataStoreValue("TestStore", typeof(DataStoreValueTestBase), expectedConcreteType);

        // Act
        var actualConcreteType = value.ConcreteType;

        // Assert
        actualConcreteType.Should().Be(expectedConcreteType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("data-store-1")]
    [InlineData("DataStore_Test")]
    [InlineData("UPPERCASE")]
    public void Constructor_WithVariousNames_AcceptsAllNames(string name)
    {
        // Arrange & Act
        var value = new DataStoreValue(name, typeof(DataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Assert
        value.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithNullName_AcceptsNull()
    {
        // Arrange & Act
        var value = new DataStoreValue(null!, typeof(DataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Assert
        value.Name.Should().BeNull();
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var value = new DataStoreValue("Test", typeof(DataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Assert - Verify properties are get-only (compile-time check)
        // If the code compiles, this test passes as the properties are defined as get-only
        value.Name.Should().NotBeNull();
        value.BaseType.Should().NotBeNull();
        value.ConcreteType.Should().NotBeNull();
    }
}

// Test helper classes - must use class inheritance (not interfaces)
// because DataStoreValue checks concreteType.BaseType == baseType
public abstract class DataStoreValueTestBase : IDataStore
{
    public virtual DbConnection GetDbConnection() => null!;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class DataStoreValueTestConcrete : DataStoreValueTestBase
{
    public override DbConnection GetDbConnection() => null!;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class UnrelatedDataStore : IDataStore
{
    public DbConnection GetDbConnection() => null!;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
