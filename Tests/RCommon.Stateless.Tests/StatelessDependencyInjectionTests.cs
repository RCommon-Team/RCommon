using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Stateless;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.Stateless.Tests;

public class StatelessDependencyInjectionTests
{
    private class TestRCommonBuilder : IRCommonBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();
        public IServiceCollection Configure() => Services;
        public IRCommonBuilder WithDateTimeSystem(Action<SystemTimeOptions> actions) => this;
        public IRCommonBuilder WithSequentialGuidGenerator(Action<SequentialGuidGeneratorOptions> actions) => this;
        public IRCommonBuilder WithSimpleGuidGenerator() => this;
        public IRCommonBuilder WithCommonFactory<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService => this;
    }

    [Fact]
    public void WithStatelessStateMachine_RegistersOpenGeneric()
    {
        var builder = new TestRCommonBuilder();
        builder.WithStatelessStateMachine();

        var provider = builder.Services.BuildServiceProvider();
        var configurator = provider.GetService<IStateMachineConfigurator<OrderState, OrderTrigger>>();

        configurator.Should().NotBeNull();
        configurator.Should().BeOfType<StatelessConfigurator<OrderState, OrderTrigger>>();
    }

    [Fact]
    public void EachResolution_ReturnsNewInstance()
    {
        var builder = new TestRCommonBuilder();
        builder.WithStatelessStateMachine();

        var provider = builder.Services.BuildServiceProvider();
        var first = provider.GetService<IStateMachineConfigurator<OrderState, OrderTrigger>>();
        var second = provider.GetService<IStateMachineConfigurator<OrderState, OrderTrigger>>();

        first.Should().NotBeSameAs(second);
    }
}
