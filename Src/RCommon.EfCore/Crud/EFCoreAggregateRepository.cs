using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon;
using RCommon.Entities;
using RCommon.Security.Claims;
using RCommon.Collections;
using RCommon.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RCommon.Persistence.Crud;

namespace RCommon.Persistence.EFCore.Crud
{

    /// <summary>
    /// A DDD-constrained repository for aggregate roots backed by Entity Framework Core.
    /// Inherits full LINQ/graph repository infrastructure from <see cref="GraphRepositoryBase{TAggregate}"/>
    /// and exposes the narrow <see cref="IAggregateRepository{TAggregate, TKey}"/> contract for
    /// aggregate-appropriate operations only.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type. Must implement <see cref="IAggregateRoot{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The type of the aggregate's identity key.</typeparam>
    public class EFCoreAggregateRepository<TAggregate, TKey> : GraphRepositoryBase<TAggregate>, IAggregateRepository<TAggregate, TKey>
        where TAggregate : class, IAggregateRoot<TKey>
        where TKey : IEquatable<TKey>
    {
        private IQueryable<TAggregate>? _repositoryQuery;
        private bool _tracking;
        private IIncludableQueryable<TAggregate, object>? _includableQueryable;
        private readonly IDataStoreFactory _dataStoreFactory;



        /// <summary>
        /// Initializes a new instance of <see cref="EFCoreAggregateRepository{TAggregate, TKey}"/>.
        /// </summary>
        /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDbContext"/> for the configured data store.</param>
        /// <param name="logger">Factory for creating loggers scoped to this repository type.</param>
        /// <param name="eventTracker">Tracker used to register entities for domain event dispatching.</param>
        /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
        /// <param name="tenantIdAccessor">Accessor for the current tenant identifier.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
        public EFCoreAggregateRepository(IDataStoreFactory dataStoreFactory,
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
            _tracking = true;
            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        }

        /// <summary>
        /// Gets the <see cref="DbSet{TAggregate}"/> from the current <see cref="ObjectContext"/> for direct entity set operations.
        /// </summary>
        protected DbSet<TAggregate> ObjectSet
        {
            get
            {
                return ObjectContext.Set<TAggregate>();
            }
        }

        /// <summary>
        /// Gets or sets whether EF Core change tracking is enabled for queries executed through this repository.
        /// </summary>
        public override bool Tracking
        {
            get => _tracking;
            set
            {
                _tracking = value;
            }

        }

        /// <summary>
        /// Adds an eager-loading include path for the specified navigation property.
        /// </summary>
        /// <param name="path">An expression selecting the navigation property to include.</param>
        /// <returns>This repository instance for fluent chaining of additional includes.</returns>
        public override IEagerLoadableQueryable<TAggregate> Include(Expression<Func<TAggregate, object>> path)
        {
            if (_includableQueryable == null)
            {
                // Start from existing query state (preserves prior ThenInclude chains) or fresh DbSet
                var source = _repositoryQuery ?? (IQueryable<TAggregate>)ObjectContext.Set<TAggregate>();
                _includableQueryable = source.Include(path);
            }
            else
            {
                _includableQueryable = _includableQueryable.Include(path);
            }

            return this;
        }

        /// <summary>
        /// Adds a subsequent eager-loading path for a nested navigation property after a prior <see cref="Include"/> call.
        /// </summary>
        /// <typeparam name="TPreviousProperty">The type of the previously included navigation property.</typeparam>
        /// <typeparam name="TProperty">The type of the nested navigation property to include.</typeparam>
        /// <param name="path">An expression selecting the nested navigation property to include.</param>
        /// <returns>This repository instance for fluent chaining.</returns>
        public override IEagerLoadableQueryable<TAggregate> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            _repositoryQuery = _includableQueryable!.ThenInclude(path);
            _includableQueryable = null; // Consumed — RepositoryQuery getter will use _repositoryQuery
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
                    _repositoryQuery = ObjectSet.AsQueryable<TAggregate>();
                }

                // Override the base query with the eager-loaded queryable if includes have been configured
                if (_includableQueryable != null)
                {
                    _repositoryQuery = _includableQueryable;
                }
                return _repositoryQuery;
            }
        }

        /// <inheritdoc />
        public override async Task AddAsync(TAggregate entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
            await ObjectSet.AddAsync(entity, token).ConfigureAwait(false);
            await SaveAsync(token).ConfigureAwait(false);
        }


        /// <summary>
        /// Deletes the entity. If <typeparamref name="TAggregate"/> implements <see cref="ISoftDelete"/>,
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
            ObjectSet.Remove(entity);
            await SaveAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the entity using the explicitly specified delete mode. When <paramref name="isSoftDelete"/>
        /// is <c>true</c>, the entity must implement <see cref="ISoftDelete"/>; its <c>IsDeleted</c> property
        /// is set to <c>true</c> and an UPDATE is issued. When <c>false</c>, a physical DELETE is always
        /// performed — even if the entity implements <see cref="ISoftDelete"/>.
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
                ObjectSet.Remove(entity);
                await SaveAsync().ConfigureAwait(false);
                return;
            }

            SoftDeleteHelper.EnsureSoftDeletable<TAggregate>();
            SoftDeleteHelper.MarkAsDeleted(entity);
            await UpdateAsync(entity, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes entities matching the specification. If <typeparamref name="TAggregate"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically.
        /// </summary>
        public async override Task<int> DeleteManyAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await this.DeleteManyAsync(specification.Predicate, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes entities matching the specification. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching entity must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TAggregate"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(ISpecification<TAggregate> specification, bool isSoftDelete, CancellationToken token = default)
        {
            return await this.DeleteManyAsync(specification.Predicate, isSoftDelete, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes entities matching the expression. If <typeparamref name="TAggregate"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically (marks each matching
        /// entity as deleted and issues UPDATEs). Otherwise a physical DELETE is executed.
        /// </summary>
        public async override Task<int> DeleteManyAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TAggregate>())
            {
                return await DeleteManyAsync(expression, isSoftDelete: true, token).ConfigureAwait(false);
            }

            return await RepositoryQuery.Where(expression).ExecuteDeleteAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes entities matching the expression. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching entity must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <remarks>
        /// The soft-delete path fetches matching entities into memory, marks each as deleted, then saves
        /// in a single round-trip. This approach is used instead of <c>ExecuteUpdateAsync</c> with a cast
        /// expression to ensure compatibility across all EF Core database providers.
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
                return await RepositoryQuery.Where(expression).ExecuteDeleteAsync(token).ConfigureAwait(false);
            }

            SoftDeleteHelper.EnsureSoftDeletable<TAggregate>();

            var entities = await this.FindQuery(expression).ToListAsync(token).ConfigureAwait(false);
            foreach (var entity in entities)
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                ObjectSet.Update(entity);
            }
            return await SaveAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task UpdateAsync(TAggregate entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            ObjectSet.Update(entity);
            await SaveAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Core query method that applies the given filter expression to the <see cref="RepositoryQuery"/>.
        /// All find operations delegate to this method to build the filtered queryable.
        /// </summary>
        /// <param name="expression">A predicate expression to filter entities.</param>
        /// <returns>An <see cref="IQueryable{TAggregate}"/> representing the filtered query.</returns>
        /// <exception cref="NullReferenceException">Thrown when <see cref="RepositoryQuery"/> is null.</exception>
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
        public async override Task<long> GetCountAsync(ISpecification<TAggregate> selectSpec, CancellationToken token = default)
        {
            return await FindCore(selectSpec.Predicate).CountAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<long> GetCountAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).CountAsync(token).ConfigureAwait(false);
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

        /// <inheritdoc />
        public override async Task<TAggregate> FindAsync(object primaryKey, CancellationToken token = default)
        {
            var entity = await ObjectSet.FindAsync(new object[] { primaryKey }, token).ConfigureAwait(false);

            // Post-fetch soft-delete check: if the entity was soft-deleted, treat it as not found
            if (entity != null && SoftDeleteHelper.IsSoftDeletable<TAggregate>() && ((ISoftDelete)entity).IsDeleted)
            {
                return default!;
            }

            // Post-fetch tenant check: if the entity belongs to a different tenant, treat it as not found
            var currentTenantId = _tenantIdAccessor.GetTenantId();
            if (entity != null && MultiTenantHelper.IsMultiTenant<TAggregate>()
                && !string.IsNullOrEmpty(currentTenantId)
                && ((IMultiTenant)entity).TenantId != currentTenantId)
            {
                return default!;
            }

            return entity!;
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
        public override IQueryable<TAggregate> FindQuery(IPagedSpecification<TAggregate> specification)
        {
            return this.FindQuery(specification.Predicate, specification.OrderByExpression,
                specification.OrderByAscending, specification.PageNumber, specification.PageSize);
        }

        /// <inheritdoc />
        public override async Task<TAggregate> FindSingleOrDefaultAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            return (await FindCore(expression).SingleOrDefaultAsync(token).ConfigureAwait(false))!;
        }

        /// <inheritdoc />
        public override async Task<TAggregate> FindSingleOrDefaultAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return (await FindCore(specification.Predicate).SingleOrDefaultAsync(token).ConfigureAwait(false))!;
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).AnyAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await FindCore(specification.Predicate).AnyAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the <see cref="RCommonDbContext"/> for the configured data store, resolved through the <see cref="IDataStoreFactory"/>.
        /// </summary>
        protected internal RCommonDbContext ObjectContext
        {
            get
            {
                return this._dataStoreFactory.Resolve<RCommonDbContext>(this.DataStoreName);
            }
        }

        /// <summary>
        /// Persists all pending changes in the <see cref="ObjectContext"/> to the database.
        /// </summary>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>The number of rows affected by the save operation.</returns>
        /// <exception cref="PersistenceException">Thrown when the underlying save operation fails.</exception>
        private async Task<int> SaveAsync(CancellationToken token = default)
        {
            int affected = 0;
            try
            {
                // acceptAllChangesOnSuccess is set to true so EF resets tracking after a successful save
                affected = await ObjectContext.SaveChangesAsync(true, token).ConfigureAwait(false);
            }
            catch (ApplicationException ex)
            {
                var persistEx = new PersistenceException($"Error in {this.GetGenericTypeName()}.SaveAsync while executing on the Context.", ex);
                throw persistEx;
            }

            return affected;
        }
        /// <summary>
        /// Adds a range of transient entities to be tracked and persisted by the repository.
        /// </summary>
        /// <param name="entities">Collection of entities to persist.</param>
        /// <param name="token">Cancellation token.</param>
        public override async Task AddRangeAsync(IEnumerable<TAggregate> entities, CancellationToken token = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            // track each entity and stamp tenant prior to adding
            foreach (var entity in entities)
            {
                EventTracker.AddEntity(entity);
                MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
            }

            await ObjectSet.AddRangeAsync(entities, token).ConfigureAwait(false);
            await SaveAsync(token).ConfigureAwait(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Explicit IAggregateRepository<TAggregate, TKey> implementations
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads an aggregate root by its identity key.
        /// </summary>
        async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.GetByIdAsync(TKey id, CancellationToken cancellationToken)
        {
            return await FilteredRepositoryQuery.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds a single aggregate matching the given specification.
        /// </summary>
        async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken)
        {
            return await FilteredRepositoryQuery.Where(specification.Predicate).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Checks whether an aggregate with the given identity key exists.
        /// </summary>
        async Task<bool> IAggregateRepository<TAggregate, TKey>.ExistsAsync(TKey id, CancellationToken cancellationToken)
        {
            return await FilteredRepositoryQuery.AnyAsync(e => e.Id.Equals(id), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds a new aggregate root to the repository and persists it.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.AddAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            EventTracker.AddEntity(aggregate);
            MultiTenantHelper.SetTenantIdIfApplicable(aggregate, _tenantIdAccessor.GetTenantId());
            await ObjectSet.AddAsync(aggregate, cancellationToken).ConfigureAwait(false);
            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing aggregate root and persists the changes.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            EventTracker.AddEntity(aggregate);
            ObjectSet.Update(aggregate);
            await SaveAsync(cancellationToken).ConfigureAwait(false);
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
                ObjectSet.Update(aggregate);
                await SaveAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            EventTracker.AddEntity(aggregate);
            ObjectSet.Remove(aggregate);
            await SaveAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds an eager-loading include path and returns the aggregate repository for fluent chaining.
        /// </summary>
        IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.Include<TProperty>(Expression<Func<TAggregate, TProperty>> path)
        {
            // Convert to Expression<Func<TAggregate, object>> so it is compatible with the
            // IIncludableQueryable<TAggregate, object> field used by the base Include logic.
            var converted = Expression.Lambda<Func<TAggregate, object>>(
                Expression.Convert(path.Body, typeof(object)), path.Parameters);

            if (_includableQueryable == null)
            {
                _includableQueryable = ObjectContext.Set<TAggregate>().Include(converted);
            }
            else
            {
                _includableQueryable = _includableQueryable.Include(converted);
            }

            return this;
        }

        /// <summary>
        /// Adds a subsequent eager-loading path for a nested navigation property and returns the aggregate repository for fluent chaining.
        /// </summary>
        IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> path)
        {
            // Rewrite the expression from Func<TPreviousProperty, TProperty> to Func<object, TProperty>
            // to match the IIncludableQueryable<TAggregate, object> field type.
            var param = Expression.Parameter(typeof(object), path.Parameters[0].Name);
            var castParam = Expression.Convert(param, typeof(TPreviousProperty));
            var body = ReplacingExpressionVisitor.Replace(path.Parameters[0], castParam, path.Body);
            var converted = Expression.Lambda<Func<object, TProperty>>(body, param);

            _repositoryQuery = _includableQueryable!.ThenInclude(converted);

            return this;
        }
    }
}
