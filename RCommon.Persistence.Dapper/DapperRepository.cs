using Microsoft.Extensions.Logging;
using RCommon.DataServices;
using RCommon.DataServices.Sql;
using RCommon.DataServices.Transactions;
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
using System.Data.Common;
using RCommon.BusinessEntities;
using DapperExtensions;
using DapperSqlMapperExtensions = Dapper.Contrib.Extensions;

namespace RCommon.Persistence.Dapper
{
    public class DapperRepository<TEntity> : SqlMapperRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {
        

        
        public DapperRepository(IDataStoreProvider dataStoreProvider, ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager)
            : base(dataStoreProvider, logger, unitOfWorkManager)
        {
            
        }



        public override async Task AddAsync(TEntity entity)
        {

            using (var connection = this.DbConnection)
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    await connection.InsertAsync(entity);
                    
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
                
            }
        }

       
        public override async Task DeleteAsync(TEntity entity)
        {
            using (var connection = this.DbConnection)
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    await connection.DeleteAsync(entity);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }

            }
        }

        

        public override async Task UpdateAsync(TEntity entity)
        {

            using (var connection = this.DbConnection)
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    await connection.UpdateAsync(entity);
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public override async Task<ICollection<TEntity>> FindAsync(string sql, IList<Parameter> dbParams, CommandType commandType = CommandType.Text)
        {
            
            using (var connection = this.DbConnection)
            {
                var parameters = new DynamicParameters();
                foreach (var p in dbParams)
                {
                    parameters.Add(p.ParameterName, p.Value, p.DbType, p.Direction, p.Size);
                }
                
                var query = await connection.QueryAsync<TEntity>(sql, parameters, commandType: commandType);
                return query.ToList();
            }
        }

        public async Task<TEntity> FindAsync(string sql, object primaryKey, CommandType commandType = CommandType.Text)
        {
            using (var connection = this.DbConnection)
            {
               
                return await connection.QuerySingleOrDefaultAsync<TEntity>(sql, primaryKey, commandType: commandType);
            }
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(string sql, IList<Parameter> dbParams, CommandType commandType = CommandType.Text)
        {
            using (var connection = this.DbConnection)
            {
                
                return await connection.QuerySingleOrDefaultAsync<TEntity>(sql, dbParams, commandType: commandType);
            }
        }


    }
}
