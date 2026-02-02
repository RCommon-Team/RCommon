using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Sql;
using Xunit;

namespace RCommon.Dapper.Tests;

public class DapperPersistenceBuilderTests
{
    private readonly IServiceCollection _services;

    public DapperPersistenceBuilderTests()
    {
        _services = new ServiceCollection();
    }

    [Fact]
    public void Constructor_WithValidServiceCollection_CreatesInstance()
    {
        // Arrange & Act
        var builder = new DapperPersistenceBuilder(_services);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceCollection_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new DapperPersistenceBuilder(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Services_ReturnsInjectedServiceCollection()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var services = builder.Services;

        // Assert
        services.Should().BeSameAs(_services);
    }

    [Fact]
    public void Constructor_RegistersISqlMapperRepositoryAsTransient()
    {
        // Arrange & Act
        var builder = new DapperPersistenceBuilder(_services);

        // Assert
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(ISqlMapperRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersIWriteOnlyRepositoryAsTransient()
    {
        // Arrange & Act
        var builder = new DapperPersistenceBuilder(_services);

        // Assert
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(IWriteOnlyRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersIReadOnlyRepositoryAsTransient()
    {
        // Arrange & Act
        var builder = new DapperPersistenceBuilder(_services);

        // Assert
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(IReadOnlyRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Builder_ImplementsIDapperBuilder()
    {
        // Arrange & Act
        var builder = new DapperPersistenceBuilder(_services);

        // Assert
        builder.Should().BeAssignableTo<IDapperBuilder>();
    }

    [Fact]
    public void Builder_ImplementsIPersistenceBuilder()
    {
        // Arrange & Act
        var builder = new DapperPersistenceBuilder(_services);

        // Assert
        builder.Should().BeAssignableTo<IPersistenceBuilder>();
    }

    [Fact]
    public void AddDbConnection_WithNullDataStoreName_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var action = () => builder.AddDbConnection<TestRDbConnection>(null!, options => { });

        // Assert
        action.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void AddDbConnection_WithEmptyDataStoreName_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var action = () => builder.AddDbConnection<TestRDbConnection>(string.Empty, options => { });

        // Assert
        action.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void AddDbConnection_WithNullOptions_ThrowsRDbConnectionException()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var action = () => builder.AddDbConnection<TestRDbConnection>("TestDataStore", null!);

        // Assert
        action.Should().Throw<RDbConnectionException>();
    }

    [Fact]
    public void AddDbConnection_WithValidParameters_ReturnsIDapperBuilder()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var result = builder.AddDbConnection<TestRDbConnection>("TestDataStore", options =>
        {
            options.ConnectionString = "Server=test;Database=test;";
        });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IDapperBuilder>();
    }

    [Fact]
    public void AddDbConnection_WithValidParameters_ReturnsSameBuilder()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var result = builder.AddDbConnection<TestRDbConnection>("TestDataStore", options =>
        {
            options.ConnectionString = "Server=test;Database=test;";
        });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddDbConnection_RegistersDataStoreFactory()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        builder.AddDbConnection<TestRDbConnection>("TestDataStore", options =>
        {
            options.ConnectionString = "Server=test;Database=test;";
        });

        // Assert
        _services.Should().Contain(sd => sd.ServiceType == typeof(IDataStoreFactory));
    }

    [Fact]
    public void AddDbConnection_ConfiguresRDbConnectionOptions()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);
        var connectionString = "Server=localhost;Database=TestDb;";

        // Act
        builder.AddDbConnection<TestRDbConnection>("TestDataStore", options =>
        {
            options.ConnectionString = connectionString;
        });

        // Assert
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(IConfigureOptions<RDbConnectionOptions>));
    }

    [Fact]
    public void AddDbConnection_CanBeCalledMultipleTimes()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var result1 = builder.AddDbConnection<TestRDbConnection>("DataStore1", options =>
        {
            options.ConnectionString = "Server=test1;Database=test1;";
        });

        var result2 = builder.AddDbConnection<TestRDbConnection>("DataStore2", options =>
        {
            options.ConnectionString = "Server=test2;Database=test2;";
        });

        // Assert
        result1.Should().BeSameAs(builder);
        result2.Should().BeSameAs(builder);
    }

    [Fact]
    public void SetDefaultDataStore_WithValidOptions_ReturnsPersistenceBuilder()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var result = builder.SetDefaultDataStore(options =>
        {
            options.DefaultDataStoreName = "TestDataStore";
        });

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IPersistenceBuilder>();
    }

    [Fact]
    public void SetDefaultDataStore_ConfiguresDefaultDataStoreOptions()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);
        var expectedName = "TestDataStore";

        // Act
        builder.SetDefaultDataStore(options =>
        {
            options.DefaultDataStoreName = expectedName;
        });

        // Assert
        _services.Should().Contain(sd =>
            sd.ServiceType == typeof(IConfigureOptions<DefaultDataStoreOptions>));
    }

    [Theory]
    [InlineData("SqlDataStore")]
    [InlineData("PostgresDataStore")]
    [InlineData("MySqlDataStore")]
    [InlineData("Production.Database")]
    public void AddDbConnection_WithVariousDataStoreNames_Succeeds(string dataStoreName)
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var result = builder.AddDbConnection<TestRDbConnection>(dataStoreName, options =>
        {
            options.ConnectionString = "Server=test;Database=test;";
        });

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void AddDbConnection_SupportsFluendChaining()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var result = builder
            .AddDbConnection<TestRDbConnection>("DataStore1", options =>
            {
                options.ConnectionString = "Server=test1;";
            })
            .AddDbConnection<TestRDbConnection>("DataStore2", options =>
            {
                options.ConnectionString = "Server=test2;";
            });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void SetDefaultDataStore_SupportsFluendChaining()
    {
        // Arrange
        var builder = new DapperPersistenceBuilder(_services);

        // Act
        var result = builder
            .AddDbConnection<TestRDbConnection>("TestDataStore", options =>
            {
                options.ConnectionString = "Server=test;";
            })
            .SetDefaultDataStore(options =>
            {
                options.DefaultDataStoreName = "TestDataStore";
            });

        // Assert
        result.Should().NotBeNull();
    }
}

/// <summary>
/// Test implementation of RDbConnection for testing purposes.
/// </summary>
public class TestRDbConnection : RDbConnection
{
    public TestRDbConnection(IOptions<RDbConnectionOptions> options) : base(options)
    {
    }
}
