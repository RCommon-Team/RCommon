using System;
using System.Collections.Generic;
using RCommon.StateMachines;

namespace RCommon.Stateless;

/// <summary>
/// Implements <see cref="IStateMachineConfigurator{TState, TTrigger}"/> using the Stateless library.
/// </summary>
/// <remarks>
/// Configuration is deferred: <see cref="ForState"/> records actions that are replayed each time
/// <see cref="Build"/> is called, allowing multiple independent machines to be created from
/// the same configurator instance.
/// </remarks>
public class StatelessConfigurator<TState, TTrigger> : IStateMachineConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly List<Action<global::Stateless.StateMachine<TState, TTrigger>>> _configActions = new();

    /// <inheritdoc />
    /// <remarks>
    /// Creates a <see cref="DeferredStateConfigurator{TState, TTrigger}"/> that records all
    /// configuration calls. The deferred actions are replayed when <see cref="Build"/> is invoked.
    /// </remarks>
    public IStateConfigurator<TState, TTrigger> ForState(TState state)
    {
        var deferred = new DeferredStateConfigurator<TState, TTrigger>();
        _configActions.Add(machine =>
        {
            var stateConfig = machine.Configure(state);
            deferred.ApplyTo(stateConfig);
        });
        return deferred;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a new <see cref="Stateless.StateMachine{TState, TTrigger}"/> with the given
    /// <paramref name="initialState"/>, replays all deferred configuration actions, and returns
    /// it wrapped in a <see cref="StatelessStateMachine{TState, TTrigger}"/>.
    /// Each call produces a fully independent machine instance.
    /// </remarks>
    public IStateMachine<TState, TTrigger> Build(TState initialState)
    {
        var machine = new global::Stateless.StateMachine<TState, TTrigger>(initialState);

        foreach (var configAction in _configActions)
        {
            configAction(machine);
        }

        return new StatelessStateMachine<TState, TTrigger>(machine);
    }
}
