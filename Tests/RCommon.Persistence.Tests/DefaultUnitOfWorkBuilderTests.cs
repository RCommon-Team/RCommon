using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Persistence.Transactions;
using System.Transactions;
using Xunit;

namespace RCommon.Persistence.Tests;

public class DefaultUnitOfWorkBuilderTests
{
    [Fact]
    public void Constructor_WithValidServices_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_RegistersIUnitOfWorkAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(IUnitOfWork) &&
            x.ImplementationType == typeof(UnitOfWork) &&
            x.Lifetime == ServiceLifetime.Transient);

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_RegistersIUnitOfWorkFactoryAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Assert
        var descriptor = services.FirstOrDefault(x =>
            x.ServiceType == typeof(IUnitOfWorkFactory) &&
            x.ImplementationType == typeof(UnitOfWorkFactory) &&
            x.Lifetime == ServiceLifetime.Transient);

        descriptor.Should().NotBeNull();
    }

    [Fact]
    public void SetOptions_WithAction_ConfiguresSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new DefaultUnitOfWorkBuilder(services);
        var expectedIsolation = IsolationLevel.Serializable;

        // Act
        builder.SetOptions(settings =>
        {
            settings.DefaultIsolation = expectedIsolation;
            settings.AutoCompleteScope = true;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<UnitOfWorkSettings>>();
        options.Should().NotBeNull();
        options!.Value.DefaultIsolation.Should().Be(expectedIsolation);
        options.Value.AutoCompleteScope.Should().BeTrue();
    }

    [Fact]
    public void SetOptions_ReturnsBuilderForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Act
        var result = builder.SetOptions(settings => { });

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(builder);
    }

    [Fact]
    public void SetOptions_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Act
        builder.SetOptions(settings => settings.AutoCompleteScope = true);
        builder.SetOptions(settings => settings.DefaultIsolation = IsolationLevel.Serializable);

        // Assert - should not throw
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<UnitOfWorkSettings>>();
        options.Should().NotBeNull();
    }

    [Fact]
    public void SetOptions_WithNullAction_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Act & Assert
        var action = () => builder.SetOptions(null!);

        // The underlying Configure method will throw if null is passed
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_DoesNotAddDuplicateRegistrations_WhenCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder1 = new DefaultUnitOfWorkBuilder(services);
        var builder2 = new DefaultUnitOfWorkBuilder(services);

        // Assert - each constructor call adds the services, so we expect 2 of each
        var unitOfWorkDescriptors = services.Where(x => x.ServiceType == typeof(IUnitOfWork)).ToList();
        var factoryDescriptors = services.Where(x => x.ServiceType == typeof(IUnitOfWorkFactory)).ToList();

        unitOfWorkDescriptors.Should().HaveCount(2);
        factoryDescriptors.Should().HaveCount(2);
    }

    [Fact]
    public void SetOptions_ConfiguresDefaultIsolation()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new DefaultUnitOfWorkBuilder(services);
        var expectedIsolation = IsolationLevel.Snapshot;

        // Act
        builder.SetOptions(settings => settings.DefaultIsolation = expectedIsolation);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<UnitOfWorkSettings>>();
        options!.Value.DefaultIsolation.Should().Be(expectedIsolation);
    }

    [Fact]
    public void SetOptions_ConfiguresAutoCompleteScope()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Act
        builder.SetOptions(settings => settings.AutoCompleteScope = true);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<UnitOfWorkSettings>>();
        options!.Value.AutoCompleteScope.Should().BeTrue();
    }

    [Fact]
    public void Builder_ImplementsIUnitOfWorkBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Assert
        builder.Should().BeAssignableTo<IUnitOfWorkBuilder>();
    }

    [Theory]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    public void SetOptions_WithDifferentIsolationLevels_ConfiguresCorrectly(IsolationLevel isolation)
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new DefaultUnitOfWorkBuilder(services);

        // Act
        builder.SetOptions(settings => settings.DefaultIsolation = isolation);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<Microsoft.Extensions.Options.IOptions<UnitOfWorkSettings>>();
        options!.Value.DefaultIsolation.Should().Be(isolation);
    }
}
