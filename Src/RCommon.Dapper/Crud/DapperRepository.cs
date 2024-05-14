using Microsoft.Extensions.Logging;
using RCommon.Persistence.Sql;
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
using RCommon.Entities;
using System.Threading;
using Microsoft.Extensions.Options;
using Dommel;
using RCommon.Collections;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;

namespace RCommon.Persistence.Dapper.Crud
{
    public class DapperRepository<TEntity> : SqlRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {

        public DapperRepository(IDataStoreRegistry dataStoreRegistry, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider,
            ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, IEntityEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
            : base(dataStoreRegistry, dataStoreEnlistmentProvider, logger, unitOfWorkManager, eventTracker, defaultDataStoreOptions)
        {
            Logger = logger.CreateLogger(GetType().Name);
        }

        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    entity.AddLocalEvent(new EntityCreatedEvent<TEntity>(entity));
                    EventTracker.AddEntity(entity);
                    await DispatchEvents();
                    await db.InsertAsync(entity, cancellationToken: token);

                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.AddAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }

            }
        }


        public override async Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    entity.AddLocalEvent(new EntityDeletedEvent<TEntity>(entity));
                    EventTracker.AddEntity(entity);
                    await DispatchEvents();
                    await db.DeleteAsync(entity, cancellationToken: token);
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.DeleteAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }

            }
        }



        public override async Task UpdateAsync(TEntity entity, CancellationToken token = default)
        {

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    entity.AddLocalEvent(new EntityUpdatedEvent<TEntity>(entity));
                    EventTracker.AddEntity(entity);
                    await DispatchEvents();
                    await db.UpdateAsync(entity, cancellationToken: token);
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.UpdateAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }
            }
        }

        public override async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindAsync(specification.Predicate, token);
        }

        public override async Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    var results = await db.SelectAsync(expression, cancellationToken: token);
                    return results.ToList();
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.FindAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }
            }
        }

        public override async Task<TEntity> FindAsync(object primaryKey, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    var result = await db.GetAsync<TEntity>(primaryKey, cancellationToken: token);
                    return result;
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.FindAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }
            }
        }

        public override async Task<long> GetCountAsync(ISpecification<TEntity> selectSpec, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    var results = await db.CountAsync(selectSpec.Predicate);
                    return results;
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.GetCountAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }
            }
        }

        public override async Task<long> GetCountAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    var results = await db.CountAsync(expression);
                    return results;
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.GetCountAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the single returned value based on the expression passed in. 
        /// </summary>
        /// <param name="expression">Custom Expression</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Value matching expression criteria.</returns>
        /// <remarks>Do not use this if querying using primary key. Use <see cref="FindAsync(object, CancellationToken)" instead 
        /// due to issues related to https://github.com/henkmollema/Dommel/issues/282</remarks>
        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            var result = await FindAsync(expression, token);
            return result.SingleOrDefault();
        }

        /// <summary>
        /// Gets the single returned value based on the expression passed in. 
        /// </summary>
        /// <param name="specification">Custom Specification</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Value matching specification expression criteria.</returns>
        /// <remarks>Do not use this if querying using primary key. Use <see cref="FindAsync(object, CancellationToken)" instead
        /// due to issues related to https://github.com/henkmollema/Dommel/issues/282</remarks>
        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindSingleOrDefaultAsync(specification, token);
        }

        public override async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    var results = await db.AnyAsync(expression);
                    return results;
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.AnyAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync();
                    }
                }
            }
        }

        public override async Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await AnyAsync(specification.Predicate, token);
        }

        protected async Task DispatchEvents()
        {
            try
            {
                if (UnitOfWorkManager.CurrentUnitOfWork == null)
                {
                    Guard.Against<NullReferenceException>(DataStore == null, "DataStore is null");
                    await DataStore.PersistChangesAsync(); // This dispatches the events
                }
            }
            catch (ApplicationException exception)
            {
                Logger.LogError(exception, "Error in {0}.DispatchEvents while executing on the Context.", GetType().FullName);
                throw;
            }
        }


    }
}
