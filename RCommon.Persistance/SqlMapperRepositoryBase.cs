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

namespace RCommon.Persistance
{
    public abstract class SqlMapperRepositoryBase<TEntity> : DisposableResource, ISqlMapperRepository<TEntity>
       where TEntity : class, IBusinessEntity
    {
        private readonly IDataStoreProvider _dataStoreProvider;
        private readonly ILogger _logger;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        public SqlMapperRepositoryBase(IDataStoreProvider dataStoreProvider, ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager)
        {
            _dataStoreProvider = dataStoreProvider;
            _logger = logger.CreateLogger(this.GetType().Name);
            _unitOfWorkManager = unitOfWorkManager;
        }


        public string TableName { get; set; }
        public string DataStoreName { get; set; }

        public abstract Task AddAsync(TEntity entity);
        public abstract Task DeleteAsync(TEntity entity);
        public abstract Task<ICollection<TEntity>> FindAsync(string sql, IList<Parameter> dbParams, CommandType commandType = CommandType.Text);
        public abstract Task<TEntity> FindSingleOrDefaultAsync(string sql, IList<Parameter> dbParams, CommandType commandType = CommandType.Text);
        public abstract Task UpdateAsync(TEntity entity);

        protected internal IDbConnection DbConnection
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
    }

}
