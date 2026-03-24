using System;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Models.Events;

namespace RCommon.Persistence.Sagas;

public interface ISaga<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    Task HandleAsync<TEvent>(TEvent @event, TState state, CancellationToken ct = default)
        where TEvent : ISerializableEvent;
    Task CompensateAsync(TState state, CancellationToken ct = default);
}
