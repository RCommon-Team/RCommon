using System;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Sagas;

public interface ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default);
    Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task SaveAsync(TState state, CancellationToken ct = default);
    Task DeleteAsync(TState state, CancellationToken ct = default);
}
