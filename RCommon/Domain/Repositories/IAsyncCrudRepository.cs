using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Domain.Repositories
{
    public interface IAsyncCrudRepository<TEntity> : IQueryable<TEntity>
    {
        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);

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

        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification);
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FindAsync(object primaryKey);

        Task<int> GetCountAsync(ISpecification<TEntity> selectSpec);
        Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression);

        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression);

        Task<bool> AnyAsync(ISpecification<TEntity> specification);

        public string DataStoreName { get; set; }


    }
}
