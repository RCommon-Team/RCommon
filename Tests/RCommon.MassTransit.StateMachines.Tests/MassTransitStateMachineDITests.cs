using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.MassTransit.StateMachines;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.MassTransit.StateMachines.Tests;

public class MassTransitStateMachineDITests
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
    public void WithMassTransitStateMachine_RegistersOpenGeneric()
    {
        var builder = new TestRCommonBuilder();
        builder.WithMassTransitStateMachine();
        var provider = builder.Services.BuildServiceProvider();
        var configurator = provider.GetRequiredService<IStateMachineConfigurator<PaymentState, PaymentTrigger>>();
        configurator.Should().BeOfType<MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>>();
    }

    [Fact]
    public void EachResolution_ReturnsNewInstance()
    {
        var builder = new TestRCommonBuilder();
        builder.WithMassTransitStateMachine();
        var provider = builder.Services.BuildServiceProvider();
        var instance1 = provider.GetRequiredService<IStateMachineConfigurator<PaymentState, PaymentTrigger>>();
        var instance2 = provider.GetRequiredService<IStateMachineConfigurator<PaymentState, PaymentTrigger>>();
        instance1.Should().NotBeSameAs(instance2);
    }
}
