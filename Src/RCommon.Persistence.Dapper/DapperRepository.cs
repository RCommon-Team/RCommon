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
using System.Threading;
using MediatR;
using Microsoft.Extensions.Options;
using Dommel;
using RCommon.Collections;

namespace RCommon.Persistence.Dapper
{
    public class DapperRepository<TEntity> : SqlRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {

        public DapperRepository(IDataStoreRegistry dataStoreRegistry, IDataStoreEnlistmentProvider dataStoreEnlistmentProvider, 
            ILoggerFactory logger, IUnitOfWorkManager unitOfWorkManager, IEventTracker eventTracker, 
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
            : base(dataStoreRegistry, dataStoreEnlistmentProvider, logger, unitOfWorkManager, eventTracker, defaultDataStoreOptions)
        {
            this.Logger = logger.CreateLogger(this.GetType().Name);
        }

        public override async Task AddAsync(TEntity entity, CancellationToken token = default)
        {

            await using (var db = this.DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    entity.AddLocalEvent(new EntityCreatedEvent<TEntity>(entity));
                    this.EventTracker.AddEntity(entity);
                    this.DispatchEvents();
                    await db.InsertAsync(entity, cancellationToken: token);

                }
                catch (ApplicationException exception)
                {
                    this.Logger.LogError(exception, "Error in {0}.AddAsync while executing on the DbConnection.", this.GetType().FullName);
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
            await using (var db = this.DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    entity.AddLocalEvent(new EntityDeletedEvent<TEntity>(entity));
                    this.EventTracker.AddEntity(entity);
                    this.DispatchEvents();
                    await db.DeleteAsync(entity, cancellationToken: token);
                }
                catch (ApplicationException exception)
                {
                    this.Logger.LogError(exception, "Error in {0}.DeleteAsync while executing on the DbConnection.", this.GetType().FullName);
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

            await using (var db = this.DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    entity.AddLocalEvent(new EntityUpdatedEvent<TEntity>(entity));
                    this.EventTracker.AddEntity(entity);
                    this.DispatchEvents();
                    await db.UpdateAsync(entity, cancellationToken: token);
                }
                catch (ApplicationException exception)
                {
                    this.Logger.LogError(exception, "Error in {0}.UpdateAsync while executing on the DbConnection.", this.GetType().FullName);
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
            return await this.FindAsync(specification.Predicate, token);
        }

        public override async Task<ICollection<TEntity>> FindAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            await using (var db = this.DataStore.GetDbConnection())
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
                    this.Logger.LogError(exception, "Error in {0}.FindAsync while executing on the DbConnection.", this.GetType().FullName);
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
            await using (var db = this.DataStore.GetDbConnection())
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
                    this.Logger.LogError(exception, "Error in {0}.FindAsync while executing on the DbConnection.", this.GetType().FullName);
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
            await using (var db = this.DataStore.GetDbConnection())
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
                    this.Logger.LogError(exception, "Error in {0}.GetCountAsync while executing on the DbConnection.", this.GetType().FullName);
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
            await using (var db = this.DataStore.GetDbConnection())
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
                    this.Logger.LogError(exception, "Error in {0}.GetCountAsync while executing on the DbConnection.", this.GetType().FullName);
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

        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            await using (var db = this.DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }
;
                    var result = await db.FirstOrDefaultAsync(expression, cancellationToken: token);
                    return result;
                }
                catch (ApplicationException exception)
                {
                    this.Logger.LogError(exception, "Error in {0}.FindSingleOrDefaultAsync while executing on the DbConnection.", this.GetType().FullName);
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

        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindSingleOrDefaultAsync(specification, token);
        }

        public override async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            await using (var db = this.DataStore.GetDbConnection())
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
                    this.Logger.LogError(exception, "Error in {0}.AnyAsync while executing on the DbConnection.", this.GetType().FullName);
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
            return await this.AnyAsync(specification.Predicate, token);
        }

        protected void DispatchEvents()
        {
            try
            {
                if (this.UnitOfWorkManager.CurrentUnitOfWork == null)
                {
                    Guard.Against<NullReferenceException>(this.DataStore == null, "DataStore is null");
                    this.DataStore.PersistChanges(); // This dispatches the events
                }
            }
            catch (ApplicationException exception)
            {
                this.Logger.LogError(exception, "Error in {0}.DispatchEvents while executing on the Context.", this.GetType().FullName);
                throw;
            }
        }

        
    }
}
