using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Entities;

namespace RCommon.Persistence.Crud;

/// <summary>
/// DDD-constrained repository for aggregate roots. Provides only aggregate-appropriate
/// operations: load by ID, find by specification, existence check, add, update, delete,
/// and eager loading. Does not expose IQueryable or collection queries.
/// </summary>
public interface IAggregateRepository<TAggregate, TKey> : INamedDataSource
    where TAggregate : class, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TAggregate?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<TAggregate?> FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(TKey id, CancellationToken cancellationToken = default);

    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken = default);
    Task DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    IAggregateRepository<TAggregate, TKey> Include<TProperty>(
        Expression<Func<TAggregate, TProperty>> path);
    IAggregateRepository<TAggregate, TKey> ThenInclude<TPreviousProperty, TProperty>(
        Expression<Func<TPreviousProperty, TProperty>> path);
}
