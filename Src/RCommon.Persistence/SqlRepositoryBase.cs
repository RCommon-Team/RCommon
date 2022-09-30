using RCommon.DataServices;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using System.Data;
using Microsoft.Extensions.Logging;
using RCommon.DataServices.Transactions;
using RCommon.DataServices.Sql;
using RCommon.BusinessEntities;
using System.Threading;
using RCommon.Collections;
using Microsoft.Extensions.Options;
using RCommon.Extensions;

namespace RCommon.Persistence
{
    public abstract class SqlRepositoryBase<TEntity> : DisposableResource, ISqlMapperRepository<TEntity>
       where TEntity : class, IBusinessEntity
    {

        public SqlRepositoryBase(IDataStoreProvider dataStoreProvider, ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, 
            IChangeTracker changeTracker, IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
        {
            DataStoreProvider = dataStoreProvider;
            UnitOfWorkManager = unitOfWorkManager;
            ChangeTracker = changeTracker;

            if (defaultDataStoreOptions != null && defaultDataStoreOptions.Value != null 
                && !defaultDataStoreOptions.Value.DefaultDataStoreName.IsNullOrEmpty())
            {
                this.DataStoreName = defaultDataStoreOptions.Value.DefaultDataStoreName;
            }
        }


        public string TableName { get; set; }
        public string DataStoreName { get; set; }

        public abstract Task AddAsync(TEntity entity, CancellationToken token = default);
        public abstract Task DeleteAsync(TEntity entity, CancellationToken token = default);
      
        public abstract Task UpdateAsync(TEntity entity, CancellationToken token = default);
        public abstract Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        public abstract Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);
        public abstract Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);
        public abstract Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);

        protected internal RDbConnection DataStore
        {
            get
            {
                var uow = this.UnitOfWorkManager.CurrentUnitOfWork;
                if (uow != null)
                {
                    return this.DataStoreProvider.GetDataStore<RDbConnection>(uow.TransactionId.Value, this.DataStoreName);

                }
                return this.DataStoreProvider.GetDataStore<RDbConnection>(this.DataStoreName);
            }
        }

        public IDataStoreProvider DataStoreProvider { get; }
        public ILogger Logger { get; set; }
        public IUnitOfWorkManager UnitOfWorkManager { get; }
        public IChangeTracker ChangeTracker { get; }
    }

}
