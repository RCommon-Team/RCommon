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
using RCommon.Security.Claims;
using System.Threading;
using Microsoft.Extensions.Options;
using Dommel;
using RCommon.Collections;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Transactions;
using static Dapper.SqlMapper;

namespace RCommon.Persistence.Dapper.Crud
{
    /// <summary>
    /// A concrete repository implementation using Dapper and the Dommel extension library for CRUD operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type managed by this repository. Must implement <see cref="IBusinessEntity"/>.</typeparam>
    /// <remarks>
    /// Each operation acquires a <see cref="DbConnection"/> from the configured <see cref="IDataStore"/>,
    /// ensures it is open before executing, and closes it in a <c>finally</c> block. This repository
    /// uses Dommel's extension methods (e.g., <c>InsertAsync</c>, <c>DeleteAsync</c>, <c>SelectAsync</c>)
    /// for SQL generation from entity mappings.
    /// </remarks>
    public class DapperRepository<TEntity> : SqlRepositoryBase<TEntity>
        where TEntity : class, IBusinessEntity
    {

        /// <summary>
        /// Initializes a new instance of <see cref="DapperRepository{TEntity}"/>.
        /// </summary>
        /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RDbConnection"/> for the configured data store.</param>
        /// <param name="logger">Factory for creating loggers scoped to this repository type.</param>
        /// <param name="eventTracker">Tracker used to register entities for domain event dispatching.</param>
        /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
        public DapperRepository(IDataStoreFactory dataStoreFactory,
            ILoggerFactory logger, IEntityEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
            ITenantIdAccessor tenantIdAccessor)
            : base(dataStoreFactory, logger, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
        {
            Logger = logger.CreateLogger(GetType().Name);
        }

        /// <inheritdoc />
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
                    EventTracker.AddEntity(entity);
                    MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
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


        /// <summary>
        /// Deletes the entity. If <typeparamref name="TEntity"/> implements <see cref="ISoftDelete"/>,
        /// a soft delete is performed automatically (sets <c>IsDeleted = true</c> and issues an UPDATE).
        /// Otherwise a physical DELETE is executed.
        /// </summary>
        public override async Task DeleteAsync(TEntity entity, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TEntity>())
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                await UpdateAsync(entity, token);
                return;
            }

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    EventTracker.AddEntity(entity);
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

        /// <summary>
        /// Deletes entities matching the expression. If <typeparamref name="TEntity"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically (marks each matching
        /// entity as deleted and issues UPDATEs). Otherwise a physical DELETE is executed.
        /// </summary>
        public async override Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TEntity>())
            {
                return await DeleteManyAsync(expression, isSoftDelete: true, token);
            }

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    return await db.DeleteMultipleAsync(expression, cancellationToken: token);
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

        /// <summary>
        /// Deletes entities matching the specification. If <typeparamref name="TEntity"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically.
        /// </summary>
        public async override Task<int> DeleteManyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, token);
        }

        /// <summary>
        /// Deletes the entity using the explicitly specified delete mode. When <paramref name="isSoftDelete"/>
        /// is <c>true</c>, the entity must implement <see cref="ISoftDelete"/>; its <c>IsDeleted</c> property
        /// is set to <c>true</c> and an UPDATE is issued. When <c>false</c>, a physical DELETE is always
        /// performed — even if the entity implements <see cref="ISoftDelete"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public override async Task DeleteAsync(TEntity entity, bool isSoftDelete, CancellationToken token = default)
        {
            if (!isSoftDelete)
            {
                // Bypass auto-detection — force a physical delete
                await using (var db = DataStore.GetDbConnection())
                {
                    try
                    {
                        if (db.State == ConnectionState.Closed)
                        {
                            await db.OpenAsync();
                        }

                        EventTracker.AddEntity(entity);
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
                return;
            }

            SoftDeleteHelper.EnsureSoftDeletable<TEntity>();
            SoftDeleteHelper.MarkAsDeleted(entity);
            await UpdateAsync(entity, token);
        }

        /// <summary>
        /// Deletes entities matching the expression. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching entity must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <remarks>
        /// The soft-delete path selects matching entities, marks each as deleted, then updates them
        /// one by one via Dommel's <c>UpdateAsync</c>. This is consistent with Dapper/Dommel's
        /// per-entity operation model (there is no bulk update-by-expression in Dommel).
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(Expression<Func<TEntity, bool>> expression, bool isSoftDelete, CancellationToken token = default)
        {
            if (!isSoftDelete)
            {
                // Bypass auto-detection — force a physical delete
                await using (var db = DataStore.GetDbConnection())
                {
                    try
                    {
                        if (db.State == ConnectionState.Closed)
                        {
                            await db.OpenAsync();
                        }

                        return await db.DeleteMultipleAsync(expression, cancellationToken: token);
                    }
                    catch (ApplicationException exception)
                    {
                        Logger.LogError(exception, "Error in {0}.DeleteManyAsync while executing on the DbConnection.", GetType().FullName);
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

            SoftDeleteHelper.EnsureSoftDeletable<TEntity>();

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    var entities = (await db.SelectAsync(expression, cancellationToken: token)).ToList();
                    int count = 0;
                    foreach (var entity in entities)
                    {
                        SoftDeleteHelper.MarkAsDeleted(entity);
                        await db.UpdateAsync(entity, cancellationToken: token);
                        count++;
                    }
                    return count;
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.DeleteManyAsync (soft delete) while executing on the DbConnection.", GetType().FullName);
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
        /// Deletes entities matching the specification. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching entity must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TEntity"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public async override Task<int> DeleteManyAsync(ISpecification<TEntity> specification, bool isSoftDelete, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, isSoftDelete, token);
        }


        /// <inheritdoc />
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

                    EventTracker.AddEntity(entity);
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

        /// <inheritdoc />
        public override async Task<ICollection<TEntity>> FindAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindAsync(specification.Predicate, token);
        }

        /// <inheritdoc />
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

                    var filteredExpression = SoftDeleteHelper.CombineWithNotDeletedFilter<TEntity>(expression);
                    filteredExpression = MultiTenantHelper.CombineWithTenantFilter(filteredExpression, _tenantIdAccessor.GetTenantId());
                    var results = await db.SelectAsync(filteredExpression, cancellationToken: token);
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

        /// <inheritdoc />
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

                    // Post-fetch soft-delete check: if the entity was soft-deleted, treat it as not found
                    if (result != null && SoftDeleteHelper.IsSoftDeletable<TEntity>() && ((ISoftDelete)result).IsDeleted)
                    {
                        return default!;
                    }

                    // Post-fetch tenant check: if the entity belongs to a different tenant, treat it as not found
                    var currentTenantId = _tenantIdAccessor.GetTenantId();
                    if (result != null && MultiTenantHelper.IsMultiTenant<TEntity>()
                        && !string.IsNullOrEmpty(currentTenantId)
                        && ((IMultiTenant)result).TenantId != currentTenantId)
                    {
                        return default!;
                    }

                    return result!;
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

        /// <inheritdoc />
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

                    var filteredPredicate = SoftDeleteHelper.CombineWithNotDeletedFilter<TEntity>(selectSpec.Predicate);
                    filteredPredicate = MultiTenantHelper.CombineWithTenantFilter(filteredPredicate, _tenantIdAccessor.GetTenantId());
                    var results = await db.CountAsync(filteredPredicate);
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

        /// <inheritdoc />
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

                    var filteredExpression = SoftDeleteHelper.CombineWithNotDeletedFilter<TEntity>(expression);
                    filteredExpression = MultiTenantHelper.CombineWithTenantFilter(filteredExpression, _tenantIdAccessor.GetTenantId());
                    var results = await db.CountAsync(filteredExpression);
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
        /// <remarks>Do not use this if querying using primary key. Use <see cref="FindAsync(object, CancellationToken)"/> instead
        /// due to issues related to https://github.com/henkmollema/Dommel/issues/282</remarks>
        public override async Task<TEntity> FindSingleOrDefaultAsync(Expression<Func<TEntity, bool>> expression, CancellationToken token = default)
        {
            // Dommel lacks a native SingleOrDefault, so we retrieve all matches and apply SingleOrDefault in-memory
            var result = await FindAsync(expression, token);
            return result.SingleOrDefault()!;
        }

        /// <summary>
        /// Gets the single returned value based on the expression passed in.
        /// </summary>
        /// <param name="specification">Custom Specification</param>
        /// <param name="token">Cancellation Token</param>
        /// <returns>Value matching specification expression criteria.</returns>
        /// <remarks>Do not use this if querying using primary key. Use <see cref="FindAsync(object, CancellationToken)"/> instead
        /// due to issues related to https://github.com/henkmollema/Dommel/issues/282</remarks>
        public override async Task<TEntity> FindSingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await FindSingleOrDefaultAsync(specification, token);
        }

        /// <inheritdoc />
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

                    var filteredExpression = SoftDeleteHelper.CombineWithNotDeletedFilter<TEntity>(expression);
                    filteredExpression = MultiTenantHelper.CombineWithTenantFilter(filteredExpression, _tenantIdAccessor.GetTenantId());
                    var results = await db.AnyAsync(filteredExpression);
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

        /// <inheritdoc />
        public override async Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken token = default)
        {
            return await AnyAsync(specification.Predicate, token);
        }

        /// <summary>
        /// Adds a range of transient entities to be persisted using Dapper.
        /// </summary>
        /// <param name="entities">Collection of entities to persist.</param>
        /// <param name="token">Cancellation token.</param>
        public override async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync();
                    }

                    foreach (var entity in entities)
                    {
                        EventTracker.AddEntity(entity);
                        MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
                        await db.InsertAsync(entity, cancellationToken: token);
                    }
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.AddRangeAsync while executing on the DbConnection.", GetType().FullName);
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
    }
}
