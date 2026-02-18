using RCommon.Entities;
using RCommon.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// Defines write-only repository operations for persisting entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to persist.</typeparam>
    /// <remarks>
    /// This interface provides async CRUD write methods. See <see cref="IReadOnlyRepository{TEntity}"/> for read operations.
    /// </remarks>
    public interface IWriteOnlyRepository<TEntity> : INamedDataSource
    {

        /// <summary>
        /// Adds a transient instance of <paramref name="entity"/> to be tracked
        /// and persisted by the repository.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> to be persisted.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task</returns>
        Task AddAsync(TEntity entity, CancellationToken token = default);

        /// <summary>
        /// Adds multiple transient instances of <paramref name="entities"/> to be tracked
        /// and persisted by the repository.
        /// </summary>
        /// <param name="entities">A collection of <typeparamref name="TEntity"/> instances to be persisted.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task</returns>
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default);

        /// <summary>
        /// Deletes the specified entity. If <typeparamref name="TEntity"/> implements <see cref="ISoftDelete"/>,
        /// a soft delete is performed automatically (sets <c>IsDeleted = true</c> and issues an UPDATE).
        /// Otherwise a physical DELETE is executed. Use <see cref="DeleteAsync(TEntity, bool, CancellationToken)"/>
        /// to explicitly control the delete mode.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> to delete.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task</returns>
        Task DeleteAsync(TEntity entity, CancellationToken token = default);

        /// <summary>
        /// Deletes entities matching the specification. If <typeparamref name="TEntity"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically.
        /// Use <see cref="DeleteManyAsync(ISpecification{TEntity}, bool, CancellationToken)"/>
        /// to explicitly control the delete mode.
        /// </summary>
        /// <param name="specification">Query specification</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Count of entities affected</returns>
        Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <summary>
        /// Deletes entities matching the expression. If <typeparamref name="TEntity"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically.
        /// Use <see cref="DeleteManyAsync(Expression{Func{TEntity, bool}}, bool, CancellationToken)"/>
        /// to explicitly control the delete mode.
        /// </summary>
        /// <param name="expression">Query expression</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Count of entities affected</returns>
        Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <summary>
        /// Marks the changes of an existing entity to be updated in the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> to be persisted.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task</returns>
        Task UpdateAsync(TEntity entity, CancellationToken token = default);

        /// <summary>
        /// Deletes the entity using the explicitly specified delete mode, bypassing auto-detection.
        /// When <paramref name="isSoftDelete"/> is <c>true</c>, the entity's <see cref="ISoftDelete.IsDeleted"/>
        /// property is set to <c>true</c> and an UPDATE is issued. When <c>false</c>, a physical DELETE is
        /// always performed — even if the entity implements <see cref="ISoftDelete"/>.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <param name="isSoftDelete">If <c>true</c>, performs a soft delete; if <c>false</c>, forces a physical delete.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but the entity does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        Task DeleteAsync(TEntity entity, bool isSoftDelete, CancellationToken token = default);

        /// <summary>
        /// Deletes entities matching the specification using the explicitly specified delete mode,
        /// bypassing auto-detection. When <paramref name="isSoftDelete"/> is <c>false</c>, a physical
        /// DELETE is always performed — even if the entity implements <see cref="ISoftDelete"/>.
        /// </summary>
        /// <param name="specification">Query specification</param>
        /// <param name="isSoftDelete">If <c>true</c>, performs a soft delete; if <c>false</c>, forces a physical delete.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Count of entities affected</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/> does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        Task<int> DeleteManyAsync(ISpecification<TEntity> specification, bool isSoftDelete, CancellationToken token = default);

        /// <summary>
        /// Deletes entities matching the expression using the explicitly specified delete mode,
        /// bypassing auto-detection. When <paramref name="isSoftDelete"/> is <c>false</c>, a physical
        /// DELETE is always performed — even if the entity implements <see cref="ISoftDelete"/>.
        /// </summary>
        /// <param name="expression">Query expression</param>
        /// <param name="isSoftDelete">If <c>true</c>, performs a soft delete; if <c>false</c>, forces a physical delete.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Count of entities affected</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/> does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default);

    }
}
