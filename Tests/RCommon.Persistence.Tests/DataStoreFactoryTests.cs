using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using System.Collections.Concurrent;
using System.Data.Common;
using Xunit;

namespace RCommon.Persistence.Tests;

public class DataStoreFactoryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IOptions<DataStoreFactoryOptions>> _mockOptions;
    private readonly DataStoreFactoryOptions _options;

    public DataStoreFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockOptions = new Mock<IOptions<DataStoreFactoryOptions>>();
        _options = new DataStoreFactoryOptions();
        _mockOptions.Setup(x => x.Value).Returns(_options);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var factory = new DataStoreFactory(_mockServiceProvider.Object, _mockOptions.Object);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Resolve_WithBaseType_WhenDataStoreExists_ReturnsDataStore()
    {
        // Arrange
        var dataStoreName = "TestDataStore";
        var mockDataStore = new Mock<TestDataStore>();

        _options.Register<TestDataStoreBase, TestDataStore>(dataStoreName);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(TestDataStore)))
            .Returns(mockDataStore.Object);

        var factory = new DataStoreFactory(_mockServiceProvider.Object, _mockOptions.Object);

        // Act
        var result = factory.Resolve<TestDataStoreBase>(dataStoreName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<TestDataStoreBase>();
    }

    [Fact]
    public void Resolve_WithBaseType_WhenDataStoreNotFound_ThrowsDataStoreNotFoundException()
    {
        // Arrange
        var factory = new DataStoreFactory(_mockServiceProvider.Object, _mockOptions.Object);

        // Act
        var action = () => factory.Resolve<TestDataStoreBase>("NonExistentDataStore");

        // Assert
        action.Should().Throw<DataStoreNotFoundException>()
            .WithMessage("*NonExistentDataStore*");
    }

    [Fact]
    public void Resolve_WithBaseType_WhenNameExistsButDifferentBaseType_ThrowsDataStoreNotFoundException()
    {
        // Arrange
        var dataStoreName = "TestDataStore";
        _options.Register<TestDataStoreBase, TestDataStore>(dataStoreName);

        var factory = new DataStoreFactory(_mockServiceProvider.Object, _mockOptions.Object);

        // Act
        var action = () => factory.Resolve<AnotherDataStoreBase>(dataStoreName);

        // Assert
        action.Should().Throw<DataStoreNotFoundException>();
    }

    [Theory]
    [InlineData("DataStore1")]
    [InlineData("DataStore2")]
    [InlineData("my-data-store")]
    public void Resolve_WithBaseType_WithVariousNames_ReturnsCorrectDataStore(string dataStoreName)
    {
        // Arrange
        var mockDataStore = new Mock<TestDataStore>();

        _options.Register<TestDataStoreBase, TestDataStore>(dataStoreName);
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(TestDataStore)))
            .Returns(mockDataStore.Object);

        var factory = new DataStoreFactory(_mockServiceProvider.Object, _mockOptions.Object);

        // Act
        var result = factory.Resolve<TestDataStoreBase>(dataStoreName);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void Resolve_WithMultipleRegistrations_ReturnsCorrectDataStore()
    {
        // Arrange
        var dataStore1Name = "DataStore1";
        var dataStore2Name = "DataStore2";

        var mockDataStore1 = new Mock<TestDataStore>();
        var mockDataStore2 = new Mock<TestDataStore>();

        _options.Register<TestDataStoreBase, TestDataStore>(dataStore1Name);
        _options.Register<TestDataStoreBase, TestDataStore>(dataStore2Name);

        _mockServiceProvider
            .Setup(x => x.GetService(typeof(TestDataStore)))
            .Returns(mockDataStore1.Object);

        var factory = new DataStoreFactory(_mockServiceProvider.Object, _mockOptions.Object);

        // Act
        var result = factory.Resolve<TestDataStoreBase>(dataStore1Name);

        // Assert
        result.Should().NotBeNull();
        _mockServiceProvider.Verify(x => x.GetService(typeof(TestDataStore)), Times.Once);
    }
}

// Test helper classes - must use class inheritance (not interfaces)
// because DataStoreValue checks concreteType.BaseType == baseType
public abstract class TestDataStoreBase : IDataStore
{
    public virtual DbConnection GetDbConnection() => null!;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class TestDataStore : TestDataStoreBase
{
    public override DbConnection GetDbConnection() => null!;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public abstract class AnotherDataStoreBase : IDataStore
{
    public virtual DbConnection GetDbConnection() => null!;
    public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public class AnotherDataStore : AnotherDataStoreBase
{
    public override DbConnection GetDbConnection() => null!;
    public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
