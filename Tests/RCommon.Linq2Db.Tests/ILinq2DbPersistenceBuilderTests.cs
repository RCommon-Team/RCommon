using FluentAssertions;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.DataProvider.SQLite;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Linq2Db;
using Xunit;

namespace RCommon.Linq2Db.Tests;

public class ILinq2DbPersistenceBuilderTests
{
    private DataOptions CreateSQLiteOptions()
    {
        return new DataOptions()
            .UseSQLite("Data Source=:memory:");
    }

    #region Interface Implementation Tests

    [Fact]
    public void ILinq2DbPersistenceBuilder_ExtendsIPersistenceBuilder()
    {
        // Assert - compile-time check that ILinq2DbPersistenceBuilder extends IPersistenceBuilder
        typeof(ILinq2DbPersistenceBuilder).Should().Implement<IPersistenceBuilder>();
    }

    [Fact]
    public void ILinq2DbPersistenceBuilder_DefinesAddDataConnectionMethod()
    {
        // Arrange
        var methodInfo = typeof(ILinq2DbPersistenceBuilder).GetMethod("AddDataConnection");

        // Assert
        methodInfo.Should().NotBeNull();
        methodInfo!.IsGenericMethod.Should().BeTrue();
        methodInfo.ReturnType.Should().Be(typeof(ILinq2DbPersistenceBuilder));
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

    #endregion

    #region AddDataConnection Method Signature Tests

    [Fact]
    public void AddDataConnection_HasCorrectGenericConstraint()
    {
        // Arrange
        var methodInfo = typeof(ILinq2DbPersistenceBuilder).GetMethod("AddDataConnection");
        var genericArguments = methodInfo!.GetGenericArguments();

        // Assert
        genericArguments.Should().HaveCount(1);
        var constraint = genericArguments[0].GetGenericParameterConstraints();
        constraint.Should().Contain(typeof(RCommonDataConnection));
    }

    [Fact]
    public void AddDataConnection_HasCorrectParameters()
    {
        // Arrange
        var methodInfo = typeof(ILinq2DbPersistenceBuilder).GetMethod("AddDataConnection");
        var parameters = methodInfo!.GetParameters();

        // Assert
        parameters.Should().HaveCount(2);
        parameters[0].Name.Should().Be("dataStoreName");
        parameters[0].ParameterType.Should().Be(typeof(string));
        parameters[1].Name.Should().Be("options");
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void AddDataConnection_ReturnsSameBuilder_AllowsMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder.AddDataConnection<TestDataConnection>(
            "Store1",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddDataConnection_CanChainMultipleCalls()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act & Assert - Fluent chaining should work
        var result = builder
            .AddDataConnection<TestDataConnection>(
                "Store1",
                (sp, options) => options.UseSQLite("Data Source=:memory:"))
            .AddDataConnection<TestDataConnection>(
                "Store2",
                (sp, options) => options.UseSQLite("Data Source=:memory:"));

        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddDataConnection_CanChainWithSetDefaultDataStore()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder
            .AddDataConnection<TestDataConnection>(
                "TestStore",
                (sp, options) => options.UseSQLite("Data Source=:memory:"))
            .SetDefaultDataStore(options => options.DefaultDataStoreName = "TestStore");

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Generic Type Parameter Tests

    [Fact]
    public void AddDataConnection_WorksWithCustomDataConnection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder.AddDataConnection<CustomDataConnection>(
            "CustomStore",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void AddDataConnection_WorksWithDerivedDataConnection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new Linq2DbPersistenceBuilder(services);

        // Act
        var result = builder.AddDataConnection<DerivedDataConnection>(
            "DerivedStore",
            (sp, options) => options.UseSQLite("Data Source=:memory:"));

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

    public class CustomDataConnection : RCommonDataConnection
    {
        public CustomDataConnection(DataOptions dataOptions) : base(dataOptions)
        {
        }

        public string CustomProperty { get; set; } = string.Empty;
    }

    public class DerivedDataConnection : TestDataConnection
    {
        public DerivedDataConnection(DataOptions dataOptions) : base(dataOptions)
        {
        }
    }

    #endregion
}
