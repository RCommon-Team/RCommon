using LinqToDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Entities;
using RCommon.Security.Claims;
using RCommon.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.Tools;
using LinqToDB.Data;
using DataExtensions = LinqToDB.Tools.DataExtensions;
using RCommon;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;
using LinqToDB.Linq;
using LinqToDB.Async;

namespace RCommon.Persistence.Linq2Db.Crud
{
    /// <summary>
    /// A concrete repository implementation using Linq2Db for CRUD operations and LINQ-based querying.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository. Must implement <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// Queries are built against <see cref="ITable{TEntity}"/> from the underlying <see cref="RCommonDataConnection"/>.
    /// Supports eager loading via <see cref="Include"/> and <see cref="ThenInclude{TPreviousProperty, TProperty}"/>
    /// using Linq2Db's <c>LoadWith</c>/<c>ThenLoad</c> API.
    /// </remarks>
    public class Linq2DbRepository<TEntity> : LinqRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private IQueryable<TEntity>? _repositoryQuery;
        private ILoadWithQueryable<TEntity, object>? _includableQueryable;
        private readonly IDataStoreFactory _dataStoreFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="Linq2DbRepository{TEntity}"/>.
        /// </summary>
        /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDataConnection"/> for the configured data store.</param>
        /// <param name="logger">Factory for creating loggers scoped to this repository type.</param>
        /// <param name="eventTracker">Tracker used to register entities for domain event dispatching.</param>
        /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
        public Linq2DbRepository(IDataStoreFactory dataStoreFactory,
            ILoggerFactory logger, IEntityEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
            ITenantIdAccessor tenantIdAccessor)
            : base(dataStoreFactory, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (eventTracker is null)
            {
                throw new ArgumentNullException(nameof(eventTracker));
            }

            if (defaultDataStoreOptions is null)
            {
                throw new ArgumentNullException(nameof(defaultDataStoreOptions));
            }

            Logger = logger.CreateLogger(GetType().Name);
            _repositoryQuery = null;
            _includableQueryable = null;
            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        }


        /// <summary>
        /// Gets the <see cref="RCommonDataConnection"/> for the configured data store, resolved through the <see cref="IDataStoreFactory"/>.
        /// </summary>
        protected internal RCommonDataConnection DataConnection
        {
            get
            {
                return this._dataStoreFactory.Resolve<RCommonDataConnection>(this.DataStoreName);
            }
        }

        /// <summary>
        /// Gets the Linq2Db <see cref="ITable{TEntity}"/> from the current <see cref="DataConnection"/> for direct table operations.
        /// </summary>
        protected ITable<TEntity> ObjectSet
        {
            get
            {
                return DataConnection.GetTable<TEntity>();
            }
        }

        /// <summary>
        /// Adds an eager-loading path for the specified navigation property using Linq2Db's <c>LoadWith</c> API.
        /// </summary>
        /// <param name="path">An expression selecting the navigation property to include.</param>
        /// <returns>This repository instance for fluent chaining of additional includes.</returns>
        public override IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            _includableQueryable = RepositoryQuery.LoadWith(path!);
            return this;
        }

        /// <summary>
        /// Adds a subsequent eager-loading path for a nested navigation property after a prior <see cref="Include"/> call,
        /// using Linq2Db's <c>ThenLoad</c> API.
        /// </summary>
        /// <typeparam name="TPreviousProperty">The type of the previously included navigation property.</typeparam>
        /// <typeparam name="TProperty">The type of the nested navigation property to include.</typeparam>
        /// <param name="path">An expression selecting the nested navigation property to include.</param>
        /// <returns>This repository instance for fluent chaining.</returns>
        public override IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            _repositoryQuery = _includableQueryable!.ThenLoad(path!);
            return this;
        }


        /// <summary>
        /// Gets the base <see cref="IQueryable{TEntity}"/> used for all query operations.
        /// Applies eager-loading expressions if any have been configured via <see cref="Include"/>.
        /// </summary>
        protected override IQueryable<TEntity> RepositoryQuery
        {
            get
            {
                if (_repositoryQuery == null)
                {
                    _repositoryQuery = ObjectSet.AsQueryable();
                }

                // Override the base query with the eager-loaded queryable if includes have been configured
                if (_includableQueryable != null)
                {
                    _repositoryQuery = _includableQueryable;
                }
                return _repositoryQuery;
            }
        }

        /// <summary>
        /// Core query method that applies the given filter expression to the <see cref="RepositoryQuery"/>.
        /// All find operations delegate to this method to build the filtered queryable.
        /// </summary>
        /// <param name="expression">A predicate expression to filter entities.</param>
        /// <returns>An <see cref="IQueryable{TEntity}"/> representing the filtered query.</returns>
        /// <exception cref="NullReferenceException">Thrown when <see cref="RepositoryQuery"/> is null.</exception>
        private IQueryable<TEntity> FindCore(Expression<Func<TEntity, bool>> expression)
        {
            IQueryable<TEntity> queryable;
            try
            {
                Guard.Against<NullReferenceException>(FilteredRepositoryQuery == null, "RepositoryQuery is null");
                queryable = FilteredRepositoryQuery.Where(expression);
            }
            catch (ApplicationException exception)
            {
                Logger.LogError(exception, "Error in {0}.FindCore while executing a query on the Context.", GetType().FullName);
                throw;
            }
            return queryable;
        }


        /// <inheritdoc />
        public async override Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
            await DataConnection.InsertAsync(entity, token: token);
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FilteredRepositoryQuery.AnyAsync(expression, token: token);
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await AnyAsync(specification.Predicate, token: token);
        }

        /// <summary>
        /// Deletes the entity. If <typeparamref name="TEntity"/> implements <see cref="ISoftDelete"/>,
        /// a soft delete is performed automatically (sets <c>IsDeleted = true</c> and issues an UPDATE).
        /// Otherwise a physical DELETE is executed.
        /// </summary>
        public async override Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TEntity>())
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                await UpdateAsync(entity, token);
                return;
            }

            EventTracker.AddEntity(entity);
            await DataConnection.DeleteAsync(entity);
        }

        /// <summary>
        /// Deletes the entity using the explicitly specified delete mode. When <paramref name="isSoftDelete"/>
        /// is <c>true</c>, the entity must implement <see cref="ISoftDelete"/>; its <c>IsDeleted</c> property
        /// is set to <c>true</c> and an UPDATE is issued. When <c>false</c>, a physical DELETE is always
        /// performed — even if the entity implements <see cref="ISoftDelete"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task DeleteAsync(TEntity entity, bool isSoftDelete, CancellationToken token = default)
        {
            if (!isSoftDelete)
            {
                // Bypass auto-detection — force a physical delete
                EventTracker.AddEntity(entity);
                await DataConnection.DeleteAsync(entity);
                return;
            }

            SoftDeleteHelper.EnsureSoftDeletable<TEntity>();
            SoftDeleteHelper.MarkAsDeleted(entity);
            await UpdateAsync(entity, token);
        }

        /// <summary>
        /// Deletes entities matching the expression. If <typeparamref name="TEntity"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically (marks each matching
        /// entity as deleted and issues UPDATEs). Otherwise a physical DELETE is executed.
        /// </summary>
        public async override Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TEntity>())
            {
                return await DeleteManyAsync(expression, isSoftDelete: true, token);
            }

            return await RepositoryQuery.Where(expression).DeleteAsync(token);
        }

        /// <summary>
        /// Deletes entities matching the expression. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching entity must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <remarks>
        /// The soft-delete path fetches matching entities into memory, marks each as deleted, then updates
        /// them one by one via Linq2Db's <c>UpdateAsync</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        {
            if (!isSoftDelete)
            {
                // Bypass auto-detection and soft-delete filter — force a physical delete
                return await RepositoryQuery.Where(expression).DeleteAsync(token);
            }

            SoftDeleteHelper.EnsureSoftDeletable<TEntity>();

            var entities = await FindQuery(expression).ToListAsync(token);
            int count = 0;
            foreach (var entity in entities)
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                await DataConnection.UpdateAsync(entity, token: token);
                count++;
            }
            return count;
        }

        /// <inheritdoc />
        public async override Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, token);
        }

        /// <summary>
        /// Deletes entities matching the specification. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching entity must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(ISpecification<TEntity> specification, bool isSoftDelete, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, isSoftDelete, token);
        }

        /// <inheritdoc />
        public override IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return FindCore(specification.Predicate);
        }

        /// <inheritdoc />
        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            return FindCore(expression);
        }

        /// <summary>
        /// This is not yet implemented due to Linq2Db's inability to find primary key or array of primary key.
        /// </summary>
        /// <param name="primaryKey">Value of Primary Key</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns><see cref="NotImplementedException"></see></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            //TODO: implement FindASync(object primaryKey)
            throw new NotImplementedException();
            //DataExtensions.RetrieveIdentity<TEntity>(IEnumerable<TEntity>
        }

        /// <inheritdoc />
        public async override Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindCore(specification.Predicate).ToListAsync(token);
        }

        /// <inheritdoc />
        public async override Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).ToListAsync(token);
        }

        /// <inheritdoc />
        public async override Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default)
        {
            IQueryable<TEntity> query;
            if (specification.OrderByAscending)
            {
                query = FindCore(specification.Predicate).OrderBy(specification.OrderByExpression);
            }
            else
            {
                query = FindCore(specification.Predicate).OrderByDescending(specification.OrderByExpression);
            }
            return await Task.FromResult(query.ToPaginatedList(specification.PageNumber, specification.PageSize));
        }

        /// <inheritdoc />
        public async override Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 1,
            CancellationToken token = default)
        {
            IQueryable<TEntity> query;
            if (orderByAscending)
            {
                query = FindCore(expression).OrderBy(orderByExpression);
            }
            else
            {
                query = FindCore(expression).OrderByDescending(orderByExpression);
            }
            return await Task.FromResult(query.ToPaginatedList(pageNumber, pageSize));
        }

        /// <inheritdoc />
        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        {
            IQueryable<TEntity> query;
            if (orderByAscending)
            {
                query = FindCore(expression).OrderBy(orderByExpression);
            }
            else
            {
                query = FindCore(expression).OrderByDescending(orderByExpression);
            }
            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        /// <inheritdoc />
        public override IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification)
        {
            return this.FindQuery(specification.Predicate, specification.OrderByExpression,
                 specification.OrderByAscending, specification.PageNumber, specification.PageSize);
        }

        /// <inheritdoc />
        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending)
        {
            IQueryable<TEntity> query;
            if (orderByAscending)
            {
                query = FindCore(expression).OrderBy(orderByExpression);
            }
            else
            {
                query = FindCore(expression).OrderByDescending(orderByExpression);
            }
            return query;
        }

        /// <inheritdoc />
        public async override Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return (await FilteredRepositoryQuery.SingleOrDefaultAsync(expression, token))!;
        }

        /// <inheritdoc />
        public async override Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindSingleOrDefaultAsync(specification.Predicate, token);
        }

        /// <inheritdoc />
        public async override Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await GetCountAsync(selectSpec.Predicate, token);
        }

        /// <inheritdoc />
        public async override Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FilteredRepositoryQuery.CountAsync(expression, token);
        }

        /// <inheritdoc />
        public async override Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            await DataConnection.UpdateAsync(entity, token: token);
        }

        /// <summary>
        /// Adds a range of transient entities to be persisted using Linq2Db. 
        /// Loops through the records and inserts them one by one.
        /// </summary>
        /// <param name="entities">Collection of entities to persist.</param>
        /// <param name="token">Cancellation token.</param>
        public override async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            // Iterate through each entity, track it, stamp tenant, and insert asynchronously.
            foreach (var entity in entities)
            {
                EventTracker.AddEntity(entity);
                MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
                await DataConnection.InsertAsync(entity, token: token);
            }
        }

    }
}
