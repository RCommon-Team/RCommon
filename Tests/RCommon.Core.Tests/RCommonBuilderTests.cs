using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.EventHandling;
using RCommon.EventHandling.Producers;
using Xunit;

namespace RCommon.Core.Tests;

public class RCommonBuilderTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidServiceCollection_InitializesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new RCommonBuilder(services);

        // Assert
        builder.Services.Should().BeSameAs(services);
    }

    [Fact]
    public void Constructor_WithNullServiceCollection_ThrowsNullReferenceException()
    {
        // Arrange & Act
        var act = () => new RCommonBuilder(null!);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void Constructor_RegistersEventBusAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new RCommonBuilder(services);
        var provider = services.BuildServiceProvider();

        // Assert
        var eventBus1 = provider.GetService<IEventBus>();
        var eventBus2 = provider.GetService<IEventBus>();
        eventBus1.Should().NotBeNull();
        eventBus1.Should().BeSameAs(eventBus2);
    }

    [Fact]
    public void Constructor_RegistersEventRouterAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var builder = new RCommonBuilder(services);
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var router1 = scope1.ServiceProvider.GetService<IEventRouter>();
        var router2 = scope2.ServiceProvider.GetService<IEventRouter>();
        router1.Should().NotBeNull();
        router1.Should().NotBeSameAs(router2);
    }

    [Fact]
    public void Constructor_ConfiguresCachingOptionsWithDisabledCaching()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new RCommonBuilder(services);
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<CachingOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.CachingEnabled.Should().BeFalse();
    }

    #endregion

    #region WithSequentialGuidGenerator Tests

    [Fact]
    public void WithSequentialGuidGenerator_RegistersSequentialGuidGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithSequentialGuidGenerator(options =>
        {
            options.DefaultSequentialGuidType = SequentialGuidType.SequentialAtEnd;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var guidGenerator = provider.GetService<IGuidGenerator>();
        guidGenerator.Should().NotBeNull();
        guidGenerator.Should().BeOfType<SequentialGuidGenerator>();
    }

    [Fact]
    public void WithSequentialGuidGenerator_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithSequentialGuidGenerator(options =>
        {
            options.DefaultSequentialGuidType = SequentialGuidType.SequentialAsString;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<SequentialGuidGeneratorOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.DefaultSequentialGuidType.Should().Be(SequentialGuidType.SequentialAsString);
    }

    [Fact]
    public void WithSequentialGuidGenerator_CalledTwice_ThrowsRCommonBuilderException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);
        builder.WithSequentialGuidGenerator(options => { });

        // Act
        var act = () => builder.WithSequentialGuidGenerator(options => { });

        // Assert
        act.Should().Throw<RCommonBuilderException>();
    }

    [Fact]
    public void WithSequentialGuidGenerator_ReturnsBuilder_ForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        var result = builder.WithSequentialGuidGenerator(options => { });

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion

    #region WithSimpleGuidGenerator Tests

    [Fact]
    public void WithSimpleGuidGenerator_RegistersSimpleGuidGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithSimpleGuidGenerator();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var guidGenerator = scope.ServiceProvider.GetService<IGuidGenerator>();
        guidGenerator.Should().NotBeNull();
        guidGenerator.Should().BeOfType<SimpleGuidGenerator>();
    }

    [Fact]
    public void WithSimpleGuidGenerator_CalledTwice_ThrowsRCommonBuilderException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);
        builder.WithSimpleGuidGenerator();

        // Act
        var act = () => builder.WithSimpleGuidGenerator();

        // Assert
        act.Should().Throw<RCommonBuilderException>();
    }

    [Fact]
    public void WithSimpleGuidGenerator_AfterSequentialGuidGenerator_ThrowsRCommonBuilderException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);
        builder.WithSequentialGuidGenerator(options => { });

        // Act
        var act = () => builder.WithSimpleGuidGenerator();

        // Assert
        act.Should().Throw<RCommonBuilderException>();
    }

    [Fact]
    public void WithSimpleGuidGenerator_ReturnsBuilder_ForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        var result = builder.WithSimpleGuidGenerator();

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion

    #region WithDateTimeSystem Tests

    [Fact]
    public void WithDateTimeSystem_RegistersSystemTime()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithDateTimeSystem(options =>
        {
            options.Kind = DateTimeKind.Utc;
        });
        var provider = services.BuildServiceProvider();

        // Assert
        var systemTime = provider.GetService<ISystemTime>();
        systemTime.Should().NotBeNull();
        systemTime.Should().BeOfType<SystemTime>();
    }

    [Fact]
    public void WithDateTimeSystem_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithDateTimeSystem(options =>
        {
            options.Kind = DateTimeKind.Utc;
        });
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptions<SystemTimeOptions>>();

        // Assert
        options.Should().NotBeNull();
        options!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void WithDateTimeSystem_CalledTwice_ThrowsRCommonBuilderException()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);
        builder.WithDateTimeSystem(options => { });

        // Act
        var act = () => builder.WithDateTimeSystem(options => { });

        // Assert
        act.Should().Throw<RCommonBuilderException>();
    }

    [Fact]
    public void WithDateTimeSystem_ReturnsBuilder_ForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        var result = builder.WithDateTimeSystem(options => { });

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion

    #region WithCommonFactory Tests

    [Fact]
    public void WithCommonFactory_RegistersFactoryCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithCommonFactory<ITestService, TestService>();
        var provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetService<ICommonFactory<ITestService>>();
        factory.Should().NotBeNull();
    }

    [Fact]
    public void WithCommonFactory_CanCreateService()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        builder.WithCommonFactory<ITestService, TestService>();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetService<ICommonFactory<ITestService>>();

        // Assert
        var service = factory!.Create();
        service.Should().NotBeNull();
        service.Should().BeOfType<TestService>();
    }

    [Fact]
    public void WithCommonFactory_ReturnsBuilder_ForFluentChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        var result = builder.WithCommonFactory<ITestService, TestService>();

        // Assert
        result.Should().BeSameAs(builder);
    }

    #endregion

    #region Configure Tests

    [Fact]
    public void Configure_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        var result = builder.Configure();

        // Assert
        result.Should().BeSameAs(services);
    }

    #endregion

    #region Fluent Chaining Tests

    [Fact]
    public void FluentChaining_MultipleConfigurations_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new RCommonBuilder(services);

        // Act
        var result = builder
            .WithSequentialGuidGenerator(options =>
            {
                options.DefaultSequentialGuidType = SequentialGuidType.SequentialAtEnd;
            })
            .WithDateTimeSystem(options =>
            {
                options.Kind = DateTimeKind.Utc;
            })
            .WithCommonFactory<ITestService, TestService>()
            .Configure();

        // Assert
        result.Should().BeSameAs(services);
        var provider = services.BuildServiceProvider();
        provider.GetService<IGuidGenerator>().Should().NotBeNull();
        provider.GetService<ISystemTime>().Should().NotBeNull();
    }

    #endregion

    #region Test Helper Classes

    public interface ITestService
    {
        string GetValue();
    }

    public class TestService : ITestService
    {
        public string GetValue() => "TestValue";
    }

    #endregion
}
