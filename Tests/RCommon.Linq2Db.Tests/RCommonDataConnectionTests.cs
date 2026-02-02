using FluentAssertions;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Linq2Db;
using Xunit;

namespace RCommon.Linq2Db.Tests;

public class RCommonDataConnectionTests
{
    private DataOptions CreateSQLiteOptions()
    {
        return new DataOptions()
            .UseSQLite("Data Source=:memory:");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithDataOptions_CreatesInstance()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();

        // Act
        using var connection = new RCommonDataConnection(dataOptions);

        // Assert
        connection.Should().NotBeNull();
    }

    [Fact]
    public void RCommonDataConnection_ImplementsIDataStore()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();

        // Act
        using var connection = new RCommonDataConnection(dataOptions);

        // Assert
        connection.Should().BeAssignableTo<IDataStore>();
    }

    [Fact]
    public void RCommonDataConnection_InheritsFromDataConnection()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();

        // Act
        using var connection = new RCommonDataConnection(dataOptions);

        // Assert
        connection.Should().BeAssignableTo<DataConnection>();
    }

    #endregion

    #region GetDbConnection Tests

    [Fact]
    public void GetDbConnection_ReturnsDbConnection()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        using var connection = new RCommonDataConnection(dataOptions);

        // Act
        var dbConnection = connection.GetDbConnection();

        // Assert
        dbConnection.Should().NotBeNull();
    }

    [Fact]
    public void GetDbConnection_ReturnsSameConnectionInstance()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        using var connection = new RCommonDataConnection(dataOptions);

        // Act
        var dbConnection1 = connection.GetDbConnection();
        var dbConnection2 = connection.GetDbConnection();

        // Assert
        dbConnection1.Should().BeSameAs(dbConnection2);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_DisposesConnection()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var connection = new RCommonDataConnection(dataOptions);

        // Act
        await connection.DisposeAsync();

        // Assert - No exception means success
        // Connection should be disposed
    }

    [Fact]
    public void Dispose_DisposesConnection()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();
        var connection = new RCommonDataConnection(dataOptions);

        // Act
        connection.Dispose();

        // Assert - No exception means success
    }

    #endregion

    #region DataOptions Configuration Tests

    [Fact]
    public void Constructor_WithSQLiteProvider_ConfiguresCorrectly()
    {
        // Arrange
        var dataOptions = CreateSQLiteOptions();

        // Act
        using var connection = new RCommonDataConnection(dataOptions);

        // Assert
        connection.DataProvider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithConnectionString_SetsConnectionString()
    {
        // Arrange
        var connectionString = "Data Source=:memory:";
        var dataOptions = new DataOptions()
            .UseSQLite(connectionString);

        // Act
        using var connection = new RCommonDataConnection(dataOptions);

        // Assert
        connection.ConnectionString.Should().Be(connectionString);
    }

    #endregion
}
