using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence.EFCore;
using Xunit;

namespace RCommon.EfCore.Tests;

public class IEFCorePersistenceBuilderTests
{
    [Fact]
    public void IEFCorePersistenceBuilder_InheritsFromIPersistenceBuilder()
    {
        // Arrange & Act
        var interfaceType = typeof(IEFCorePersistenceBuilder);

        // Assert
        interfaceType.Should().Implement<IPersistenceBuilder>();
    }

    [Fact]
    public void IEFCorePersistenceBuilder_HasAddDbContextMethod()
    {
        // Arrange
        var interfaceType = typeof(IEFCorePersistenceBuilder);

        // Act
        var method = interfaceType.GetMethod("AddDbContext");

        // Assert
        method.Should().NotBeNull();
    }

    [Fact]
    public void AddDbContext_GenericConstraint_RequiresRCommonDbContext()
    {
        // Arrange
        var interfaceType = typeof(IEFCorePersistenceBuilder);
        var method = interfaceType.GetMethod("AddDbContext");

        // Act
        var genericConstraints = method!.GetGenericArguments()[0].GetGenericParameterConstraints();

        // Assert
        genericConstraints.Should().Contain(typeof(RCommonDbContext));
    }

    [Fact]
    public void EFCorePerisistenceBuilder_ImplementsIEFCorePersistenceBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new EFCorePerisistenceBuilder(services);

        // Assert
        builder.Should().BeAssignableTo<IEFCorePersistenceBuilder>();
    }

    [Fact]
    public void AddDbContext_ReturnsIEFCorePersistenceBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        IEFCorePersistenceBuilder builder = new EFCorePerisistenceBuilder(services);

        // Act
        var result = builder.AddDbContext<TestDbContext>("TestStore", options =>
            options.UseInMemoryDatabase("TestStore"));

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IEFCorePersistenceBuilder>();
    }

    [Fact]
    public void AddDbContext_AllowsMethodChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        IEFCorePersistenceBuilder builder = new EFCorePerisistenceBuilder(services);

        // Act
        var result = builder
            .AddDbContext<TestDbContext>("TestStore1", options =>
                options.UseInMemoryDatabase("TestStore1"))
            .AddDbContext<SecondTestDbContext>("TestStore2", options =>
                options.UseInMemoryDatabase("TestStore2"));

        // Assert
        result.Should().NotBeNull();
        services.Should().Contain(sd => sd.ServiceType == typeof(TestDbContext));
        services.Should().Contain(sd => sd.ServiceType == typeof(SecondTestDbContext));
    }

    [Fact]
    public void Services_Property_IsAccessibleFromInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        IEFCorePersistenceBuilder builder = new EFCorePerisistenceBuilder(services);

        // Act
        var result = builder.Services;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void SetDefaultDataStore_IsAccessibleFromInterface()
    {
        // Arrange
        var services = new ServiceCollection();
        IEFCorePersistenceBuilder builder = new EFCorePerisistenceBuilder(services);

        // Act
        var result = builder.SetDefaultDataStore(options =>
            options.DefaultDataStoreName = "TestDataStore");

        // Assert
        result.Should().NotBeNull();
    }
}
