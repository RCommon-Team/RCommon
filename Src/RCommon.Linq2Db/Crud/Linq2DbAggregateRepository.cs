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
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Tools;
using LinqToDB.Data;
using RCommon;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;
using LinqToDB.Linq;
using LinqToDB.Async;

namespace RCommon.Persistence.Linq2Db.Crud
{
    /// <summary>
    /// A DDD-constrained repository for aggregate roots backed by Linq2Db.
    /// Inherits full LINQ repository infrastructure from <see cref="LinqRepositoryBase{TAggregate}"/>
    /// and exposes the narrow <see cref="IAggregateRepository{TAggregate, TKey}"/> contract for
    /// aggregate-appropriate operations only.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type. Must implement <see cref="IAggregateRoot{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The type of the aggregate's identity key.</typeparam>
    public class Linq2DbAggregateRepository<TAggregate, TKey> : LinqRepositoryBase<TAggregate>, IAggregateRepository<TAggregate, TKey>
        where TAggregate : class, IAggregateRoot<TKey>
        where TKey : IEquatable<TKey>
    {
        private IQueryable<TAggregate>? _repositoryQuery;
        private ILoadWithQueryable<TAggregate, object>? _includableQueryable;
        private readonly IDataStoreFactory _dataStoreFactory;

        /// <summary>
        /// Initializes a new instance of <see cref="Linq2DbAggregateRepository{TAggregate, TKey}"/>.
        /// </summary>
        /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDataConnection"/> for the configured data store.</param>
        /// <param name="logger">Factory for creating loggers scoped to this repository type.</param>
        /// <param name="eventTracker">Tracker used to register entities for domain event dispatching.</param>
        /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
        /// <param name="tenantIdAccessor">Accessor for the current tenant identifier.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
        public Linq2DbAggregateRepository(IDataStoreFactory dataStoreFactory,
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
        /// Gets the Linq2Db <see cref="ITable{TAggregate}"/> from the current <see cref="DataConnection"/> for direct table operations.
        /// </summary>
        protected ITable<TAggregate> Table
        {
            get
            {
                return DataConnection.GetTable<TAggregate>();
            }
        }

        /// <summary>
        /// Adds an eager-loading path for the specified navigation property using Linq2Db's <c>LoadWith</c> API.
        /// </summary>
        /// <param name="path">An expression selecting the navigation property to include.</param>
        /// <returns>This repository instance for fluent chaining of additional includes.</returns>
        public override IEagerLoadableQueryable<TAggregate> Include(Expression<Func<TAggregate, object>> path)
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
        public override IEagerLoadableQueryable<TAggregate> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            _repositoryQuery = _includableQueryable!.ThenLoad(path!);
            return this;
        }

        /// <summary>
        /// Gets the base <see cref="IQueryable{TAggregate}"/> used for all query operations.
        /// Applies eager-loading expressions if any have been configured via <see cref="Include"/>.
        /// </summary>
        protected override IQueryable<TAggregate> RepositoryQuery
        {
            get
            {
                if (_repositoryQuery == null)
                {
                    _repositoryQuery = Table.AsQueryable();
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
        /// Core query method that applies the given filter expression to the <see cref="FilteredRepositoryQuery"/>.
        /// All find operations delegate to this method to build the filtered queryable.
        /// </summary>
        /// <param name="expression">A predicate expression to filter entities.</param>
        /// <returns>An <see cref="IQueryable{TAggregate}"/> representing the filtered query.</returns>
        /// <exception cref="NullReferenceException">Thrown when <see cref="FilteredRepositoryQuery"/> is null.</exception>
        private IQueryable<TAggregate> FindCore(Expression<Func<TAggregate, bool>> expression)
        {
            IQueryable<TAggregate> queryable;
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
        public async override Task AddAsync(TAggregate entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
            await DataConnection.InsertAsync(entity, token: token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            return await FilteredRepositoryQuery.AnyAsync(expression, token: token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await AnyAsync(specification.Predicate, token: token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the aggregate. If <typeparamref name="TAggregate"/> implements <see cref="ISoftDelete"/>,
        /// a soft delete is performed automatically (sets <c>IsDeleted = true</c> and issues an UPDATE).
        /// Otherwise a physical DELETE is executed.
        /// </summary>
        public async override Task DeleteAsync(TAggregate entity, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TAggregate>())
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                await UpdateAsync(entity, token).ConfigureAwait(false);
                return;
            }

            EventTracker.AddEntity(entity);
            await DataConnection.DeleteAsync(entity, token: token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the aggregate using the explicitly specified delete mode. When <paramref name="isSoftDelete"/>
        /// is <c>true</c>, the aggregate must implement <see cref="ISoftDelete"/>; its <c>IsDeleted</c> property
        /// is set to <c>true</c> and an UPDATE is issued. When <c>false</c>, a physical DELETE is always
        /// performed — even if the aggregate implements <see cref="ISoftDelete"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TAggregate"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task DeleteAsync(TAggregate entity, bool isSoftDelete, CancellationToken token = default)
        {
            if (!isSoftDelete)
            {
                // Bypass auto-detection — force a physical delete
                EventTracker.AddEntity(entity);
                await DataConnection.DeleteAsync(entity, token: token).ConfigureAwait(false);
                return;
            }

            SoftDeleteHelper.EnsureSoftDeletable<TAggregate>();
            SoftDeleteHelper.MarkAsDeleted(entity);
            await UpdateAsync(entity, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes aggregates matching the expression. If <typeparamref name="TAggregate"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically (marks each matching
        /// entity as deleted and issues UPDATEs). Otherwise a physical DELETE is executed.
        /// </summary>
        public async override Task<int> DeleteManyAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TAggregate>())
            {
                return await DeleteManyAsync(expression, isSoftDelete: true, token).ConfigureAwait(false);
            }

            return await RepositoryQuery.Where(expression).DeleteAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes aggregates matching the expression. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching aggregate must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <remarks>
        /// The soft-delete path fetches matching entities into memory, marks each as deleted, then updates
        /// them one by one via Linq2Db's <c>UpdateAsync</c>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TAggregate"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(Expression<Func<TAggregate, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        {
            if (!isSoftDelete)
            {
                // Bypass auto-detection and soft-delete filter — force a physical delete
                return await RepositoryQuery.Where(expression).DeleteAsync(token).ConfigureAwait(false);
            }

            SoftDeleteHelper.EnsureSoftDeletable<TAggregate>();

            var entities = await FindQuery(expression).ToListAsync(token).ConfigureAwait(false);
            int count = 0;
            foreach (var entity in entities)
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                await DataConnection.UpdateAsync(entity, token: token).ConfigureAwait(false);
                count++;
            }
            return count;
        }

        /// <inheritdoc />
        public async override Task<int> DeleteManyAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes aggregates matching the specification. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching aggregate must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TAggregate"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(ISpecification<TAggregate> specification, bool isSoftDelete, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, isSoftDelete, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override IQueryable<TAggregate> FindQuery(ISpecification<TAggregate> specification)
        {
            return FindCore(specification.Predicate);
        }

        /// <inheritdoc />
        public override IQueryable<TAggregate> FindQuery(Expression<Func<TAggregate, bool>> expression)
        {
            return FindCore(expression);
        }

        /// <summary>
        /// This is not yet implemented due to Linq2Db's inability to find primary key or array of primary key.
        /// </summary>
        /// <param name="primaryKey">Value of Primary Key</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns><see cref="NotImplementedException"/></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override async Task<TAggregate> FindAsync(object primaryKey, CancellationToken token = default)
        {
            //TODO: implement FindAsync(object primaryKey)
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async override Task<ICollection<TAggregate>> FindAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await FindCore(specification.Predicate).ToListAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<ICollection<TAggregate>> FindAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).ToListAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<IPaginatedList<TAggregate>> FindAsync(IPagedSpecification<TAggregate> specification, CancellationToken token = default)
        {
            IQueryable<TAggregate> query;
            if (specification.OrderByAscending)
            {
                query = FindCore(specification.Predicate).OrderBy(specification.OrderByExpression);
            }
            else
            {
                query = FindCore(specification.Predicate).OrderByDescending(specification.OrderByExpression);
            }
            return await Task.FromResult(query.ToPaginatedList(specification.PageNumber, specification.PageSize)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<IPaginatedList<TAggregate>> FindAsync(Expression<Func<TAggregate, bool>> expression, Expression<Func<TAggregate, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 1,
            CancellationToken token = default)
        {
            IQueryable<TAggregate> query;
            if (orderByAscending)
            {
                query = FindCore(expression).OrderBy(orderByExpression);
            }
            else
            {
                query = FindCore(expression).OrderByDescending(orderByExpression);
            }
            return await Task.FromResult(query.ToPaginatedList(pageNumber, pageSize)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override IQueryable<TAggregate> FindQuery(Expression<Func<TAggregate, bool>> expression, Expression<Func<TAggregate, object>> orderByExpression,
            bool orderByAscending, int pageNumber = 1, int pageSize = 0)
        {
            IQueryable<TAggregate> query;
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
        public override IQueryable<TAggregate> FindQuery(IPagedSpecification<TAggregate> specification)
        {
            return this.FindQuery(specification.Predicate, specification.OrderByExpression,
                specification.OrderByAscending, specification.PageNumber, specification.PageSize);
        }

        /// <inheritdoc />
        public override IQueryable<TAggregate> FindQuery(Expression<Func<TAggregate, bool>> expression, Expression<Func<TAggregate, object>> orderByExpression,
            bool orderByAscending)
        {
            IQueryable<TAggregate> query;
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
        public async override Task<TAggregate> FindSingleOrDefaultAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            return (await FilteredRepositoryQuery.SingleOrDefaultAsync(expression, token).ConfigureAwait(false))!;
        }

        /// <inheritdoc />
        public async override Task<TAggregate> FindSingleOrDefaultAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await FindSingleOrDefaultAsync(specification.Predicate, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<long> GetCountAsync(ISpecification<TAggregate> selectSpec, CancellationToken token = default)
        {
            return await GetCountAsync(selectSpec.Predicate, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<long> GetCountAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            return await FilteredRepositoryQuery.CountAsync(expression, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task UpdateAsync(TAggregate entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            await DataConnection.UpdateAsync(entity, token: token).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a range of transient aggregates to be persisted using Linq2Db.
        /// Loops through the records and inserts them one by one.
        /// </summary>
        /// <param name="entities">Collection of aggregates to persist.</param>
        /// <param name="token">Cancellation token.</param>
        public override async Task AddRangeAsync(IEnumerable<TAggregate> entities, CancellationToken token = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            foreach (var entity in entities)
            {
                EventTracker.AddEntity(entity);
                MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
                await DataConnection.InsertAsync(entity, token: token).ConfigureAwait(false);
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // Explicit IAggregateRepository<TAggregate, TKey> implementations
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads an aggregate root by its identity key.
        /// </summary>
        async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.GetByIdAsync(TKey id, CancellationToken cancellationToken)
        {
            return await FilteredRepositoryQuery.FirstOrDefaultAsync(e => e.Id.Equals(id), token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds a single aggregate matching the given specification.
        /// </summary>
        async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken)
        {
            return await FilteredRepositoryQuery.Where(specification.Predicate).FirstOrDefaultAsync(token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks whether an aggregate with the given identity key exists.
        /// </summary>
        async Task<bool> IAggregateRepository<TAggregate, TKey>.ExistsAsync(TKey id, CancellationToken cancellationToken)
        {
            return await FilteredRepositoryQuery.AnyAsync(e => e.Id.Equals(id), token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a new aggregate root to the repository and persists it.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.AddAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            EventTracker.AddEntity(aggregate);
            MultiTenantHelper.SetTenantIdIfApplicable(aggregate, _tenantIdAccessor.GetTenantId());
            await DataConnection.InsertAsync(aggregate, token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing aggregate root and persists the changes.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            EventTracker.AddEntity(aggregate);
            await DataConnection.UpdateAsync(aggregate, token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an aggregate root. If the aggregate implements <see cref="ISoftDelete"/>,
        /// a soft delete is performed automatically.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TAggregate>())
            {
                SoftDeleteHelper.MarkAsDeleted(aggregate);
                EventTracker.AddEntity(aggregate);
                await DataConnection.UpdateAsync(aggregate, token: cancellationToken).ConfigureAwait(false);
                return;
            }

            EventTracker.AddEntity(aggregate);
            await DataConnection.DeleteAsync(aggregate, token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds an eager-loading include path and returns the aggregate repository for fluent chaining.
        /// </summary>
        IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.Include<TProperty>(Expression<Func<TAggregate, TProperty>> path)
        {
            // Convert to Expression<Func<TAggregate, object>> so it is compatible with the
            // ILoadWithQueryable<TAggregate, object> field used by the base Include logic.
            var converted = Expression.Lambda<Func<TAggregate, object>>(
                Expression.Convert(path.Body, typeof(object)), path.Parameters);

            _includableQueryable = RepositoryQuery.LoadWith(converted!);

            return this;
        }

        /// <summary>
        /// Adds a subsequent eager-loading path for a nested navigation property and returns the aggregate repository for fluent chaining.
        /// </summary>
        IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> path)
        {
            // Rewrite the expression from Func<TPreviousProperty, TProperty> to Func<object, TProperty>
            // to match the ILoadWithQueryable<TAggregate, object> field type.
            var param = Expression.Parameter(typeof(object), path.Parameters[0].Name);
            var castParam = Expression.Convert(param, typeof(TPreviousProperty));
            var body = new ParameterReplacingVisitor(path.Parameters[0], castParam).Visit(path.Body);
            var converted = Expression.Lambda<Func<object, TProperty>>(body, param);

            _repositoryQuery = _includableQueryable!.ThenLoad(converted!);

            return this;
        }

        /// <summary>
        /// A simple expression visitor that replaces a specific parameter expression with another expression.
        /// Used to rewrite ThenInclude expressions for Linq2Db's ThenLoad API.
        /// </summary>
        private sealed class ParameterReplacingVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam;
            private readonly Expression _newExpr;

            public ParameterReplacingVisitor(ParameterExpression oldParam, Expression newExpr)
            {
                _oldParam = oldParam;
                _newExpr = newExpr;
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParam ? _newExpr : base.VisitParameter(node);
            }
        }
    }
}
