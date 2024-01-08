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
using RCommon.Persistence.Transactions;
using RCommon.Persistence.Crud;

namespace RCommon.Persistence.EFCore.Crud
{

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
            ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, ILocalEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
            : base(dataStoreRegistry, dataStoreEnlistmentProvider, unitOfWorkManager, eventTracker, defaultDataStoreOptions)
        {
            Logger = logger.CreateLogger(GetType().Name);
            _repositoryQuery = null;
            _includableQueryable = null;
            _tracking = true;
            _objectSets = new Dictionary<Type, object>();
        }

        protected DbSet<TEntity> ObjectSet
        {
            get
            {
                return ObjectContext.Set<TEntity>();
            }
        }

        public override bool Tracking
        {
            get => _tracking;
            set
            {
                _tracking = value;
            }

        }

        public override IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            _includableQueryable = RepositoryQuery.Include(path);
            return this;
        }

        public override IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            _repositoryQuery = _includableQueryable.ThenInclude(path);
            return this;
        }

        protected override IQueryable<TEntity> RepositoryQuery
        {
            get
            {
                if (_repositoryQuery == null)
                {
                    _repositoryQuery = ObjectSet.AsQueryable<TEntity>();
                }

                // Start Eagerloading
                if (_includableQueryable != null)
                {
                    _repositoryQuery = _includableQueryable;
                }
                return _repositoryQuery;
            }
        }

        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await ObjectSet.AddAsync(entity, token);
            entity.AddLocalEvent(new EntityCreatedEvent<TEntity>(entity));
            EventTracker.AddEntity(entity);
            await SaveAsync(token);
        }


        public async override Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            ObjectSet.Remove(entity);
            entity.AddLocalEvent(new EntityDeletedEvent<TEntity>(entity));
            EventTracker.AddEntity(entity);
            await SaveAsync();
        }

        public async override Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            ObjectSet.Update(entity);
            entity.AddLocalEvent(new EntityUpdatedEvent<TEntity>(entity));
            EventTracker.AddEntity(entity);
            await SaveAsync(token);
        }

        private IQueryable<TEntity> FindCore(Expression<Func<TEntity, bool>> expression)
        {
            IQueryable<TEntity> queryable;
            try
            {
                Guard.Against<NullReferenceException>(RepositoryQuery == null, "RepositoryQuery is null");
                queryable = RepositoryQuery.Where(expression);
            }
            catch (ApplicationException exception)
            {
                Logger.LogError(exception, "Error in {0}.FindCore while executing a query on the Context.", GetType().FullName);
                throw;
            }
            return queryable;
        }

        public async override Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await FindCore(selectSpec.Predicate).CountAsync(token);
        }

        public async override Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).CountAsync(token);
        }

        public override IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return FindCore(specification.Predicate);
        }

        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            return FindCore(expression);
        }

        public override async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            return await ObjectSet.FindAsync(new object[] { primaryKey }, token);
        }

        public async override Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindCore(specification.Predicate).ToListAsync(token);
        }

        public async override Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).ToListAsync(token);
        }

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
            return await Task.FromResult(query.ToPaginatedList(specification.PageIndex, specification.PageSize));
        }

        public async override Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression,
            bool orderByAscending, int? pageIndex, int pageSize = 1,
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
            return await Task.FromResult(query.ToPaginatedList(pageIndex, pageSize));
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).SingleOrDefaultAsync(token);
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindCore(specification.Predicate).SingleOrDefaultAsync(token);
        }

        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await FindCore(expression).AnyAsync(token);
        }

        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindCore(specification.Predicate).AnyAsync(token);
        }

        protected internal RCommonDbContext ObjectContext
        {
            get
            {
                return DataStoreRegistry.GetDataStore<RCommonDbContext>(DataStoreName);
            }
        }

        private async Task<int> SaveAsync(CancellationToken token = default)
        {
            int affected = 0;
            try
            {
                if (UnitOfWorkManager.CurrentUnitOfWork == null)
                {
                    affected = await ObjectContext.SaveChangesAsync(true, token); // This will dispatch the local events
                }
            }
            catch (ApplicationException exception)
            {
                Logger.LogError(exception, "Error in {0}.SaveAsync while executing on the Context.", GetType().FullName);
                throw;
            }

            return affected;
        }
    }
}

