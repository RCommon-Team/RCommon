using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RCommon.StateMachines;

namespace RCommon.MassTransit.StateMachines;

/// <summary>
/// Stores per-state configuration for a MassTransit-based state machine, including
/// unconditional transitions, guarded transitions, and entry/exit actions.
/// </summary>
/// <typeparam name="TState">The enum type representing states.</typeparam>
/// <typeparam name="TTrigger">The enum type representing triggers.</typeparam>
public class MassTransitStateConfigurator<TState, TTrigger> : IStateConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    internal TState State { get; }
    internal Dictionary<TTrigger, TState> Transitions { get; } = new();
    internal Dictionary<TTrigger, List<(Func<bool> Guard, TState Destination)>> GuardedTransitions { get; } = new();
    internal List<Func<CancellationToken, Task>> EntryActions { get; } = new();
    internal List<Func<CancellationToken, Task>> ExitActions { get; } = new();

    public MassTransitStateConfigurator(TState state)
    {
        State = state;
    }

    /// <inheritdoc />
    public IStateConfigurator<TState, TTrigger> Permit(TTrigger trigger, TState destinationState)
    {
        Transitions[trigger] = destinationState;
        return this;
    }

    /// <inheritdoc />
    public IStateConfigurator<TState, TTrigger> PermitIf(TTrigger trigger, TState destinationState, Func<bool> guard)
    {
        if (!GuardedTransitions.TryGetValue(trigger, out var guards))
        {
            guards = new List<(Func<bool> Guard, TState Destination)>();
            GuardedTransitions[trigger] = guards;
        }
        guards.Add((guard, destinationState));
        return this;
    }

    /// <inheritdoc />
    public IStateConfigurator<TState, TTrigger> OnEntry(Func<CancellationToken, Task> action)
    {
        EntryActions.Add(action);
        return this;
    }

    /// <inheritdoc />
    public IStateConfigurator<TState, TTrigger> OnExit(Func<CancellationToken, Task> action)
    {
        ExitActions.Add(action);
        return this;
    }
}
