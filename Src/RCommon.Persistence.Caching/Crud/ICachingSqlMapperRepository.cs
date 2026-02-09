using RCommon.Entities;
using RCommon.Persistence.Crud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Persistence.Caching.Crud
{
    /// <summary>
    /// Extends <see cref="ISqlMapperRepository{TEntity}"/> with cache-aware query overloads that accept a cache key.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository.</typeparam>
    /// <remarks>
    /// Each overload mirrors a standard <c>FindAsync</c> method but adds an <c>object cacheKey</c>
    /// parameter so results can be stored in and retrieved from a cache layer before hitting the data store.
    /// </remarks>
    public interface ICachingSqlMapperRepository<TEntity> : ISqlMapperRepository<TEntity>
        where TEntity : class, IBusinessEntity
    {
        /// <summary>
        /// Finds a collection of entities matching the specification, caching the result under the specified key.
        /// </summary>
        /// <param name="cacheKey">The key used to store/retrieve the cached result.</param>
        /// <param name="specification">The specification defining the query filter.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A collection of matching entities.</returns>
        Task<ICollection<TEntity>> FindAsync(object cacheKey, ISpecification<TEntity> specification, CancellationToken token = default);

        /// <summary>
        /// Finds a collection of entities matching the expression, caching the result under the specified key.
        /// </summary>
        /// <param name="cacheKey">The key used to store/retrieve the cached result.</param>
        /// <param name="expression">A filter expression for the query.</param>
        /// <param name="token">A cancellation token.</param>
        /// <returns>A collection of matching entities.</returns>
        Task<ICollection<TEntity>> FindAsync(object cacheKey, Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
    }
}
