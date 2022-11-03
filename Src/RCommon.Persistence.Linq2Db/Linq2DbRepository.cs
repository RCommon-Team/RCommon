using LinqToDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.BusinessEntities;
using RCommon.Collections;
using RCommon.DataServices;
using RCommon.DataServices.Transactions;
using RCommon.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Linq2Db
{
    public class Linq2DbRepository<TEntity> : LinqRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {

        public Linq2DbRepository(IDataStoreRegistry dataStoreRegistry, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider,
            ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, IEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions) 
            : base(dataStoreRegistry, dataStoreEnlistmentProvider, unitOfWorkManager, eventTracker, defaultDataStoreOptions)
        {
            this.Logger = logger.CreateLogger(this.GetType().Name);
        }


        protected override IQueryable<TEntity> RepositoryQuery => this.ObjectSet.AsQueryable<TEntity>();

        protected internal RCommonDataConnection DataConnection
        {
            get
            {
                 return this.DataStoreRegistry.GetDataStore<RCommonDataConnection>(this.DataStoreName);
            }
        }

        protected ITable<TEntity> ObjectSet
        {
            get
            {
                return this.DataConnection.GetTable<TEntity>();
            }
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


        public async override Task AddAsync(TEntity entity, CancellationToken token = default)
        {
            await this.DataConnection.InsertAsync(entity, token: token);
            entity.AddLocalEvent(new EntityCreatedEvent<TEntity>(entity));
            this.EventTracker.AddEntity(entity);
        }

        public async override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await this.ObjectSet.AnyAsync(expression, token: token);
        }

        public async override Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await this.AnyAsync(specification.Predicate, token: token);
        }

        public async override Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            await this.DataConnection.DeleteAsync(entity);
            entity.AddLocalEvent(new EntityDeletedEvent<TEntity>(entity));
            this.EventTracker.AddEntity(entity);
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
            //TODO: implement FindASync(object primaryKey)
            throw new NotImplementedException();
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

        public async override Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await this.ObjectSet.SingleOrDefaultAsync(expression, token);
        }

        public async override Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await this.FindSingleOrDefaultAsync(specification.Predicate, token);
        }

        public async override Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            return await this.GetCountAsync(selectSpec.Predicate, token);
        }

        public async override Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            return await this.ObjectSet.CountAsync(expression, token);
        }

        public async override Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {
            await this.DataConnection.UpdateAsync(entity, token: token);
            entity.AddLocalEvent(new EntityUpdatedEvent<TEntity>(entity));
            this.EventTracker.AddEntity(entity);
        }
    }
}
