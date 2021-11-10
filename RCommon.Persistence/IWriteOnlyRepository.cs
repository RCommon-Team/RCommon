using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public interface IWriteOnlyRepository<TEntity>
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
        /// Marks the changes of an existing entity to be deleted from the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> that should be
        /// updated in the database.</param>
        /// /// <param name="token">Cancellation Token</param>
        /// <remarks>Implementors of this method must handle the Delete scneario. </remarks>
        /// <returns>Task</returns>
        Task DeleteAsync(TEntity entity, CancellationToken token = default);

        /// <summary>
        /// Marks the changes of an existing entity to be updated in the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> to be persisted.</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Task</returns>
        Task UpdateAsync(TEntity entity, CancellationToken token = default);

    }
}
