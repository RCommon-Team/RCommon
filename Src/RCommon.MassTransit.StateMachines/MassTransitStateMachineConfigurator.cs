using System;
using System.Collections.Generic;
using RCommon.StateMachines;

namespace RCommon.MassTransit.StateMachines;

/// <summary>
/// Configures state machine transitions, guards, and actions, then builds independent
/// <see cref="IStateMachine{TState, TTrigger}"/> instances. Configuration is performed once
/// via <see cref="ForState"/> calls, while <see cref="Build"/> can be called many times
/// with different initial states to produce independent machine instances that share the
/// same transition configuration.
/// </summary>
/// <typeparam name="TState">The enum type representing states.</typeparam>
/// <typeparam name="TTrigger">The enum type representing triggers.</typeparam>
public class MassTransitStateMachineConfigurator<TState, TTrigger> : IStateMachineConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly Dictionary<TState, MassTransitStateConfigurator<TState, TTrigger>> _stateConfigs = new();

    /// <inheritdoc />
    public IStateConfigurator<TState, TTrigger> ForState(TState state)
    {
        if (!_stateConfigs.TryGetValue(state, out var config))
        {
            config = new MassTransitStateConfigurator<TState, TTrigger>(state);
            _stateConfigs[state] = config;
        }
        return config;
    }

    /// <inheritdoc />
    public IStateMachine<TState, TTrigger> Build(TState initialState)
    {
        return new MassTransitStateMachine<TState, TTrigger>(initialState, _stateConfigs);
    }
}
