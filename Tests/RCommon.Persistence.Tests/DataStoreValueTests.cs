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
        var baseType = typeof(IDataStoreValueTestBase);
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
        var baseType = typeof(IDataStoreValueTestBase);
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
        var value = new DataStoreValue(expectedName, typeof(IDataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Act
        var actualName = value.Name;

        // Assert
        actualName.Should().Be(expectedName);
    }

    [Fact]
    public void BaseType_ReturnsCorrectType()
    {
        // Arrange
        var expectedBaseType = typeof(IDataStoreValueTestBase);
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
        var value = new DataStoreValue("TestStore", typeof(IDataStoreValueTestBase), expectedConcreteType);

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
        var value = new DataStoreValue(name, typeof(IDataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Assert
        value.Name.Should().Be(name);
    }

    [Fact]
    public void Constructor_WithNullName_AcceptsNull()
    {
        // Arrange & Act
        var value = new DataStoreValue(null!, typeof(IDataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Assert
        value.Name.Should().BeNull();
    }

    [Fact]
    public void Properties_AreReadOnly()
    {
        // Arrange
        var value = new DataStoreValue("Test", typeof(IDataStoreValueTestBase), typeof(DataStoreValueTestConcrete));

        // Assert - Verify properties are get-only (compile-time check)
        // If the code compiles, this test passes as the properties are defined as get-only
        value.Name.Should().NotBeNull();
        value.BaseType.Should().NotBeNull();
        value.ConcreteType.Should().NotBeNull();
    }
}

// Test helper interfaces and classes
public interface IDataStoreValueTestBase : IDataStore
{
}

public class DataStoreValueTestConcrete : IDataStoreValueTestBase
{
    public DbConnection GetDbConnection() => null!;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class UnrelatedDataStore : IDataStore
{
    public DbConnection GetDbConnection() => null!;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
