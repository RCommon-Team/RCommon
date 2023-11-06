namespace RCommon.Persistence.EFCore
{
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using RCommon;
    using RCommon.BusinessEntities;
    using RCommon.Collections;
    using RCommon.DataServices;
    using RCommon.DataServices.Transactions;
    using RCommon.Extensions;
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

    /// <summary>
    /// A concrete implementation for Entity Framework Core.
    /// currently exposes much of the functionality of EF with the exception of change tracking and peristance models. We expose IQueryable to layers down stream
    /// so that complex joins can be utilized and then managed at the domain level. This implementation makes special considerations for managing the lifetime of the
    /// <see cref="DbContext"/> specifically when it applies to the <see cref="UnitOfWorkScope"/>. 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EFCoreRepository<TEntity> : GraphRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private readonly Dictionary<Type, object> _objectSets;
        private IQueryable<TEntity> _repositoryQuery;
        private bool _tracking;
        private IIncludableQueryable<TEntity, object> _includableQueryable;



        /// <summary>
        /// The default constructor for the repository. 
        /// </summary>
        /// <param name="dbContext">The <see cref="TDataStore"/> is injected with scoped lifetime so it will always return the same instance of the <see cref="DbContext"/>
        /// througout the HTTP request or the scope of the thread.</param>
        /// <param name="logger">Logger used throughout the application.</param>
        public EFCoreRepository(IDataStoreRegistry dataStoreRegistry, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider, 
            ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, IEventTracker eventTracker, 
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions) 
            : base(dataStoreRegistry, dataStoreEnlistmentProvider, unitOfWorkManager, eventTracker, defaultDataStoreOptions)
        {
            this.Logger = logger.CreateLogger(this.GetType().Name);
            this._repositoryQuery = null;
            this._includableQueryable = null;
            this._tracking = true;
            this._objectSets = new Dictionary<Type, object>();
        }

        protected DbSet<TEntity> ObjectSet
        {
            get
            {
                return this.ObjectContext.Set<TEntity>();
            }
        }

        public override bool Tracking
        {
            get => this._tracking;
            set
            {
                this._tracking = value;
            }

        }

        public override IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            this._includableQueryable = this.RepositoryQuery.Include(path);
            return this;
        }

        public override IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            this._repositoryQuery = this._includableQueryable.ThenInclude(path);
            return this;
        }

        protected override IQueryable<TEntity> RepositoryQuery
        {
            get
            {
                if (this._repositoryQuery == null)
                {
                    this._repositoryQuery = this.ObjectSet.AsQueryable<TEntity>();
                }

                // Start Eagerloading
                if (this._includableQueryable != null)
                {
                    this._repositoryQuery = this._includableQueryable;
                }
                return this._repositoryQuery;
            }
        }

        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await this.ObjectSet.AddAsync(entity, token);
            entity.AddLocalEvent(new EntityCreatedEvent<TEntity>(entity));
            this.EventTracker.AddEntity(entity);
            await this.SaveAsync(token);
        }


        public async override Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            this.ObjectSet.Remove(entity);
            entity.AddLocalEvent(new EntityDeletedEvent<TEntity>(entity));
            this.EventTracker.AddEntity(entity);
            await this.SaveAsync();
        }

        public async override Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            this.ObjectSet.Update(entity);
            entity.AddLocalEvent(new EntityUpdatedEvent<TEntity>(entity));
            this.EventTracker.AddEntity(entity);
            await this.SaveAsync(token);
        }

        private IQueryable<TEntity> FindCore(Expression<Func<TEntity, bool>> expression)
        {
            IQueryable<TEntity> queryable;
            try
            {
                Guard.Against<NullReferenceException>(this.RepositoryQuery == null, "RepositoryQuery is null");
                queryable = this.RepositoryQuery.Where<TEntity>(expression);
            }
            catch (ApplicationException exception)
            {
                this.Logger.LogError(exception, "Error in {0}.FindCore while executing a query on the Context.", this.GetType().FullName);
                throw;
            }
            return queryable;
        }

        public async override Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await this.FindCore(selectSpec.Predicate).CountAsync(token);
        }

        public async override Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await this.FindCore(expression).CountAsync(token);
        }

        public override IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return this.FindCore(specification.Predicate);
        }

        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            return this.FindCore(expression);
        }

        public override async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            return await this.ObjectSet.FindAsync(new object[] { primaryKey }, token);
        }

        public async override Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await this.FindCore(specification.Predicate).ToListAsync(token);
        }

        public async override Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await this.FindCore(expression).ToListAsync(token);
        }

        public async override Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default)
        {
            IQueryable<TEntity> query;
            if (specification.OrderByAscending)
            {
                query = this.FindCore(specification.Predicate).OrderBy(specification.OrderByExpression);
            }
            else
            {
                query = this.FindCore(specification.Predicate).OrderByDescending(specification.OrderByExpression);
            }
            return await Task.FromResult(query.ToPaginatedList(specification.PageIndex, specification.PageSize));
        }

        public async override Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int? pageIndex, int pageSize = 1, 
            CancellationToken token = default)
        {
            IQueryable<TEntity> query;
            if (orderByAscending)
            {
                query = this.FindCore(expression).OrderBy(orderByExpression);
            }
            else
            {
                query = this.FindCore(expression).OrderByDescending(orderByExpression);
            }
            return await Task.FromResult(query.ToPaginatedList(pageIndex, pageSize));
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await this.FindCore(expression).SingleOrDefaultAsync(token);
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await this.FindCore(specification.Predicate).SingleOrDefaultAsync(token);
        }

        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await this.FindCore(expression).AnyAsync(token);
        }

        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await this.FindCore(specification.Predicate).AnyAsync(token);
        }

        protected internal RCommonDbContext ObjectContext
        {
            get
            {
                return this.DataStoreRegistry.GetDataStore<RCommonDbContext>(this.DataStoreName);
            }
        }

        private async Task<int> SaveAsync(CancellationToken token = default)
        {
            int affected = 0;
            try
            {
                if (this.UnitOfWorkManager.CurrentUnitOfWork == null)
                {
                    affected = await this.ObjectContext.SaveChangesAsync(true, token); // This will dispatch the local events
                }
            }
            catch (ApplicationException exception)
            {
                this.Logger.LogError(exception, "Error in {0}.SaveAsync while executing on the Context.", this.GetType().FullName);
                throw;
            }
            
            return affected;
        }
    }
}

