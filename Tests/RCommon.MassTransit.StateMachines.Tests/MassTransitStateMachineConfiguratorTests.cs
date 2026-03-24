using System.Threading.Tasks;
using FluentAssertions;
using RCommon.MassTransit.StateMachines;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.MassTransit.StateMachines.Tests;

public class MassTransitStateMachineConfiguratorTests
{
    [Fact]
    public void ForState_ReturnsIStateConfigurator()
    {
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        var stateConfig = configurator.ForState(PaymentState.Pending);
        stateConfig.Should().BeAssignableTo<IStateConfigurator<PaymentState, PaymentTrigger>>();
    }

    [Fact]
    public void Build_ReturnsIStateMachine()
    {
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        configurator.ForState(PaymentState.Pending)
            .Permit(PaymentTrigger.Authorize, PaymentState.Authorized);
        var machine = configurator.Build(PaymentState.Pending);
        machine.Should().BeAssignableTo<IStateMachine<PaymentState, PaymentTrigger>>();
    }

    [Fact]
    public void ForState_CalledTwice_ReturnsSameConfig()
    {
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        var config1 = configurator.ForState(PaymentState.Pending);
        var config2 = configurator.ForState(PaymentState.Pending);
        config1.Should().BeSameAs(config2);
    }

    [Fact]
    public async Task MachinesFromSameConfigurator_AreIndependent()
    {
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        configurator.ForState(PaymentState.Pending)
            .Permit(PaymentTrigger.Authorize, PaymentState.Authorized);
        var machine1 = configurator.Build(PaymentState.Pending);
        var machine2 = configurator.Build(PaymentState.Pending);

        await machine1.FireAsync(PaymentTrigger.Authorize);

        machine1.CurrentState.Should().Be(PaymentState.Authorized);
        machine2.CurrentState.Should().Be(PaymentState.Pending);
    }
}
