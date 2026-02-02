using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Moq;
using RCommon.Persistence.Sql;
using System.Data.Common;
using Xunit;

namespace RCommon.Persistence.Tests;

public class RDbConnectionTests
{
    private readonly Mock<IOptions<RDbConnectionOptions>> _mockOptions;
    private readonly RDbConnectionOptions _options;

    public RDbConnectionTests()
    {
        _mockOptions = new Mock<IOptions<RDbConnectionOptions>>();
        _options = new RDbConnectionOptions();
        _mockOptions.Setup(x => x.Value).Returns(_options);
    }

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange & Act
        var connection = new RDbConnection(_mockOptions.Object);

        // Assert
        connection.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var action = () => new RDbConnection(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void GetDbConnection_WhenOptionsNotConfigured_ThrowsRDbConnectionException()
    {
        // Arrange
        var mockOptionsWithNull = new Mock<IOptions<RDbConnectionOptions>>();
        mockOptionsWithNull.Setup(x => x.Value).Returns((RDbConnectionOptions)null!);

        var connection = new RDbConnection(mockOptionsWithNull.Object);

        // Act
        var action = () => connection.GetDbConnection();

        // Assert
        action.Should().Throw<RDbConnectionException>()
            .WithMessage("*No options configured*");
    }

    [Fact]
    public void GetDbConnection_WhenDbFactoryNotConfigured_ThrowsRDbConnectionException()
    {
        // Arrange
        _options.DbFactory = null;
        _options.ConnectionString = "Server=localhost;Database=Test;";

        var connection = new RDbConnection(_mockOptions.Object);

        // Act
        var action = () => connection.GetDbConnection();

        // Assert
        action.Should().Throw<RDbConnectionException>()
            .WithMessage("*DbProviderFactory*");
    }

    [Fact]
    public void GetDbConnection_WhenConnectionStringNotConfigured_ThrowsRDbConnectionException()
    {
        // Arrange
        _options.DbFactory = SqlClientFactory.Instance;
        _options.ConnectionString = null;

        var connection = new RDbConnection(_mockOptions.Object);

        // Act
        var action = () => connection.GetDbConnection();

        // Assert
        action.Should().Throw<RDbConnectionException>()
            .WithMessage("*connection string*");
    }

    [Fact]
    public void GetDbConnection_WhenConnectionStringIsEmpty_ThrowsRDbConnectionException()
    {
        // Arrange
        _options.DbFactory = SqlClientFactory.Instance;
        _options.ConnectionString = "";

        var connection = new RDbConnection(_mockOptions.Object);

        // Act
        var action = () => connection.GetDbConnection();

        // Assert
        action.Should().Throw<RDbConnectionException>()
            .WithMessage("*connection string*");
    }

    [Fact]
    public void GetDbConnection_WithValidConfiguration_ReturnsDbConnection()
    {
        // Arrange
        _options.DbFactory = SqlClientFactory.Instance;
        _options.ConnectionString = "Server=localhost;Database=Test;";

        var connection = new RDbConnection(_mockOptions.Object);

        // Act
        var dbConnection = connection.GetDbConnection();

        // Assert
        dbConnection.Should().NotBeNull();
        dbConnection.ConnectionString.Should().Be(_options.ConnectionString);
    }

    [Fact]
    public void GetDbConnection_ReturnsConnectionFromFactory()
    {
        // Arrange
        _options.DbFactory = SqlClientFactory.Instance;
        _options.ConnectionString = "Server=localhost;Database=TestDb;";

        var connection = new RDbConnection(_mockOptions.Object);

        // Act
        var dbConnection = connection.GetDbConnection();

        // Assert
        dbConnection.Should().BeOfType<SqlConnection>();
    }

    [Fact]
    public void Connection_ImplementsIRDbConnection()
    {
        // Arrange & Act
        var connection = new RDbConnection(_mockOptions.Object);

        // Assert
        connection.Should().BeAssignableTo<IRDbConnection>();
    }

    [Fact]
    public void Dispose_CanBeCalledSafely()
    {
        // Arrange
        var connection = new RDbConnection(_mockOptions.Object);

        // Act
        var action = () => connection.Dispose();

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void GetDbConnection_MultipleCalls_ReturnsNewConnectionsEachTime()
    {
        // Arrange
        _options.DbFactory = SqlClientFactory.Instance;
        _options.ConnectionString = "Server=localhost;Database=Test;";

        var connection = new RDbConnection(_mockOptions.Object);

        // Act
        var dbConnection1 = connection.GetDbConnection();
        var dbConnection2 = connection.GetDbConnection();

        // Assert
        dbConnection1.Should().NotBeSameAs(dbConnection2);
    }
}
