using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Mediator;
using Xunit;

namespace RCommon.Mediator.Tests;

public class IMediatorBuilderTests
{
    #region Interface Implementation Tests

    [Fact]
    public void IMediatorBuilder_CanBeImplemented()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = new TestMediatorBuilder(services);

        // Assert
        builder.Should().BeAssignableTo<IMediatorBuilder>();
    }

    [Fact]
    public void IMediatorBuilder_CanBeMocked()
    {
        // Arrange & Act
        var mockBuilder = new Mock<IMediatorBuilder>();

        // Assert
        mockBuilder.Object.Should().BeAssignableTo<IMediatorBuilder>();
    }

    #endregion

    #region Services Property Tests

    [Fact]
    public void Services_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);

        // Act
        var result = builder.Services;

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void Services_AllowsServiceRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);

        // Act
        builder.Services.AddSingleton<ITestService, TestService>();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ITestService) &&
            sd.ImplementationType == typeof(TestService));
    }

    [Fact]
    public void Services_AllowsMultipleServiceRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);

        // Act
        builder.Services.AddSingleton<ITestService, TestService>();
        builder.Services.AddScoped<IAnotherService, AnotherService>();
        builder.Services.AddTransient<IThirdService, ThirdService>();

        // Assert
        services.Should().HaveCount(3);
    }

    [Fact]
    public void Services_MockedBuilder_ReturnsConfiguredServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = new Mock<IMediatorBuilder>();
        mockBuilder.Setup(x => x.Services).Returns(services);

        // Act
        var result = mockBuilder.Object.Services;

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void Services_CanRegisterMediatorAdapter()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);

        // Act
        builder.Services.AddSingleton<IMediatorAdapter, TestMediatorAdapter>();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMediatorAdapter) &&
            sd.ImplementationType == typeof(TestMediatorAdapter));
    }

    [Fact]
    public void Services_CanRegisterWithFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);
        var testService = new TestService();

        // Act
        builder.Services.AddSingleton<ITestService>(sp => testService);

        // Assert
        var provider = services.BuildServiceProvider();
        var resolvedService = provider.GetRequiredService<ITestService>();
        resolvedService.Should().BeSameAs(testService);
    }

    [Fact]
    public void Services_CanRegisterWithDifferentLifetimes()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);

        // Act
        builder.Services.AddSingleton<ISingletonService, SingletonService>();
        builder.Services.AddScoped<IScopedService, ScopedService>();
        builder.Services.AddTransient<ITransientService, TransientService>();

        // Assert
        services.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Singleton);
        services.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.Lifetime == ServiceLifetime.Transient);
    }

    #endregion

    #region Complex Scenarios Tests

    [Fact]
    public void IMediatorBuilder_SupportsFluentConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new FluentMediatorBuilder(services);

        // Act
        builder
            .AddHandler<TestHandler>()
            .AddHandler<AnotherTestHandler>();

        // Assert
        services.Should().Contain(sd => sd.ImplementationType == typeof(TestHandler));
        services.Should().Contain(sd => sd.ImplementationType == typeof(AnotherTestHandler));
    }

    [Fact]
    public void IMediatorBuilder_CanBeUsedInConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);
        var configurationExecuted = false;

        // Act
        Action<IMediatorBuilder> configure = b =>
        {
            b.Services.AddSingleton<ITestService, TestService>();
            configurationExecuted = true;
        };

        configure(builder);

        // Assert
        configurationExecuted.Should().BeTrue();
        services.Should().Contain(sd => sd.ServiceType == typeof(ITestService));
    }

    [Fact]
    public void IMediatorBuilder_ServicesAreResolvedCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);
        builder.Services.AddSingleton<ITestService, TestService>();
        builder.Services.AddSingleton<IMediatorAdapter, TestMediatorAdapter>();

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        var testService = provider.GetRequiredService<ITestService>();
        var mediatorAdapter = provider.GetRequiredService<IMediatorAdapter>();

        testService.Should().BeOfType<TestService>();
        mediatorAdapter.Should().BeOfType<TestMediatorAdapter>();
    }

    [Fact]
    public void IMediatorBuilder_SupportsExtensionMethods()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = new TestMediatorBuilder(services);

        // Act
        builder.AddCustomService<ITestService, TestService>();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ITestService) &&
            sd.ImplementationType == typeof(TestService));
    }

    #endregion

    #region Test Helper Classes

    public interface ITestService { }
    public class TestService : ITestService { }

    public interface IAnotherService { }
    public class AnotherService : IAnotherService { }

    public interface IThirdService { }
    public class ThirdService : IThirdService { }

    public interface ISingletonService { }
    public class SingletonService : ISingletonService { }

    public interface IScopedService { }
    public class ScopedService : IScopedService { }

    public interface ITransientService { }
    public class TransientService : ITransientService { }

    public class TestHandler { }
    public class AnotherTestHandler { }

    public class TestMediatorBuilder : IMediatorBuilder
    {
        public IServiceCollection Services { get; }

        public TestMediatorBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }

    public class FluentMediatorBuilder : IMediatorBuilder
    {
        public IServiceCollection Services { get; }

        public FluentMediatorBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public FluentMediatorBuilder AddHandler<THandler>() where THandler : class
        {
            Services.AddTransient<THandler>();
            return this;
        }
    }

    public class TestMediatorAdapter : IMediatorAdapter
    {
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(default(TResponse)!);
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    #endregion
}

#region Extension Methods

public static class MediatorBuilderTestExtensions
{
    public static IMediatorBuilder AddCustomService<TService, TImplementation>(this IMediatorBuilder builder)
        where TService : class
        where TImplementation : class, TService
    {
        builder.Services.AddSingleton<TService, TImplementation>();
        return builder;
    }
}

#endregion
