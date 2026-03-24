using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using RCommon.MassTransit.StateMachines;
using RCommon.StateMachines;
using Xunit;

namespace RCommon.MassTransit.StateMachines.Tests;

public enum PaymentState { Pending, Authorized, Captured, Refunded, Failed }
public enum PaymentTrigger { Authorize, Capture, Refund, Fail }

public class MassTransitStateMachineTests
{
    private static MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger> CreateConfigurator()
    {
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        configurator.ForState(PaymentState.Pending)
            .Permit(PaymentTrigger.Authorize, PaymentState.Authorized)
            .Permit(PaymentTrigger.Fail, PaymentState.Failed);
        configurator.ForState(PaymentState.Authorized)
            .Permit(PaymentTrigger.Capture, PaymentState.Captured);
        configurator.ForState(PaymentState.Captured)
            .Permit(PaymentTrigger.Refund, PaymentState.Refunded);
        return configurator;
    }

    [Fact]
    public void Build_ReturnsCorrectInitialState()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        machine.CurrentState.Should().Be(PaymentState.Pending);
    }

    [Fact]
    public async Task FireAsync_TransitionsCorrectly()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        await machine.FireAsync(PaymentTrigger.Authorize);
        machine.CurrentState.Should().Be(PaymentState.Authorized);
    }

    [Fact]
    public void CanFire_ReturnsTrue_ForPermittedTrigger()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        machine.CanFire(PaymentTrigger.Authorize).Should().BeTrue();
    }

    [Fact]
    public void CanFire_ReturnsFalse_ForUnpermittedTrigger()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        machine.CanFire(PaymentTrigger.Capture).Should().BeFalse();
    }

    [Fact]
    public async Task FireAsync_ThrowsForUnpermittedTrigger()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        Func<Task> act = () => machine.FireAsync(PaymentTrigger.Capture);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void PermittedTriggers_ReturnsCorrectSet()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        var triggers = machine.PermittedTriggers.ToList();
        triggers.Should().Contain(PaymentTrigger.Authorize);
        triggers.Should().Contain(PaymentTrigger.Fail);
        triggers.Should().HaveCount(2);
    }

    [Fact]
    public async Task PermitIf_AllowsTransitionWhenGuardTrue()
    {
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        configurator.ForState(PaymentState.Pending)
            .PermitIf(PaymentTrigger.Authorize, PaymentState.Authorized, () => true);
        var machine = configurator.Build(PaymentState.Pending);
        await machine.FireAsync(PaymentTrigger.Authorize);
        machine.CurrentState.Should().Be(PaymentState.Authorized);
    }

    [Fact]
    public async Task PermitIf_BlocksTransitionWhenGuardFalse()
    {
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        configurator.ForState(PaymentState.Pending)
            .PermitIf(PaymentTrigger.Authorize, PaymentState.Authorized, () => false);
        var machine = configurator.Build(PaymentState.Pending);
        Func<Task> act = () => machine.FireAsync(PaymentTrigger.Authorize);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task OnEntry_ExecutesDuringTransition_WithCancellationToken()
    {
        CancellationToken capturedToken = default;
        var cts = new CancellationTokenSource();
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        configurator.ForState(PaymentState.Pending)
            .Permit(PaymentTrigger.Authorize, PaymentState.Authorized);
        configurator.ForState(PaymentState.Authorized)
            .OnEntry(ct =>
            {
                capturedToken = ct;
                return Task.CompletedTask;
            });
        var machine = configurator.Build(PaymentState.Pending);
        await machine.FireAsync(PaymentTrigger.Authorize, cts.Token);
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public async Task OnExit_ExecutesDuringTransition_WithCancellationToken()
    {
        CancellationToken capturedToken = default;
        var cts = new CancellationTokenSource();
        var configurator = new MassTransitStateMachineConfigurator<PaymentState, PaymentTrigger>();
        configurator.ForState(PaymentState.Pending)
            .Permit(PaymentTrigger.Authorize, PaymentState.Authorized)
            .OnExit(ct =>
            {
                capturedToken = ct;
                return Task.CompletedTask;
            });
        var machine = configurator.Build(PaymentState.Pending);
        await machine.FireAsync(PaymentTrigger.Authorize, cts.Token);
        capturedToken.Should().Be(cts.Token);
    }

    [Fact]
    public void Build_CalledMultipleTimes_ProducesIndependentMachines()
    {
        var configurator = CreateConfigurator();
        var machine1 = configurator.Build(PaymentState.Pending);
        var machine2 = configurator.Build(PaymentState.Authorized);
        machine1.CurrentState.Should().Be(PaymentState.Pending);
        machine2.CurrentState.Should().Be(PaymentState.Authorized);
    }

    [Fact]
    public async Task MultipleTransitions_InSequence()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);

        await machine.FireAsync(PaymentTrigger.Authorize);
        machine.CurrentState.Should().Be(PaymentState.Authorized);

        await machine.FireAsync(PaymentTrigger.Capture);
        machine.CurrentState.Should().Be(PaymentState.Captured);

        await machine.FireAsync(PaymentTrigger.Refund);
        machine.CurrentState.Should().Be(PaymentState.Refunded);
    }

    [Fact]
    public async Task CancellationToken_ThrowsWhenCancelled()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        Func<Task> act = () => machine.FireAsync(PaymentTrigger.Authorize, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task FireAsyncWithData_DelegatesToFireAsync()
    {
        var configurator = CreateConfigurator();
        var machine = configurator.Build(PaymentState.Pending);
        await machine.FireAsync(PaymentTrigger.Authorize, new { Amount = 100.0m });
        machine.CurrentState.Should().Be(PaymentState.Authorized);
    }
}
