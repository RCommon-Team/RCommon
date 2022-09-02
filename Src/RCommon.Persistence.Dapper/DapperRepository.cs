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
using System.Threading;
using MediatR;
using DapperExtensions.Sql;
using DapperExtensions.Mapper;

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



        protected virtual AsyncDatabase GetAsyncDatabase(DbConnection connection, SqlDialectBase sqlDialect)
        {
            var config = new DapperExtensionsConfiguration(typeof(PluralizedAutoClassMapper<>), new List<Assembly>(), sqlDialect);
            var sqlGenerator = new SqlGeneratorImpl(config);
            var db = new AsyncDatabase(connection, sqlGenerator);
            return db;
        }



        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {

            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {
                try
                {
                    if (db.Connection.State == ConnectionState.Closed)
                    {
                        db.Connection.Open();
                    }

                    entity.AddLocalEvent(new EntityCreatedEvent<TEntity>(entity));
                    this.ChangeTracker.AddEntity(entity);
                    await db.Insert(entity, 30);
                    this.SaveChanges();

                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (db.Connection.State == ConnectionState.Open)
                    {
                        db.Connection.Close();
                    }
                }

            }
        }


        public override async Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {
                try
                {
                    if (db.Connection.State == ConnectionState.Closed)
                    {
                        db.Connection.Open();
                    }

                    entity.AddLocalEvent(new EntityDeletedEvent<TEntity>(entity));
                    this.ChangeTracker.AddEntity(entity);
                    await db.Delete(entity, 30);
                    this.SaveChanges();
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (db.Connection.State == ConnectionState.Open)
                    {
                        db.Connection.Close();
                    }
                }

            }
        }



        public override async Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {

            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {
                try
                {
                    if (db.Connection.State == ConnectionState.Closed)
                    {
                        db.Connection.Open();
                    }

                    entity.AddLocalEvent(new EntityUpdatedEvent<TEntity>(entity));
                    this.ChangeTracker.AddEntity(entity);
                    await db.Update(entity, 30, false);
                    this.SaveChanges();
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    if (db.Connection.State == ConnectionState.Open)
                    {
                        db.Connection.Close();
                    }
                }
            }
        }

        public override async Task<ICollection<TEntity>> FindAsync(string sql, IList<RCommon.DataServices.Sql.Parameter> dbParams, CommandType commandType = CommandType.Text)
        {

            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {
                var parameters = new DynamicParameters();
                foreach (var p in dbParams)
                {
                    parameters.Add(p.ParameterName, p.Value, p.DbType, p.Direction, p.Size);
                }
                var query = await db.Connection.QueryAsync<TEntity>(sql, parameters, commandType: commandType);
                return query.ToList();
            }
        }

        public async Task<TEntity> FindAsync(object primaryKey)
        {
            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {

                return await db.Get<TEntity>(primaryKey, 30);
            }
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(object primaryKey)
        {
            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {

                return await db.Get<TEntity>(primaryKey, 30);
            }
        }

        public override async Task<TEntity> FindAsync(string sql, object primaryKey, CommandType commandType = CommandType.Text)
        {
            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {
               
                return await db.Connection.QuerySingleOrDefaultAsync<TEntity>(sql, primaryKey, commandType: commandType);
            }
        }

        public override async Task<TEntity> FindSingleOrDefaultAsync(string sql, IList<RCommon.DataServices.Sql.Parameter> dbParams, CommandType commandType = CommandType.Text)
        {
            using (var db = this.GetAsyncDatabase(this.DbConnection, new SqlServerDialect()))
            {
                
                return await db.Connection.QuerySingleOrDefaultAsync<TEntity>(sql, dbParams, commandType: commandType);
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
