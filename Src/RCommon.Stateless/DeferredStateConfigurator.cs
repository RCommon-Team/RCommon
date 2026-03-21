using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RCommon.StateMachines;

namespace RCommon.Stateless;

/// <summary>
/// Records <see cref="IStateConfigurator{TState, TTrigger}"/> calls as deferred actions
/// that are replayed against a real <see cref="Stateless.StateMachine{TState, TTrigger}.StateConfiguration"/>
/// when <see cref="StatelessConfigurator{TState, TTrigger}.Build"/> is called.
/// </summary>
/// <remarks>
/// This enables the consumer pattern where <c>ForState()</c> is called during configuration
/// (before any machine exists) and <c>Build()</c> is called later, potentially multiple times
/// with different initial states, each producing an independent machine.
/// </remarks>
internal class DeferredStateConfigurator<TState, TTrigger> : IStateConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly List<Action<global::Stateless.StateMachine<TState, TTrigger>.StateConfiguration>> _actions = new();

    /// <inheritdoc />
    public IStateConfigurator<TState, TTrigger> Permit(TTrigger trigger, TState destinationState)
    {
        _actions.Add(config => config.Permit(trigger, destinationState));
        return this;
    }

    /// <inheritdoc />
    public IStateConfigurator<TState, TTrigger> PermitIf(TTrigger trigger, TState destinationState, Func<bool> guard)
    {
        _actions.Add(config => config.PermitIf(trigger, destinationState, guard));
        return this;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stateless's <c>OnEntryAsync</c> does not accept a <see cref="CancellationToken"/>.
    /// The action is invoked with <see cref="CancellationToken.None"/>.
    /// </remarks>
    public IStateConfigurator<TState, TTrigger> OnEntry(Func<CancellationToken, Task> action)
    {
        _actions.Add(config => config.OnEntryAsync(() => action(CancellationToken.None)));
        return this;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Stateless's <c>OnExitAsync</c> does not accept a <see cref="CancellationToken"/>.
    /// The action is invoked with <see cref="CancellationToken.None"/>.
    /// </remarks>
    public IStateConfigurator<TState, TTrigger> OnExit(Func<CancellationToken, Task> action)
    {
        _actions.Add(config => config.OnExitAsync(() => action(CancellationToken.None)));
        return this;
    }

    /// <summary>
    /// Replays all recorded configuration actions against the specified state configuration.
    /// </summary>
    /// <param name="stateConfig">The Stateless state configuration to apply the deferred actions to.</param>
    internal void ApplyTo(global::Stateless.StateMachine<TState, TTrigger>.StateConfiguration stateConfig)
    {
        foreach (var action in _actions)
        {
            action(stateConfig);
        }
    }
}
