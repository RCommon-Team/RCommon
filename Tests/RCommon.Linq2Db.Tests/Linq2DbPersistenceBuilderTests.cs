using FluentAssertions;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.DataProvider.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Linq2Db;
using RCommon.Persistence.Linq2Db.Crud;
using Xunit;

namespace RCommon.Linq2Db.Tests;

public class Linq2DbPersistenceBuilderTests
{
    private DataOptions CreateSQLiteOptions()
    {
        return new DataOptions()
            .UseSQLite("Data Source=:memory:");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServices_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new Linq2DbPersistenceBuilder(services);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act
        var act = () => new Linq2DbPersistenceBuilder(services!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Linq2DbPersistenceBuilder_ImplementsILinq2DbPersistenceBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new Linq2DbPersistenceBuilder(services);

        // Assert
        builder.Should().BeAssignableTo<ILinq2DbPersistenceBuilder>();
    }

    [Fact]
    public void Linq2DbPersistenceBuilder_ImplementsIPersistenceBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new Linq2DbPersistenceBuilder(services);

        // Assert
        builder.Should().BeAssignableTo<IPersistenceBuilder>();
    }

    #endregion

    #region Services Property Tests

    [Fact]
    public void Services_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder.Services;

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion

    #region Constructor Registration Tests

    [Fact]
    public void Constructor_RegistersIReadOnlyRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new Linq2DbPersistenceBuilder(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IReadOnlyRepository<>) &&
            sd.ImplementationType == typeof(Linq2DbRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersIWriteOnlyRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new Linq2DbPersistenceBuilder(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IWriteOnlyRepository<>) &&
            sd.ImplementationType == typeof(Linq2DbRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersILinqRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new Linq2DbPersistenceBuilder(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ILinqRepository<>) &&
            sd.ImplementationType == typeof(Linq2DbRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    #endregion

    #region AddDataConnection Tests

    [Fact]
    public void AddDataConnection_WithValidParameters_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder.AddDataConnection<TestDataConnection>(
            "TestStore",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddDataConnection_WithEmptyDataStoreName_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var act = () => builder.AddDataConnection<TestDataConnection>(
            "",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        act.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void AddDataConnection_WithNullDataStoreName_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var act = () => builder.AddDataConnection<TestDataConnection>(
            null!,
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        act.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void AddDataConnection_WithNullOptions_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var act = () => builder.AddDataConnection<TestDataConnection>(
            "TestStore",
            null!);

        // Assert
        act.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void AddDataConnection_RegistersIDataStoreFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        builder.AddDataConnection<TestDataConnection>(
            "TestStore",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IDataStoreFactory) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddDataConnection_ConfiguresDataStoreFactoryOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        builder.AddDataConnection<TestDataConnection>(
            "TestStore",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType.Name.Contains("IConfigureOptions"));
    }

    [Fact]
    public void AddDataConnection_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        builder.AddDataConnection<TestDataConnection>(
            "TestStore1",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));
        builder.AddDataConnection<TestDataConnection>(
            "TestStore2",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert - No exception means success
        builder.Should().NotBeNull();
    }

    [Fact]
    public void AddDataConnection_ReturnsSameBuilderForFluent()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result1 = builder.AddDataConnection<TestDataConnection>(
            "TestStore1",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));
        var result2 = result1.AddDataConnection<TestDataConnection>(
            "TestStore2",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        result1.Should().BeSameAs(builder);
        result2.Should().BeSameAs(builder);
    }

    #endregion

    #region SetDefaultDataStore Tests

    [Fact]
    public void SetDefaultDataStore_WithValidOptions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder.SetDefaultDataStore(options =>
        {
            options.DefaultDataStoreName = "DefaultStore";
        });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void SetDefaultDataStore_ConfiguresDefaultDataStoreOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        builder.SetDefaultDataStore(options =>
        {
            options.DefaultDataStoreName = "MyDefaultStore";
        });

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType.Name.Contains("IConfigureOptions"));
    }

    [Fact]
    public void SetDefaultDataStore_CanBeChainedWithAddDataConnection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder
            .AddDataConnection<TestDataConnection>(
                "TestStore",
                (sp, options) => options.UseSQLite("Data Source=:memory:"))
            .SetDefaultDataStore(options =>
            {
                options.DefaultDataStoreName = "TestStore";
            });

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Helper Classes

    public class TestDataConnection : RCommonDataConnection
    {
        public TestDataConnection(DataOptions dataOptions) : base(dataOptions)
        {
        }
    }

    #endregion
}
