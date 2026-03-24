using FluentAssertions;
using RCommon.Stateless;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.Stateless.Tests;

public class StatelessConfiguratorTests
{
    [Fact]
    public void ForState_ReturnsIStateConfigurator()
    {
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();

        var result = configurator.ForState(OrderState.Pending);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IStateConfigurator<OrderState, OrderTrigger>>();
    }

    [Fact]
    public void Build_ReturnsIStateMachine()
    {
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();
        configurator.ForState(OrderState.Pending)
            .Permit(OrderTrigger.Approve, OrderState.Approved);

        var result = configurator.Build(OrderState.Pending);

        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IStateMachine<OrderState, OrderTrigger>>();
    }

    [Fact]
    public void FluentChaining_Works()
    {
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();

        var act = () => configurator.ForState(OrderState.Pending)
            .Permit(OrderTrigger.Approve, OrderState.Approved)
            .OnEntry(ct => Task.CompletedTask)
            .OnExit(ct => Task.CompletedTask);

        act.Should().NotThrow();
    }

    [Fact]
    public async Task MultipleForStateCalls_ConfigureDifferentStates()
    {
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();
        configurator.ForState(OrderState.Pending)
            .Permit(OrderTrigger.Approve, OrderState.Approved);
        configurator.ForState(OrderState.Approved)
            .Permit(OrderTrigger.Ship, OrderState.Shipped);

        var machine = configurator.Build(OrderState.Pending);

        await machine.FireAsync(OrderTrigger.Approve);
        machine.CurrentState.Should().Be(OrderState.Approved);

        await machine.FireAsync(OrderTrigger.Ship);
        machine.CurrentState.Should().Be(OrderState.Shipped);
    }
}
