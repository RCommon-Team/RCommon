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
using System.Threading;
using MediatR;

namespace RCommon.Persistence.Dapper
{
    public class DapperRepository<TEntity> : SqlMapperRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {
        private readonly IMediator _mediator;

        public DapperRepository(IDataStoreProvider dataStoreProvider, ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, IChangeTracker changeTracker
            , IMediator mediator)
            : base(dataStoreProvider, logger, unitOfWorkManager, changeTracker)
        {
            _mediator = mediator;
        }



        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {

            using (var connection = this.DbConnection)
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    entity.AddLocalEvent(new EntityCreatedEvent<TEntity>(entity));
                    this.ChangeTracker.AddEntity(entity);
                    await connection.InsertAsync(entity);
                    this.SaveChanges();

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

       
        public override async Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            using (var connection = this.DbConnection)
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    entity.AddLocalEvent(new EntityDeletedEvent<TEntity>(entity));
                    this.ChangeTracker.AddEntity(entity);
                    await connection.DeleteAsync(entity);
                    this.SaveChanges();
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

        

        public override async Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {

            using (var connection = this.DbConnection)
            {
                try
                {
                    if (connection.State == ConnectionState.Closed)
                    {
                        connection.Open();
                    }

                    entity.AddLocalEvent(new EntityUpdatedEvent<TEntity>(entity));
                    this.ChangeTracker.AddEntity(entity);
                    await connection.UpdateAsync(entity);
                    this.SaveChanges();
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

        protected void SaveChanges()
        {
            // We are not actually persisting anything since that is handled by the client
            // , but we need to publish events.
            this.ChangeTracker.TrackedEntities.PublishLocalEvents(_mediator);
        }


    }
}
