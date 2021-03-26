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
using Dapper;
using System.Reflection;
using System.ComponentModel;

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


        

        protected string EntitySetName =>
            this.ObjectContext.GetType().GetProperties().Single<PropertyInfo>(delegate (PropertyInfo p)
            {
                bool flag1;
                if (!p.PropertyType.IsGenericType)
                {
                    flag1 = false;
                }
                else
                {
                    Type[] typeArguments = new Type[] { typeof(TEntity) };
                    flag1 = typeof(IQueryable<>).MakeGenericType(typeArguments).IsAssignableFrom(p.PropertyType);
                }
                return flag1;
            }).Name;

        public async override Task AddAsync(TEntity entity)
        {
            var insertQuery = this.GenerateInsertQuery();

            using (var connection = this.SqlConnectionManager.GetSqlDbConnection("providerName", "connSTring"))
            {
                await connection.ExecuteAsync(insertQuery, entity);
            }
        }

        private string GenerateInsertQuery()
        {
            var insertQuery = new StringBuilder($"INSERT INTO {_tableName} ");

            insertQuery.Append("(");

            var properties = GenerateListOfProperties(GetProperties);
            properties.ForEach(prop => { insertQuery.Append($"[{prop}],"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(") VALUES (");

            properties.ForEach(prop => { insertQuery.Append($"@{prop},"); });

            insertQuery
                .Remove(insertQuery.Length - 1, 1)
                .Append(")");

            return insertQuery.ToString();
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
            //var cmd = this.SqlConnectionManager.GetSqlDbConnection("providerName", "connSTring").CreateCommand();
            //cmd.
        }

        private string GenerateUpdateQuery()
        {
            var updateQuery = new StringBuilder($"UPDATE {_tableName} SET ");
            var properties = GenerateListOfProperties(GetProperties);

            properties.ForEach(property =>
            {
                if (!property.Equals("Id"))
                {
                    updateQuery.Append($"{property}=@{property},");
                }
            });

            updateQuery.Remove(updateQuery.Length - 1, 1); //remove last comma
            updateQuery.Append(" WHERE Id=@Id");

            return updateQuery.ToString();
        }

        protected internal ISqlConnectionManager SqlConnectionManager
        {
            get
            {

                if (this._unitOfWorkManager.CurrentUnitOfWork != null)
                {

                    return this._dataStoreProvider.GetDataStore<ISqlConnectionManager>(this._unitOfWorkManager.CurrentUnitOfWork.TransactionId.Value, this.DataStoreName);

                }
                return this._dataStoreProvider.GetDataStore<ISqlConnectionManager>(this.DataStoreName);
            }
        }

        private static List<string> GenerateListOfProperties(IEnumerable<PropertyInfo> listOfProperties)
        {
            return (from prop in listOfProperties
                    let attributes = prop.GetCustomAttributes(typeof(DescriptionAttribute), false)
                    where attributes.Length <= 0 || (attributes[0] as DescriptionAttribute)?.Description != "ignore"
                    select prop.Name).ToList();
        }

        private IEnumerable<PropertyInfo> GetProperties => typeof(TEntity).GetProperties();


    }
}
