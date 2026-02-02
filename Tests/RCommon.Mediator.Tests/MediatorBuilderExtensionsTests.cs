using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RCommon.Mediator;
using Xunit;

namespace RCommon.Mediator.Tests;

public class MediatorBuilderExtensionsTests
{
    #region WithMediator<T>() Tests

    [Fact]
    public void WithMediator_RegistersMediatorServiceInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var mockBuilder = CreateRCommonBuilder(services);

        // Act
        mockBuilder.WithMediator<TestMediatorBuilder>();

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMediatorService) &&
            sd.ImplementationType == typeof(MediatorService) &&
            sd.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void WithMediator_ReturnsRCommonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);

        // Act
        var result = builder.WithMediator<TestMediatorBuilder>();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithMediator_CreatesMediatorBuilderInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);
        TestMediatorBuilder.LastCreatedInstance = null;

        // Act
        builder.WithMediator<TestMediatorBuilder>();

        // Assert
        TestMediatorBuilder.LastCreatedInstance.Should().NotBeNull();
    }

    [Fact]
    public void WithMediator_PassesBuilderToMediatorBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);
        TestMediatorBuilder.LastCreatedInstance = null;

        // Act
        builder.WithMediator<TestMediatorBuilder>();

        // Assert
        TestMediatorBuilder.LastCreatedInstance!.RCommonBuilder.Should().BeSameAs(builder);
    }

    #endregion

    #region WithMediator<T>(Action<T>) Tests

    [Fact]
    public void WithMediator_WithAction_RegistersMediatorServiceInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);

        // Act
        builder.WithMediator<TestMediatorBuilder>(config => { });

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(IMediatorService) &&
            sd.ImplementationType == typeof(MediatorService) &&
            sd.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void WithMediator_WithAction_ReturnsRCommonBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);

        // Act
        var result = builder.WithMediator<TestMediatorBuilder>(config => { });

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithMediator_WithAction_InvokesConfigurationAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);
        var actionInvoked = false;

        // Act
        builder.WithMediator<TestMediatorBuilder>(config =>
        {
            actionInvoked = true;
        });

        // Assert
        actionInvoked.Should().BeTrue();
    }

    [Fact]
    public void WithMediator_WithAction_PassesMediatorBuilderToAction()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);
        TestMediatorBuilder? capturedConfig = null;

        // Act
        builder.WithMediator<TestMediatorBuilder>(config =>
        {
            capturedConfig = config;
        });

        // Assert
        capturedConfig.Should().NotBeNull();
        capturedConfig.Should().BeOfType<TestMediatorBuilder>();
    }

    [Fact]
    public void WithMediator_WithAction_MediatorBuilderHasAccessToServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);
        IServiceCollection? capturedServices = null;

        // Act
        builder.WithMediator<TestMediatorBuilder>(config =>
        {
            capturedServices = config.Services;
        });

        // Assert
        capturedServices.Should().BeSameAs(services);
    }

    [Fact]
    public void WithMediator_WithAction_AllowsServiceRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);

        // Act
        builder.WithMediator<TestMediatorBuilder>(config =>
        {
            config.Services.AddSingleton<ITestService, TestService>();
        });

        // Assert
        services.Should().Contain(sd =>
            sd.ServiceType == typeof(ITestService) &&
            sd.ImplementationType == typeof(TestService));
    }

    [Fact]
    public void WithMediator_WithAction_AllowsMultipleConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);
        var configurationSteps = new List<string>();

        // Act
        builder.WithMediator<TestMediatorBuilder>(config =>
        {
            configurationSteps.Add("Step1");
            configurationSteps.Add("Step2");
        });

        // Assert
        configurationSteps.Should().BeEquivalentTo(new[] { "Step1", "Step2" });
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public void WithMediator_CanBeChainedWithOtherBuilderMethods()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);

        // Act
        var result = builder
            .WithMediator<TestMediatorBuilder>()
            .WithSimpleGuidGenerator();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithMediator_MultipleCallsOverwriteRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);

        // Act
        builder.WithMediator<TestMediatorBuilder>();
        builder.WithMediator<TestMediatorBuilder>();

        // Assert
        var registrations = services.Where(sd => sd.ServiceType == typeof(IMediatorService)).ToList();
        registrations.Should().HaveCount(2);
    }

    #endregion

    #region MediatorBuilder Creation Tests

    [Fact]
    public void WithMediator_CreatesNewMediatorBuilderForEachCall()
    {
        // Arrange
        var services = new ServiceCollection();
        var builder = CreateRCommonBuilder(services);
        var instances = new List<TestMediatorBuilder>();

        // Act
        builder.WithMediator<TestMediatorBuilder>(config => instances.Add(config));
        builder.WithMediator<TestMediatorBuilder>(config => instances.Add(config));

        // Assert
        instances.Should().HaveCount(2);
        instances[0].Should().NotBeSameAs(instances[1]);
    }

    #endregion

    #region Test Helpers

    private static IRCommonBuilder CreateRCommonBuilder(IServiceCollection services)
    {
        return new TestRCommonBuilder(services);
    }

    public interface ITestService { }

    public class TestService : ITestService { }

    public class TestMediatorBuilder : IMediatorBuilder
    {
        public static TestMediatorBuilder? LastCreatedInstance { get; set; }
        public IRCommonBuilder RCommonBuilder { get; }
        public IServiceCollection Services => RCommonBuilder.Services;

        public TestMediatorBuilder(IRCommonBuilder builder)
        {
            RCommonBuilder = builder;
            LastCreatedInstance = this;
        }
    }

    public class TestRCommonBuilder : IRCommonBuilder
    {
        public IServiceCollection Services { get; }

        public TestRCommonBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Configure()
        {
            return Services;
        }

        public IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions)
        {
            return this;
        }

        public IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions)
        {
            return this;
        }

        public IRCommonBuilder WithSimpleGuidGenerator()
        {
            return this;
        }

        public IRCommonBuilder WithCommonFactory<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return this;
        }
    }

    #endregion
}
