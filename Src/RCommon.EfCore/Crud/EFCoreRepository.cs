using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon;
using RCommon.Entities;
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
    /// A concrete repository implementation for Entity Framework Core that supports CRUD operations,
    /// LINQ queries, eager loading, and graph-based entity navigation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository. Must implement <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// Exposes much of the EF Core functionality with the exception of direct change tracking and persistence models.
    /// Exposes <see cref="IQueryable{T}"/> to downstream layers so that complex joins can be utilized and managed at the domain level.
    /// This implementation makes special considerations for managing the lifetime of the <see cref="DbContext"/>
    /// specifically when it applies to the <see cref="UnitOfWorkScope"/>.
    /// </remarks>
    public class EFCoreRepository<TEntity> : GraphRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private IQueryable<TEntity>? _repositoryQuery;
        private bool _tracking;
        private IIncludableQueryable<TEntity, object>? _includableQueryable;
        private readonly IDataStoreFactory _dataStoreFactory;



        /// <summary>
        /// Initializes a new instance of <see cref="EFCoreRepository{TEntity}"/>.
        /// </summary>
        /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDbContext"/> for the configured data store.</param>
        /// <param name="logger">Factory for creating loggers scoped to this repository type.</param>
        /// <param name="eventTracker">Tracker used to register entities for domain event dispatching.</param>
        /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
        public EFCoreRepository(IDataStoreFactory dataStoreFactory,
            ILoggerFactory logger, IEntityEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
            : base(dataStoreFactory, eventTracker, defaultDataStoreOptions)
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
        /// Gets the <see cref="DbSet{TEntity}"/> from the current <see cref="ObjectContext"/> for direct entity set operations.
        /// </summary>
        protected DbSet<TEntity> ObjectSet
        {
            get
            {
                return ObjectContext.Set<TEntity>();
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
        public override IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            // On first call, start from the DbSet; on subsequent calls, chain from the existing includable query
            if (_includableQueryable == null)
            {
                _includableQueryable = ObjectContext.Set<TEntity>().Include(path);
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
        public override IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            // TODO: This is likely a bug. The receiver is incorrect.
            _repositoryQuery = _includableQueryable!.ThenInclude(path);
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
                    _repositoryQuery = ObjectSet.AsQueryable<TEntity>();
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
        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            await ObjectSet.AddAsync(entity, token);
            await SaveAsync(token);
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
            ObjectSet.Remove(entity);
            await SaveAsync();
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
                ObjectSet.Remove(entity);
                await SaveAsync();
                return;
            }

            SoftDeleteHelper.EnsureSoftDeletable<TEntity>();
            SoftDeleteHelper.MarkAsDeleted(entity);
            await UpdateAsync(entity, token);
        }

        /// <summary>
        /// Deletes entities matching the specification. If <typeparamref name="TEntity"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically.
        /// </summary>
        public async override Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await this.DeleteManyAsync(specification.Predicate, token);
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
            return await this.DeleteManyAsync(specification.Predicate, isSoftDelete, token);
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

            return await RepositoryQuery.Where(expression).ExecuteDeleteAsync(token);
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
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        {
            if (!isSoftDelete)
            {
                // Bypass auto-detection and soft-delete filter — force a physical delete
                return await RepositoryQuery.Where(expression).ExecuteDeleteAsync(token);
            }

            SoftDeleteHelper.EnsureSoftDeletable<TEntity>();

            var entities = await this.FindQuery(expression).ToListAsync(token);
            foreach (var entity in entities)
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                ObjectSet.Update(entity);
            }
            return await SaveAsync(token);
        }

        /// <inheritdoc />
        public async override Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            ObjectSet.Update(entity);
            await SaveAsync(token);
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
        public async override Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await FindCore(selectSpec.Predicate).CountAsync(token);
        }

        /// <inheritdoc />
        public async override Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).CountAsync(token);
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

        /// <inheritdoc />
        public override async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            var entity = await ObjectSet.FindAsync(new object[] { primaryKey }, token);

            // Post-fetch soft-delete check: if the entity was soft-deleted, treat it as not found
            if (entity != null && SoftDeleteHelper.IsSoftDeletable<TEntity>() && ((ISoftDelete)entity).IsDeleted)
            {
                return default!;
            }

            return entity!;
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
        public override IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification)
        {
            return this.FindQuery(specification.Predicate, specification.OrderByExpression,
                specification.OrderByAscending, specification.PageNumber, specification.PageSize);
        }

        /// <inheritdoc />
        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return (await FindCore(expression).SingleOrDefaultAsync(token))!;
        }

        /// <inheritdoc />
        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return (await FindCore(specification.Predicate).SingleOrDefaultAsync(token))!;
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).AnyAsync(token);
        }

        /// <inheritdoc />
        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindCore(specification.Predicate).AnyAsync(token);
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
                affected = await ObjectContext.SaveChangesAsync(true, token);
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
        public override async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            // track each entity prior to adding
            foreach (var entity in entities)
            {
                EventTracker.AddEntity(entity);
            }

            await ObjectSet.AddRangeAsync(entities, token);
            await SaveAsync(token);
        }
    }
}

