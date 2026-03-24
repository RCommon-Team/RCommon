using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Sagas;

public class InMemorySagaStore<TState, TKey> : ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly ConcurrentDictionary<TKey, TState> _store = new();

    public Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        _store.TryGetValue(id, out var state);
        return Task.FromResult(state);
    }

    public Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        var state = _store.Values.FirstOrDefault(s => s.CorrelationId == correlationId);
        return Task.FromResult(state);
    }

    public Task SaveAsync(TState state, CancellationToken ct = default)
    {
        _store.AddOrUpdate(state.Id, state, (_, _) => state);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TState state, CancellationToken ct = default)
    {
        _store.TryRemove(state.Id, out _);
        return Task.CompletedTask;
    }
}
