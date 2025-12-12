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
        /// Marks the changes of an existing entity to be deleted from the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> that should be
        /// updated in the database.</param>
        /// /// <param name="token">Cancellation Token</param>
        /// <remarks>Implementors of this method must handle the Delete scenario. </remarks>
        /// <returns>Task</returns>
        Task DeleteAsync(TEntity entity, CancellationToken token = default);

        /// <summary>
        /// Deletes entities matching the criteria of the specification
        /// </summary>
        /// <param name="specification">Query specification</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Count of entities affected</returns>
        Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <summary>
        /// Deletes entities matching the criteria of the expression
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

    }
}
