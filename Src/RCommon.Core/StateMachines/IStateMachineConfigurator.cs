using System;

namespace RCommon.StateMachines;

public interface IStateMachineConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    IStateConfigurator<TState, TTrigger> ForState(TState state);
    IStateMachine<TState, TTrigger> Build(TState initialState);
}
