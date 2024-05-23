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
using RCommon.Persistence.Sql;
using RCommon.Entities;
using System.Threading;
using RCommon.Collections;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Transactions;

namespace RCommon.Persistence.Crud
{
    public abstract class SqlRepositoryBase<TEntity> : DisposableResource, ISqlMapperRepository<TEntity>
       where TEntity : class, IBusinessEntity
    {
        private string _dataStoreName;
        private readonly IDataStoreEnlistmentProvider _dataStoreEnlistmentProvider;

        public SqlRepositoryBase(IDataStoreRegistry dataStoreRegistry, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider, 
            ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, IEntityEventTracker eventTracker, 
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
        {
            DataStoreRegistry = dataStoreRegistry ?? throw new ArgumentNullException(nameof(dataStoreRegistry));
            _dataStoreEnlistmentProvider = dataStoreEnlistmentProvider ?? throw new ArgumentNullException(nameof(dataStoreEnlistmentProvider));
            UnitOfWorkManager = unitOfWorkManager ?? throw new ArgumentNullException(nameof(unitOfWorkManager));
            EventTracker = eventTracker ?? throw new ArgumentNullException(nameof(eventTracker));

            if (defaultDataStoreOptions != null && defaultDataStoreOptions.Value != null 
                && !defaultDataStoreOptions.Value.DefaultDataStoreName.IsNullOrEmpty())
            {
                this.DataStoreName = defaultDataStoreOptions.Value.DefaultDataStoreName;
            }
        }


        public string TableName { get; set; }

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
                return this.DataStoreRegistry.GetDataStore<RDbConnection>(this.DataStoreName);
            }
        }

        public string DataStoreName
        {
            get => _dataStoreName;
            set
            {
                _dataStoreName = value;
                var dataStore = this.DataStoreRegistry.GetDataStore(_dataStoreName);

                // Enlist Data Stores that are participating in transactions
                if (this.UnitOfWorkManager.IsUnitOfWorkActive)
                {
                    this._dataStoreEnlistmentProvider.EnlistDataStore(this.UnitOfWorkManager.CurrentUnitOfWorkTransactionId, dataStore);
                }
            }
        }

        public IDataStoreRegistry DataStoreRegistry { get; }
        public ILogger Logger { get; set; }
        public IUnitOfWorkManager UnitOfWorkManager { get; }
        public IEntityEventTracker EventTracker { get; }
    }

}
