using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.StateMachines;

public interface IStateMachine<TState, TTrigger>
    where TState : struct, Enum
    where TTrigger : struct, Enum
{
    TState CurrentState { get; }
    Task FireAsync(TTrigger trigger, CancellationToken cancellationToken = default);
    Task FireAsync<TData>(TTrigger trigger, TData data, CancellationToken cancellationToken = default);
    bool CanFire(TTrigger trigger);
    IEnumerable<TTrigger> PermittedTriggers { get; }
}
