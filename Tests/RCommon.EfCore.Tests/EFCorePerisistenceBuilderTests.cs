using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.EFCore;
using RCommon.Persistence.EFCore.Crud;
using Xunit;

namespace RCommon.EfCore.Tests;

public class EFCorePerisistenceBuilderTests
{
    [Fact]
    public void Constructor_WithValidServices_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        var action = () => new EFCorePerisistenceBuilder(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Constructor_RegistersReadOnlyRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IReadOnlyRepository<>) &&
            sd.ImplementationType == typeof(EFCoreRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersWriteOnlyRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IWriteOnlyRepository<>) &&
            sd.ImplementationType == typeof(EFCoreRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersLinqRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ILinqRepository<>) &&
            sd.ImplementationType == typeof(EFCoreRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Constructor_RegistersGraphRepository()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IGraphRepository<>) &&
            sd.ImplementationType == typeof(EFCoreRepository<>) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void Services_ReturnsInjectedServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);

        // Act
        var result = builder.Services;

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddDbContext_WithValidParameters_RegistersDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);
        var dataStoreName = "TestDataStore";

        // Act
        var result = builder.AddDbContext<TestDbContext>(dataStoreName, options =>
            options.UseInMemoryDatabase(dataStoreName));

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddDbContext_WithValidParameters_RegistersDataStoreFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);
        var dataStoreName = "TestDataStore";

        // Act
        builder.AddDbContext<TestDbContext>(dataStoreName, options =>
            options.UseInMemoryDatabase(dataStoreName));

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IDataStoreFactory) &&
            sd.ImplementationType == typeof(DataStoreFactory) &&
            sd.Lifetime == ServiceLifetime.Transient);
    }

    [Fact]
    public void AddDbContext_WithNullDataStoreName_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);

        // Act
        var action = () => builder.AddDbContext<TestDbContext>(null!, options =>
            options.UseInMemoryDatabase("test"));

        // Assert
        action.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void AddDbContext_WithEmptyDataStoreName_ThrowsUnsupportedDataStoreException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);

        // Act
        var action = () => builder.AddDbContext<TestDbContext>(string.Empty, options =>
            options.UseInMemoryDatabase("test"));

        // Assert
        action.Should().Throw<UnsupportedDataStoreException>();
    }

    [Fact]
    public void AddDbContext_ReturnsSameBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);
        var dataStoreName = "TestDataStore";

        // Act
        var result = builder.AddDbContext<TestDbContext>(dataStoreName, options =>
            options.UseInMemoryDatabase(dataStoreName));

        // Assert
        result.Should().BeOfType<EFCorePerisistenceBuilder>();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddDbContext_WithNullOptions_RegistersDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);
        var dataStoreName = "TestDataStore";

        // Act
        var result = builder.AddDbContext<TestDbContext>(dataStoreName, null);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void SetDefaultDataStore_WithValidOptions_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);

        // Act
        var result = builder.SetDefaultDataStore(options =>
            options.DefaultDataStoreName = "TestDataStore");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void Builder_ImplementsIEFCorePersistenceBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        builder.Should().BeAssignableTo<IEFCorePersistenceBuilder>();
    }

    [Fact]
    public void Builder_ImplementsIPersistenceBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        builder.Should().BeAssignableTo<IPersistenceBuilder>();
    }

    [Fact]
    public void AddDbContext_MultipleCalls_RegistersMultipleDbContexts()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);

        // Act
        builder.AddDbContext<TestDbContext>("DataStore1", options =>
            options.UseInMemoryDatabase("DataStore1"));
        builder.AddDbContext<SecondTestDbContext>("DataStore2", options =>
            options.UseInMemoryDatabase("DataStore2"));

        // Assert
        services.Should().Contain(sd => sd.ServiceType == typeof(TestDbContext));
        services.Should().Contain(sd => sd.ServiceType == typeof(SecondTestDbContext));
    }

    [Theory]
    [InlineData("MyDataStore")]
    [InlineData("TestDB")]
    [InlineData("production-database")]
    [InlineData("DataStore_123")]
    public void AddDbContext_WithVariousValidNames_Succeeds(string dataStoreName)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new EFCorePerisistenceBuilder(services);

        // Act
        var result = builder.AddDbContext<TestDbContext>(dataStoreName, options =>
            options.UseInMemoryDatabase(dataStoreName));

        // Assert
        result.Should().NotBeNull();
    }
}

/// <summary>
/// Second test DbContext for multiple registration tests.
/// </summary>
public class SecondTestDbContext : RCommonDbContext
{
    public SecondTestDbContext(DbContextOptions<SecondTestDbContext> options) : base(options)
    {
    }
}
