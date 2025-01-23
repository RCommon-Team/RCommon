﻿using System.Collections;
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
        private readonly IDataStoreFactory _dataStoreFactory;

        public SqlRepositoryBase(IDataStoreFactory dataStoreFactory, 
            ILoggerFactory logger, IEntityEventTracker eventTracker, 
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (defaultDataStoreOptions is null)
            {
                throw new ArgumentNullException(nameof(defaultDataStoreOptions));
            }

            _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
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
        public abstract Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default);
        public abstract Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default);
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
                return this._dataStoreFactory.Resolve<RDbConnection>(this.DataStoreName);
            }
        }

        public string DataStoreName
        {
            get => _dataStoreName;
            set
            {
                _dataStoreName = value;
            }
        }

        public ILogger Logger { get; set; }
        public IEntityEventTracker EventTracker { get; }
    }

}
