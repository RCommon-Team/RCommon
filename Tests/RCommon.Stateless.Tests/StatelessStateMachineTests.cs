using FluentAssertions;
using RCommon.Stateless;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.Stateless.Tests;

public enum OrderState { Pending, Approved, Shipped, Completed, Cancelled }
public enum OrderTrigger { Approve, Ship, Complete, Cancel }

public class StatelessStateMachineTests
{
    private static StatelessConfigurator<OrderState, OrderTrigger> CreateConfigurator()
    {
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();
        configurator.ForState(OrderState.Pending)
            .Permit(OrderTrigger.Approve, OrderState.Approved)
            .Permit(OrderTrigger.Cancel, OrderState.Cancelled);
        configurator.ForState(OrderState.Approved)
            .Permit(OrderTrigger.Ship, OrderState.Shipped);
        configurator.ForState(OrderState.Shipped)
            .Permit(OrderTrigger.Complete, OrderState.Completed);
        return configurator;
    }

    [Fact]
    public void Build_ReturnsCorrectInitialState()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);

        machine.CurrentState.Should().Be(OrderState.Pending);
    }

    [Fact]
    public async Task FireAsync_TransitionsCorrectly()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);

        await machine.FireAsync(OrderTrigger.Approve);

        machine.CurrentState.Should().Be(OrderState.Approved);
    }

    [Fact]
    public void CanFire_ReturnsTrue_ForPermittedTrigger()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);

        machine.CanFire(OrderTrigger.Approve).Should().BeTrue();
    }

    [Fact]
    public void CanFire_ReturnsFalse_ForUnpermittedTrigger()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);

        machine.CanFire(OrderTrigger.Ship).Should().BeFalse();
    }

    [Fact]
    public async Task FireAsync_ThrowsForUnpermittedTrigger()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);

        Func<Task> act = () => machine.FireAsync(OrderTrigger.Ship);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void PermittedTriggers_ReturnsCorrectSet()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);

        machine.PermittedTriggers.Should().Contain(OrderTrigger.Approve)
            .And.Contain(OrderTrigger.Cancel);
    }

    [Fact]
    public async Task PermitIf_AllowsTransitionWhenGuardTrue()
    {
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();
        configurator.ForState(OrderState.Pending)
            .PermitIf(OrderTrigger.Approve, OrderState.Approved, () => true);

        var machine = configurator.Build(OrderState.Pending);
        await machine.FireAsync(OrderTrigger.Approve);

        machine.CurrentState.Should().Be(OrderState.Approved);
    }

    [Fact]
    public void PermitIf_BlocksTransitionWhenGuardFalse()
    {
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();
        configurator.ForState(OrderState.Pending)
            .PermitIf(OrderTrigger.Approve, OrderState.Approved, () => false);

        var machine = configurator.Build(OrderState.Pending);

        machine.CanFire(OrderTrigger.Approve).Should().BeFalse();
    }

    [Fact]
    public async Task OnEntry_ExecutesDuringTransition()
    {
        var entryExecuted = false;
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();
        configurator.ForState(OrderState.Pending)
            .Permit(OrderTrigger.Approve, OrderState.Approved);
        configurator.ForState(OrderState.Approved)
            .OnEntry(ct =>
            {
                entryExecuted = true;
                return Task.CompletedTask;
            });

        var machine = configurator.Build(OrderState.Pending);
        await machine.FireAsync(OrderTrigger.Approve);

        entryExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task OnExit_ExecutesDuringTransition()
    {
        var exitExecuted = false;
        var configurator = new StatelessConfigurator<OrderState, OrderTrigger>();
        configurator.ForState(OrderState.Pending)
            .Permit(OrderTrigger.Approve, OrderState.Approved)
            .OnExit(ct =>
            {
                exitExecuted = true;
                return Task.CompletedTask;
            });

        var machine = configurator.Build(OrderState.Pending);
        await machine.FireAsync(OrderTrigger.Approve);

        exitExecuted.Should().BeTrue();
    }

    [Fact]
    public void Build_CalledMultipleTimes_ProducesIndependentMachines()
    {
        var configurator = CreateConfigurator();

        var machine1 = configurator.Build(OrderState.Pending);
        var machine2 = configurator.Build(OrderState.Approved);

        machine1.CurrentState.Should().Be(OrderState.Pending);
        machine2.CurrentState.Should().Be(OrderState.Approved);
    }

    [Fact]
    public async Task MultipleTransitions_InSequence()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);

        await machine.FireAsync(OrderTrigger.Approve);
        machine.CurrentState.Should().Be(OrderState.Approved);

        await machine.FireAsync(OrderTrigger.Ship);
        machine.CurrentState.Should().Be(OrderState.Shipped);

        await machine.FireAsync(OrderTrigger.Complete);
        machine.CurrentState.Should().Be(OrderState.Completed);
    }

    [Fact]
    public async Task CancellationToken_ThrowsWhenCancelled()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(OrderState.Pending);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => machine.FireAsync(OrderTrigger.Approve, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
