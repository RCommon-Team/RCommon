using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.StateMachines;

public interface IStateConfigurator<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    IStateConfigurator<TState, TTrigger> Permit(TTrigger trigger, TState destinationState);
    IStateConfigurator<TState, TTrigger> OnEntry(Func<CancellationToken, Task> action);
    IStateConfigurator<TState, TTrigger> OnExit(Func<CancellationToken, Task> action);
    IStateConfigurator<TState, TTrigger> PermitIf(
        TTrigger trigger, TState destinationState, Func<bool> guard);
}
