using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RCommon.StateMachines;

namespace RCommon.Stateless;

/// <summary>
/// Wraps a <see cref="Stateless.StateMachine{TState, TTrigger}"/> to implement
/// the RCommon <see cref="IStateMachine{TState, TTrigger}"/> abstraction.
/// </summary>
public class StatelessStateMachine<TState, TTrigger> : IStateMachine<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    private readonly global::Stateless.StateMachine<TState, TTrigger> _machine;
    private readonly Dictionary<(TTrigger, Type), object> _triggerParameterCache = new();

    /// <summary>
    /// Initializes a new instance of <see cref="StatelessStateMachine{TState, TTrigger}"/>
    /// wrapping the specified Stateless machine.
    /// </summary>
    /// <param name="machine">The underlying Stateless state machine instance.</param>
    internal StatelessStateMachine(global::Stateless.StateMachine<TState, TTrigger> machine)
    {
        _machine = machine ?? throw new ArgumentNullException(nameof(machine));
    }

    /// <inheritdoc />
    public TState CurrentState => _machine.State;

    /// <inheritdoc />
    public bool CanFire(TTrigger trigger) => _machine.CanFire(trigger);

    /// <inheritdoc />
#pragma warning disable CS0618 // Stateless recommends PermittedTriggersAsync, but the interface requires a synchronous property
    public IEnumerable<TTrigger> PermittedTriggers => _machine.PermittedTriggers;
#pragma warning restore CS0618

    /// <inheritdoc />
    public async Task FireAsync(TTrigger trigger, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _machine.FireAsync(trigger).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="Stateless.StateMachine{TState, TTrigger}.SetTriggerParameters{TArg0}"/>
    /// to register the parameterized trigger. The descriptor is cached by trigger and data type
    /// to prevent double-registration, which Stateless does not allow.
    /// </remarks>
    public async Task FireAsync<TData>(TTrigger trigger, TData data, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = (trigger, typeof(TData));
        if (!_triggerParameterCache.TryGetValue(key, out var cached))
        {
            cached = _machine.SetTriggerParameters<TData>(trigger);
            _triggerParameterCache[key] = cached;
        }

        var descriptor = (global::Stateless.StateMachine<TState, TTrigger>.TriggerWithParameters<TData>)cached;
        await _machine.FireAsync(descriptor, data).ConfigureAwait(false);
    }
}
