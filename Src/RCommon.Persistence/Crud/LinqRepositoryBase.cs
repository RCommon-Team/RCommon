using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using RCommon.Entities;
using RCommon.Security.Claims;
using System.Data;
using Microsoft.Extensions.Logging;
using RCommon.Persistence.Sql;
using System.Threading;
using RCommon.Collections;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Transactions;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// Abstract base class for LINQ-enabled repositories that provides common queryable infrastructure,
    /// event tracking, and data store resolution for entities of type <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type, which must implement <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// This class implements <see cref="IQueryable{T}"/> by delegating to the abstract <see cref="RepositoryQuery"/>
    /// property, which concrete implementations must provide. It also handles default data store name assignment
    /// from <see cref="DefaultDataStoreOptions"/>.
    /// </remarks>
    public abstract class LinqRepositoryBase<TEntity> : DisposableResource, ILinqRepository<TEntity>
       where TEntity : IBusinessEntity
    {
        private string _dataStoreName = default!;
        private readonly IDataStoreFactory _dataStoreFactory;
        protected readonly ITenantIdAccessor _tenantIdAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinqRepositoryBase{TEntity}"/> class.
        /// </summary>
        /// <param name="dataStoreFactory">The factory used to resolve named data stores.</param>
        /// <param name="eventTracker">The entity event tracker for publishing domain events.</param>
        /// <param name="defaultDataStoreOptions">Options specifying the default data store name.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="dataStoreFactory"/>, <paramref name="eventTracker"/>, or
        /// <paramref name="defaultDataStoreOptions"/> is <c>null</c>.
        /// </exception>
        public LinqRepositoryBase(IDataStoreFactory dataStoreFactory,
            IEntityEventTracker eventTracker, IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
            ITenantIdAccessor tenantIdAccessor)
        {
            if (defaultDataStoreOptions is null)
            {
                throw new ArgumentNullException(nameof(defaultDataStoreOptions));
            }
            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
            EventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));
            _tenantIdAccessor = tenantIdAccessor ?? throw new ArgumentNullException(nameof(tenantIdAccessor));

            // Apply default data store name if configured, so repositories work without explicit name assignment
            if (defaultDataStoreOptions != null && defaultDataStoreOptions.Value != null
                && !defaultDataStoreOptions.Value.DefaultDataStoreName.IsNullOrEmpty())
            {
                this.DataStoreName = defaultDataStoreOptions.Value.DefaultDataStoreName;
            }
        }


        /// <summary>
        /// Gets the <see cref="IQueryable{TEntity}"/> used by the <see cref="GraphRepositoryBase{TEntity}"/> 
        /// to execute Linq queries.
        /// </summary>
        /// <value>A <see cref="IQueryable{TEntity}"/> instance.</value>
        /// <remarks>
        /// Inheritors of this base class should return a valid non-null <see cref="IQueryable{TEntity}"/> instance.
        /// </remarks>
        protected abstract IQueryable<TEntity> RepositoryQuery { get; }

        /// <summary>
        /// Gets the <see cref="RepositoryQuery"/> with automatic soft-delete and tenant filters applied.
        /// When <typeparamref name="TEntity"/> implements <see cref="ISoftDelete"/>, soft-deleted entities are excluded.
        /// When <typeparamref name="TEntity"/> implements <see cref="IMultiTenant"/> and a tenant ID is available,
        /// only entities belonging to the current tenant are returned.
        /// </summary>
        /// <remarks>
        /// All read operations should use this property instead of <see cref="RepositoryQuery"/> directly
        /// to ensure proper filtering. Write/delete operations that need unfiltered
        /// access should continue to use <see cref="RepositoryQuery"/>.
        /// </remarks>
        protected IQueryable<TEntity> FilteredRepositoryQuery
        {
            get
            {
                var query = RepositoryQuery;
                if (SoftDeleteHelper.IsSoftDeletable<TEntity>())
                    query = query.Where(SoftDeleteHelper.GetNotDeletedFilter<TEntity>());
                var tenantId = _tenantIdAccessor.GetTenantId();
                if (MultiTenantHelper.IsMultiTenant<TEntity>() && !string.IsNullOrEmpty(tenantId))
                    query = query.Where(MultiTenantHelper.GetTenantFilter<TEntity>(tenantId));
                return query;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="IEnumerator{TEntity}" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<TEntity> GetEnumerator()
        {
            return FilteredRepositoryQuery.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return FilteredRepositoryQuery.GetEnumerator();
        }

        /// <summary>
        /// Gets the expression tree that is associated with the instance of <see cref="IQueryable" />.
        /// </summary>
        /// <returns>
        /// The <see cref="Expression" /> that is associated with this instance of <see cref="IQueryable" />.
        /// </returns>
        public Expression Expression
        {
            get { return FilteredRepositoryQuery.Expression; }
        }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree associated with this instance of <see cref="IQueryable" /> is executed.
        /// </summary>
        /// <returns>
        /// A <see cref="Type" /> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.
        /// </returns>
        public Type ElementType
        {
            get { return FilteredRepositoryQuery.ElementType; }
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        /// <returns>
        /// The <see cref="IQueryProvider" /> that is associated with this data source.
        /// </returns>
        public IQueryProvider Provider
        {
            get { return FilteredRepositoryQuery.Provider; }
        }


        /// <summary>
        /// Querries the repository based on the provided specification and returns results that
        /// are only satisfied by the specification.
        /// </summary>
        /// <param name="specification">A <see cref="ISpecification{TEntity}"/> instnace used to filter results
        /// that only satisfy the specification.</param>
        /// <returns>A <see cref="IEnumerable{TEntity}"/> that can be used to enumerate over the results
        /// of the query.</returns>
        public IEnumerable<TEntity> Query(ISpecification<TEntity> specification)
        {
            return FilteredRepositoryQuery.Where(specification.Predicate).AsQueryable();
        }

        /// <inheritdoc />
        public abstract IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification);

        /// <inheritdoc />
        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression);

        /// <inheritdoc />
        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending);

        /// <inheritdoc />
        public abstract Task AddAsync(TEntity entity, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task DeleteAsync(TEntity entity, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task DeleteAsync(TEntity entity, bool isSoftDelete, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<int> DeleteManyAsync(ISpecification<TEntity> specification, bool isSoftDelete, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task UpdateAsync(TEntity entity, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0,
            CancellationToken token = default);

        /// <inheritdoc />
        public abstract Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default);

        /// <inheritdoc />
        public abstract IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0);

        /// <inheritdoc />
        public abstract IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification);

        /// <inheritdoc />
        public abstract IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path);

        /// <inheritdoc />
        public abstract IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path);

        /// <summary>
        /// Gets or sets the logger instance for this repository.
        /// </summary>
        public ILogger Logger { get; set; } = default!;

        /// <summary>
        /// Gets the entity event tracker used to track and publish domain events raised by entities.
        /// </summary>
        public IEntityEventTracker EventTracker { get; }

        /// <inheritdoc />
        public string DataStoreName
        {
            get => _dataStoreName;
            set
            {
                _dataStoreName = value;
            }
        }
    }

}
