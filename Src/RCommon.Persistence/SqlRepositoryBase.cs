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

namespace RCommon.Persistence
{
    public abstract class SqlRepositoryBase<TEntity> : DisposableResource, ISqlMapperRepository<TEntity>
       where TEntity : class, IBusinessEntity
    {
        private readonly IDataStoreProvider _dataStoreProvider;
        private readonly ILogger _logger;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public SqlRepositoryBase(IDataStoreProvider dataStoreProvider, ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, IChangeTracker changeTracker)
        {
            _dataStoreProvider = dataStoreProvider;
            _logger = logger.CreateLogger(this.GetType().Name);
            _unitOfWorkManager = unitOfWorkManager;
            ChangeTracker = changeTracker;
        }


        public string TableName { get; set; }
        public string DataStoreName { get; set; }

        public abstract Task AddAsync(TEntity entity, CancellationToken token = default);
        public abstract Task DeleteAsync(TEntity entity, CancellationToken token = default);
      
        public abstract Task UpdateAsync(TEntity entity, CancellationToken token = default);
        public abstract Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        public abstract Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default);
        public abstract Task<int> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default);
        public abstract Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        public abstract Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default);
        public abstract Task<IPaginatedList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, Expression<Func<TEntity, object>> orderByExpression, bool orderByAscending, int? pageIndex, int pageSize = 0, CancellationToken token = default);
        public abstract Task<IPaginatedList<TEntity>> FindAsync(IPagedSpecification<TEntity> specification, CancellationToken token = default);

        protected internal DbConnection DbConnection
        {
            get
            {
                var uow = this._unitOfWorkManager.CurrentUnitOfWork;
                if (uow != null)
                {
                    return this._dataStoreProvider.GetDataStore<RDbConnection>(uow.TransactionId.Value, this.DataStoreName).GetDbConnection();

                }
                return this._dataStoreProvider.GetDataStore<RDbConnection>(this.DataStoreName).GetDbConnection();
            }
        }

        public IChangeTracker ChangeTracker { get; }
    }

}
