using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RCommon.StateMachines;

namespace RCommon.MassTransit.StateMachines;

/// <summary>
/// A lightweight dictionary-based finite state machine implementing <see cref="IStateMachine{TState, TTrigger}"/>.
/// Each instance is independent and tracks its own current state.
/// </summary>
/// <typeparam name="TState">The enum type representing states.</typeparam>
/// <typeparam name="TTrigger">The enum type representing triggers.</typeparam>
public class MassTransitStateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly Dictionary<TState, MassTransitStateConfigurator<TState, TTrigger>> _stateConfigs;
    private TState _currentState;

    public MassTransitStateMachine(
        TState initialState,
        Dictionary<TState, MassTransitStateConfigurator<TState, TTrigger>> stateConfigs)
    {
        _currentState = initialState;
        _stateConfigs = stateConfigs ?? throw new ArgumentNullException(nameof(stateConfigs));
    }

    /// <inheritdoc />
    public TState CurrentState => _currentState;

    /// <inheritdoc />
    public bool CanFire(TTrigger trigger)
    {
        if (!_stateConfigs.TryGetValue(_currentState, out var config))
        {
            return false;
        }

        if (config.Transitions.ContainsKey(trigger))
        {
            return true;
        }

        if (config.GuardedTransitions.TryGetValue(trigger, out var guards))
        {
            return guards.Any(g => g.Guard());
        }

        return false;
    }

    /// <inheritdoc />
    public IEnumerable<TTrigger> PermittedTriggers
    {
        get
        {
            if (!_stateConfigs.TryGetValue(_currentState, out var config))
            {
                return Enumerable.Empty<TTrigger>();
            }

            var unconditional = config.Transitions.Keys;

            var guarded = config.GuardedTransitions
                .Where(kvp => kvp.Value.Any(g => g.Guard()))
                .Select(kvp => kvp.Key);

            return unconditional.Union(guarded);
        }
    }

    /// <inheritdoc />
    public async Task FireAsync(TTrigger trigger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_stateConfigs.TryGetValue(_currentState, out var config))
        {
            throw new InvalidOperationException(
                $"No configuration found for state '{_currentState}'.");
        }

        // Resolve destination: check unconditional first, then first passing guard.
        TState destination;
        if (config.Transitions.TryGetValue(trigger, out var unconditionalDest))
        {
            destination = unconditionalDest;
        }
        else if (config.GuardedTransitions.TryGetValue(trigger, out var guards))
        {
            var match = guards.FirstOrDefault(g => g.Guard());
            if (match.Guard is null)
            {
                throw new InvalidOperationException(
                    $"Trigger '{trigger}' has guarded transitions from state '{_currentState}', but no guard condition is satisfied.");
            }
            destination = match.Destination;
        }
        else
        {
            throw new InvalidOperationException(
                $"No valid transition for trigger '{trigger}' from state '{_currentState}'.");
        }

        // Execute exit actions for current state.
        foreach (var exitAction in config.ExitActions)
        {
            await exitAction(cancellationToken).ConfigureAwait(false);
        }

        // Update state.
        _currentState = destination;

        // Execute entry actions for new state.
        if (_stateConfigs.TryGetValue(_currentState, out var newConfig))
        {
            foreach (var entryAction in newConfig.EntryActions)
            {
                await entryAction(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Fires a trigger with associated data. In this implementation the data parameter is ignored
    /// and the transition is handled identically to <see cref="FireAsync(TTrigger, CancellationToken)"/>.
    /// This is a documented limitation of the dictionary-based FSM adapter.
    /// </summary>
    public Task FireAsync<TData>(TTrigger trigger, TData data, CancellationToken cancellationToken = default)
    {
        return FireAsync(trigger, cancellationToken);
    }
}
