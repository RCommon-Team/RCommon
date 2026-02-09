using RCommon.Collections;
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
    /// Defines read-only repository operations for querying entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to query.</typeparam>
    /// <remarks>
    /// This interface provides async query methods using both <see cref="ISpecification{TEntity}"/> and
    /// lambda expressions. See <see cref="IWriteOnlyRepository{TEntity}"/> for write operations.
    /// </remarks>
    public interface IReadOnlyRepository<TEntity> : INamedDataSource
    {
        /// <summary>
        /// Finds all entities matching the given specification.
        /// </summary>
        /// <param name="specification">The specification to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>A collection of entities satisfying the specification.</returns>
        Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <summary>
        /// Finds all entities matching the given expression.
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>A collection of entities satisfying the expression.</returns>
        Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <summary>
        /// Finds a single entity by its primary key.
        /// </summary>
        /// <param name="primaryKey">The primary key value of the entity to find.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>The entity if found; otherwise, the default value for <typeparamref name="TEntity"/>.</returns>
        Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);

        /// <summary>
        /// Gets the count of entities matching the given specification.
        /// </summary>
        /// <param name="selectSpec">The specification to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>The number of matching entities.</returns>
        Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);

        /// <summary>
        /// Gets the count of entities matching the given expression.
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>The number of matching entities.</returns>
        Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <summary>
        /// Finds a single entity matching the expression, or returns the default value if none is found.
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>The matching entity or the default value for <typeparamref name="TEntity"/>.</returns>
        Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <summary>
        /// Finds a single entity matching the specification, or returns the default value if none is found.
        /// </summary>
        /// <param name="specification">The specification to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>The matching entity or the default value for <typeparamref name="TEntity"/>.</returns>
        Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <summary>
        /// Determines whether any entity matches the given expression.
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns><c>true</c> if at least one entity matches; otherwise, <c>false</c>.</returns>
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <summary>
        /// Determines whether any entity matches the given specification.
        /// </summary>
        /// <param name="specification">The specification to filter entities.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns><c>true</c> if at least one entity matches; otherwise, <c>false</c>.</returns>
        Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);
    }
}
