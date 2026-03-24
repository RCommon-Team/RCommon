using System;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Models.Events;
using RCommon.StateMachines;

namespace RCommon.Persistence.Sagas;

public abstract class SagaOrchestrator<TState, TKey, TSagaState, TSagaTrigger>
    : ISaga<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
    where TSagaState : struct, Enum
    where TSagaTrigger : struct, Enum
{
    private readonly IStateMachineConfigurator<TSagaState, TSagaTrigger> _configurator;
    private IStateMachine<TSagaState, TSagaTrigger>? _stateMachineTemplate;

    protected ISagaStore<TState, TKey> Store { get; }

    protected SagaOrchestrator(
        ISagaStore<TState, TKey> store,
        IStateMachineConfigurator<TSagaState, TSagaTrigger> configurator)
    {
        Store = store;
        _configurator = configurator;
    }

    protected abstract void ConfigureStateMachine(
        IStateMachineConfigurator<TSagaState, TSagaTrigger> configurator);

    protected abstract TSagaTrigger MapEventToTrigger<TEvent>(TEvent @event)
        where TEvent : ISerializableEvent;

    protected abstract TSagaState InitialState { get; }

    private void EnsureConfigured()
    {
        if (_stateMachineTemplate == null)
        {
            ConfigureStateMachine(_configurator);
            _stateMachineTemplate = _configurator.Build(InitialState);
        }
    }

    public async Task HandleAsync<TEvent>(TEvent @event, TState state, CancellationToken ct = default)
        where TEvent : ISerializableEvent
    {
        EnsureConfigured();

        var currentState = string.IsNullOrEmpty(state.CurrentStep)
            ? InitialState
            : Enum.Parse<TSagaState>(state.CurrentStep);

        var machine = _configurator.Build(currentState);
        var trigger = MapEventToTrigger(@event);

        if (!machine.CanFire(trigger))
            return;

        await machine.FireAsync(trigger, ct).ConfigureAwait(false);
        state.CurrentStep = machine.CurrentState.ToString()!;
        await Store.SaveAsync(state, ct).ConfigureAwait(false);
    }

    public abstract Task CompensateAsync(TState state, CancellationToken ct = default);
}
