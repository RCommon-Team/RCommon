using LinqToDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Entities;
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

namespace RCommon.Persistence.Linq2Db.Crud
{
    public class Linq2DbRepository<TEntity> : LinqRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private IQueryable<TEntity> _repositoryQuery;
        private ILoadWithQueryable<TEntity, object> _includableQueryable;
        private readonly IDataStoreFactory _dataStoreFactory;

        public Linq2DbRepository(IDataStoreFactory dataStoreFactory,
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
            _repositoryQuery = null;
            _includableQueryable = null;
            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        }


        protected internal RCommonDataConnection DataConnection
        {
            get
            {
                return this._dataStoreFactory.Resolve<RCommonDataConnection>(this.DataStoreName);
            }
        }

        protected ITable<TEntity> ObjectSet
        {
            get
            {
                return DataConnection.GetTable<TEntity>();
            }
        }

        public override IEagerLoadableQueryable<TEntity> Include(Expression<Func<TEntity, object>> path)
        {
            _includableQueryable = RepositoryQuery.LoadWith(path);
            return this;
        }

        public override IEagerLoadableQueryable<TEntity> ThenInclude<TPreviousProperty, TProperty>(Expression<Func<object, TProperty>> path)
        {
            _repositoryQuery = _includableQueryable.ThenLoad(path);
            return this;
        }


        protected override IQueryable<TEntity> RepositoryQuery
        {
            get
            {
                if (_repositoryQuery == null)
                {
                    _repositoryQuery = ObjectSet.AsQueryable();
                }

                // Start Eagerloading
                if (_includableQueryable != null)
                {
                    _repositoryQuery = _includableQueryable;
                }
                return _repositoryQuery;
            }
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


        public async override Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            await DataConnection.InsertAsync(entity, token: token);
        }

        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await RepositoryQuery.AnyAsync(expression, token: token);
        }

        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await AnyAsync(specification.Predicate, token: token);
        }

        public async override Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            await DataConnection.DeleteAsync(entity);
        }

        public override IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            return FindCore(specification.Predicate);
        }

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
            return await Task.FromResult(query.ToPaginatedList(specification.PageNumber, specification.PageSize));
        }

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

        public override IQueryable<TEntity> FindQuery(IPagedSpecification<TEntity> specification)
        {
            return this.FindQuery(specification.Predicate, specification.OrderByExpression,
                 specification.OrderByAscending, specification.PageNumber, specification.PageSize);
        }

        public async override Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await RepositoryQuery.SingleOrDefaultAsync(expression, token);
        }

        public async override Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindSingleOrDefaultAsync(specification.Predicate, token);
        }

        public async override Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await GetCountAsync(selectSpec.Predicate, token);
        }

        public async override Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await RepositoryQuery.CountAsync(expression, token);
        }

        public async override Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            EventTracker.AddEntity(entity);
            await DataConnection.UpdateAsync(entity, token: token);
        }
    }
}
