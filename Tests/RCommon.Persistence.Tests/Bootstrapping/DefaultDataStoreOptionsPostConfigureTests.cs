using System;
using System.Data.Common;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RCommon;
using RCommon.Persistence;
using Xunit;

namespace RCommon.Persistence.Tests.Bootstrapping;

public class DefaultDataStoreOptionsPostConfigureTests
{
    private static IOptions<DataStoreFactoryOptions> BuildFactoryOptions(params string[] registeredNames)
    {
        var factoryOptions = new DataStoreFactoryOptions();
        foreach (var name in registeredNames)
        {
            factoryOptions.Register<FakeDataStoreBase, FakeDataStore>(name);
        }

        var mock = new Mock<IOptions<DataStoreFactoryOptions>>();
        mock.Setup(x => x.Value).Returns(factoryOptions);
        return mock.Object;
    }

    public abstract class FakeDataStoreBase : IDataStore
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public DbConnection GetDbConnection() => throw new NotSupportedException("Test fake");
    }
    public class FakeDataStore : FakeDataStoreBase { }

    [Fact]
    public void PostConfigure_WithExactlyOneRegisteredDataStore_InfersItAsDefault()
    {
        // Arrange
        var factoryOptions = BuildFactoryOptions("OnlyStore");
        var postConfigure = new DefaultDataStoreOptionsPostConfigure(factoryOptions);
        var options = new DefaultDataStoreOptions();

        // Act
        postConfigure.PostConfigure(null, options);

        // Assert
        options.DefaultDataStoreName.Should().Be("OnlyStore");
    }

    [Fact]
    public void PostConfigure_WithMultipleRegisteredDataStores_DoesNotInferADefault()
    {
        // Arrange
        var factoryOptions = BuildFactoryOptions("Store1", "Store2");
        var postConfigure = new DefaultDataStoreOptionsPostConfigure(factoryOptions);
        var options = new DefaultDataStoreOptions();

        // Act
        postConfigure.PostConfigure(null, options);

        // Assert
        options.DefaultDataStoreName.Should().BeNullOrEmpty();
    }

    [Fact]
    public void PostConfigure_WithNoRegisteredDataStores_DoesNotInferADefault()
    {
        // Arrange
        var factoryOptions = BuildFactoryOptions();
        var postConfigure = new DefaultDataStoreOptionsPostConfigure(factoryOptions);
        var options = new DefaultDataStoreOptions();

        // Act
        postConfigure.PostConfigure(null, options);

        // Assert
        options.DefaultDataStoreName.Should().BeNullOrEmpty();
    }

    [Fact]
    public void PostConfigure_WhenExplicitDefaultAlreadySet_NeverOverridesIt()
    {
        // Arrange -- consumer already called SetDefaultDataStore(...) explicitly, even though
        // multiple stores are registered (or even a store other than the one registered exists)
        var factoryOptions = BuildFactoryOptions("Store1", "Store2");
        var postConfigure = new DefaultDataStoreOptionsPostConfigure(factoryOptions);
        var options = new DefaultDataStoreOptions { DefaultDataStoreName = "Store1" };

        // Act
        postConfigure.PostConfigure(null, options);

        // Assert
        options.DefaultDataStoreName.Should().Be("Store1");
    }

    [Fact]
    public void PostConfigure_WithExactlyOneRegisteredDataStore_LogsInformation()
    {
        // Arrange
        var factoryOptions = BuildFactoryOptions("OnlyStore");
        var mockLogger = new Mock<ILogger<DefaultDataStoreOptions>>();
        var postConfigure = new DefaultDataStoreOptionsPostConfigure(factoryOptions, mockLogger.Object);
        var options = new DefaultDataStoreOptions();

        // Act
        postConfigure.PostConfigure(null, options);

        // Assert
        mockLogger.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void PostConfigure_WithoutLogger_DoesNotThrow()
    {
        // Arrange -- ILogger<T> is an optional constructor parameter; must not NRE when absent
        var factoryOptions = BuildFactoryOptions("OnlyStore");
        var postConfigure = new DefaultDataStoreOptionsPostConfigure(factoryOptions, logger: null);
        var options = new DefaultDataStoreOptions();

        // Act
        var action = () => postConfigure.PostConfigure(null, options);

        // Assert
        action.Should().NotThrow();
        options.DefaultDataStoreName.Should().Be("OnlyStore");
    }
}
