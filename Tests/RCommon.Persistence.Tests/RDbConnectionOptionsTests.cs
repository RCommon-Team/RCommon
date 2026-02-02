using FluentAssertions;
using Microsoft.Data.SqlClient;
using RCommon.Persistence.Sql;
using System.Data.Common;
using Xunit;

namespace RCommon.Persistence.Tests;

public class RDbConnectionOptionsTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Arrange & Act
        var options = new RDbConnectionOptions();

        // Assert
        options.Should().NotBeNull();
        options.DbFactory.Should().BeNull();
        options.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void DbFactory_CanBeSetAndGet()
    {
        // Arrange
        var options = new RDbConnectionOptions();
        var expectedFactory = SqlClientFactory.Instance;

        // Act
        options.DbFactory = expectedFactory;

        // Assert
        options.DbFactory.Should().Be(expectedFactory);
    }

    [Fact]
    public void ConnectionString_CanBeSetAndGet()
    {
        // Arrange
        var options = new RDbConnectionOptions();
        var expectedConnectionString = "Server=localhost;Database=TestDb;Trusted_Connection=True;";

        // Act
        options.ConnectionString = expectedConnectionString;

        // Assert
        options.ConnectionString.Should().Be(expectedConnectionString);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Server=localhost;")]
    [InlineData("Server=myserver;Database=mydb;User Id=user;Password=pass;")]
    [InlineData("Data Source=.;Initial Catalog=Test;Integrated Security=True;")]
    public void ConnectionString_CanBeSetToVariousFormats(string connectionString)
    {
        // Arrange
        var options = new RDbConnectionOptions();

        // Act
        options.ConnectionString = connectionString;

        // Assert
        options.ConnectionString.Should().Be(connectionString);
    }

    [Fact]
    public void DbFactory_CanBeSetToSqlClientFactory()
    {
        // Arrange
        var options = new RDbConnectionOptions();

        // Act
        options.DbFactory = SqlClientFactory.Instance;

        // Assert
        options.DbFactory.Should().Be(SqlClientFactory.Instance);
    }

    [Fact]
    public void DbFactory_CanBeSetToNull()
    {
        // Arrange
        var options = new RDbConnectionOptions();
        options.DbFactory = SqlClientFactory.Instance;

        // Act
        options.DbFactory = null;

        // Assert
        options.DbFactory.Should().BeNull();
    }

    [Fact]
    public void ConnectionString_CanBeSetToNull()
    {
        // Arrange
        var options = new RDbConnectionOptions();
        options.ConnectionString = "Server=localhost;";

        // Act
        options.ConnectionString = null;

        // Assert
        options.ConnectionString.Should().BeNull();
    }

    [Fact]
    public void MultipleInstances_AreIndependent()
    {
        // Arrange
        var options1 = new RDbConnectionOptions();
        var options2 = new RDbConnectionOptions();

        // Act
        options1.ConnectionString = "Server=server1;";
        options1.DbFactory = SqlClientFactory.Instance;

        // Assert
        options2.ConnectionString.Should().BeNull();
        options2.DbFactory.Should().BeNull();
    }

    [Fact]
    public void Options_AllPropertiesCanBeSetTogether()
    {
        // Arrange
        var options = new RDbConnectionOptions();
        var expectedConnectionString = "Server=localhost;Database=Test;";
        var expectedFactory = SqlClientFactory.Instance;

        // Act
        options.ConnectionString = expectedConnectionString;
        options.DbFactory = expectedFactory;

        // Assert
        options.ConnectionString.Should().Be(expectedConnectionString);
        options.DbFactory.Should().Be(expectedFactory);
    }
}
