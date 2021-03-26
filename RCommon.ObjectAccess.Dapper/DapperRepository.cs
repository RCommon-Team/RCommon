using Microsoft.Extensions.Logging;
using RCommon.DataServices;
using RCommon.DataServices.Sql;
using RCommon.DataServices.Transactions;
using RCommon.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ObjectAccess.Dapper
{
    public class DapperRepository<TEntity> : SqlMapperRepositoryBase<TEntity> where TEntity : class
    {
        private readonly IDataStoreProvider _dataStoreProvider;
        private readonly ILogger _logger;
        private readonly IUnitOfWorkManager _unitOfWorkManager;

        
        public DapperRepository(IDataStoreProvider dataStoreProvider, ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager)
        {
            _dataStoreProvider = dataStoreProvider;
            _logger = logger.CreateLogger(this.GetType().Name);
            _unitOfWorkManager = unitOfWorkManager;
        }


        protected override IQueryable<TEntity> RepositoryQuery => throw new NotImplementedException();

        public override Task AddAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> AnyAsync(ISpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public override Task DeleteAsync(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public override Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public override Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public override Task<TEntity> FindAsync(object primaryKey)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<TEntity> FindQuery(ISpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<TEntity> FindQuery(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public override Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public override Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification)
        {
            throw new NotImplementedException();
        }

        public override Task<int> GetCountAsync(ISpecification<TEntity> selectSpec)
        {
            throw new NotImplementedException();
        }

        public override Task<int> GetCountAsync(Expression<Func<TEntity, bool>> expression)
        {
            throw new NotImplementedException();
        }

        public override Task UpdateAsync(TEntity entity)
        {
            throw new NotImplementedException();
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
                this._logger.LogError(exception, "Error in Repository.FindCore: " + base.GetType().ToString() + " while executing a query on the DataStore.", expression);
                throw new RepositoryException("Error in Repository: " + base.GetType().ToString() + " while executing a query on the DataStore.", exception.GetBaseException());
            }
            return queryable;
        }

        protected internal IDbConnection DbConnection
        {
            get
            {

                if (this._unitOfWorkManager.CurrentUnitOfWork != null)
                {

                    return this._dataStoreProvider.GetDataStore<IDbConnection>(this._unitOfWorkManager.CurrentUnitOfWork.TransactionId.Value, this.DataStoreName);

                }
                return this._dataStoreProvider.GetDataStore<IDbConnection>(this.DataStoreName);
            }
        }
    }
}
