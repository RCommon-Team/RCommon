using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RCommon.Domain.Repositories
{
    public interface ICrudRepository<TEntity, TDataStore>
    {

        /// <summary>
        /// Adds a transient instance of <paramref name="entity"/> to be tracked
        /// and persisted by the repository.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> to be persisted.</param>
        TEntity Add(TEntity entity);

        /// <summary>
        /// Marks the changes of an existing entity to be saved to the store.
        /// </summary>
        /// <param name="entity">An instance of <typeparamref name="TEntity"/> that should be
        /// updated in the database.</param>
        /// <remarks>Implementors of this method must handle the Update scneario. </remarks>
        void Delete(TEntity entity);

        /// <summary>
        /// Attaches a detached entity.
        /// </summary>
        /// <param name="entity">The entity instance to attach back to the repository.</param>
        /// <exception cref="NotSupportedException">Implentors should throw the NotImplementedException if Attaching
        /// entities is not supported.</exception>
        void Attach(TEntity entity);

        void Update(TEntity entity);

        ICollection<TEntity> Find(ISpecification<TEntity> specification);

        ICollection<TEntity> Find(Expression<Func<TEntity, bool>> expression);

        TEntity Find(object primaryKey);

        int GetCount(ISpecification<TEntity> selectSpec);
        int GetCount(Expression<Func<TEntity, bool>> expression);

        TEntity FindSingleOrDefault(Expression<Func<TEntity, bool>> expression);

        TEntity FindSingleOrDefault(ISpecification<TEntity> specification);


    }
}
