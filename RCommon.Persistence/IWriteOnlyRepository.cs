using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        Task AddAsync(TEntity entity);

        /// <summary>
        /// Marks the changes of an existing entity to be deleted from the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> that should be
        /// updated in the database.</param>
        /// <remarks>Implementors of this method must handle the Delete scneario. </remarks>
        Task DeleteAsync(TEntity entity);

        Task UpdateAsync(TEntity entity);

    }
}
