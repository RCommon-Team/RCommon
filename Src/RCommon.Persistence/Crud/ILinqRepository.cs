using RCommon.Entities;
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
    /// A LINQ-enabled repository that combines read, write, queryable, and eager loading capabilities
    /// for entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type, which must implement <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// This interface extends <see cref="IQueryable{T}"/>, allowing repositories to be used directly
    /// in LINQ queries. For graph/change-tracked scenarios, see <see cref="IGraphRepository{TEntity}"/>.
    /// </remarks>
    public interface ILinqRepository<TEntity>: IQueryable<TEntity>, IReadOnlyRepository<TEntity>, IWriteOnlyRepository<TEntity>,
        IEagerLoadableQueryable<TEntity>
        where TEntity : IBusinessEntity
    {
        /// <summary>
        /// Returns a queryable filtered by the given specification.
        /// </summary>
        /// <param name="specification">The specification to filter entities.</param>
        /// <returns>An <see cref="IQueryable{TEntity}"/> representing the filtered query.</returns>
        IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);

        /// <summary>
        /// Returns a queryable filtered by the given expression.
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <returns>An <see cref="IQueryable{TEntity}"/> representing the filtered query.</returns>
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);

        /// <summary>
        /// Returns a queryable filtered, ordered, and paged by the given parameters.
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <param name="orderByExpression">An expression specifying the ordering property.</param>
        /// <param name="orderByAscending"><c>true</c> for ascending order; <c>false</c> for descending.</param>
        /// <param name="pageNumber">The 1-based page number (default is 1).</param>
        /// <param name="pageSize">The page size; 0 means no paging (default is 0).</param>
        /// <returns>An <see cref="IQueryable{TEntity}"/> representing the filtered, ordered, and paged query.</returns>
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0);

        /// <summary>
        /// Returns a queryable filtered and paged by the given paged specification.
        /// </summary>
        /// <param name="specification">The paged specification containing filter, ordering, and paging criteria.</param>
        /// <returns>An <see cref="IQueryable{TEntity}"/> representing the filtered and paged query.</returns>
        IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification);

        /// <summary>
        /// Returns a queryable filtered and ordered by the given parameters (without paging).
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <param name="orderByExpression">An expression specifying the ordering property.</param>
        /// <param name="orderByAscending"><c>true</c> for ascending order; <c>false</c> for descending.</param>
        /// <returns>An <see cref="IQueryable{TEntity}"/> representing the filtered and ordered query.</returns>
        IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending);

        /// <summary>
        /// Finds entities matching the expression with ordering and paging, returning a paginated list.
        /// </summary>
        /// <param name="expression">A lambda expression to filter entities.</param>
        /// <param name="orderByExpression">An expression specifying the ordering property.</param>
        /// <param name="orderByAscending"><c>true</c> for ascending order; <c>false</c> for descending.</param>
        /// <param name="pageNumber">The 1-based page number (default is 1).</param>
        /// <param name="pageSize">The page size; 0 means no paging (default is 0).</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>A paginated list of matching entities.</returns>
        Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0,
            CancellationToken token = default);

        /// <summary>
        /// Finds entities matching the paged specification, returning a paginated list.
        /// </summary>
        /// <param name="specification">The paged specification containing filter, ordering, and paging criteria.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>A paginated list of matching entities.</returns>
        Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default);

        /// <summary>
        /// Eagerly loads a related navigation property specified by the expression.
        /// </summary>
        /// <param name="path">An expression specifying the navigation property to include.</param>
        /// <returns>An <see cref="IEagerLoadableQueryable{TEntity}"/> for further chained includes via ThenInclude.</returns>
        IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path);
    }
}
