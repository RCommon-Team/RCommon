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
    /// A DDD-constrained repository for aggregate roots backed by Dapper and the Dommel extension library.
    /// Inherits SQL infrastructure from <see cref="SqlRepositoryBase{TAggregate}"/> and exposes the narrow
    /// <see cref="IAggregateRepository{TAggregate, TKey}"/> contract for aggregate-appropriate operations only.
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate root type. Must implement <see cref="IAggregateRoot{TKey}"/>.</typeparam>
    /// <typeparam name="TKey">The type of the aggregate's identity key.</typeparam>
    /// <remarks>
    /// Each operation acquires a <see cref="DbConnection"/> from the configured <see cref="IDataStore"/>,
    /// ensures it is open before executing, and closes it in a <c>finally</c> block. This repository
    /// uses Dommel's extension methods (e.g., <c>InsertAsync</c>, <c>DeleteAsync</c>, <c>SelectAsync</c>)
    /// for SQL generation from entity mappings.
    ///
    /// Include and ThenInclude are no-ops on this repository because Dapper does not support eager loading.
    /// </remarks>
    public class DapperAggregateRepository<TAggregate, TKey> : SqlRepositoryBase<TAggregate>, IAggregateRepository<TAggregate, TKey>
        where TAggregate : class, IAggregateRoot<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DapperAggregateRepository{TAggregate, TKey}"/>.
        /// </summary>
        /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RDbConnection"/> for the configured data store.</param>
        /// <param name="logger">Factory for creating loggers scoped to this repository type.</param>
        /// <param name="eventTracker">Tracker used to register entities for domain event dispatching.</param>
        /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
        /// <param name="tenantIdAccessor">Accessor for the current tenant identifier.</param>
        public DapperAggregateRepository(IDataStoreFactory dataStoreFactory,
            ILoggerFactory logger, IEntityEventTracker eventTracker,
            IOptions<DefaultDataStoreOptions> defaultDataStoreOptions,
            ITenantIdAccessor tenantIdAccessor)
            : base(dataStoreFactory, logger, eventTracker, defaultDataStoreOptions, tenantIdAccessor)
        {
            Logger = logger.CreateLogger(GetType().Name);
        }

        // ──────────────────────────────────────────────────────────────────────
        // SqlRepositoryBase<TAggregate> abstract member implementations
        // These delegate to the explicit IAggregateRepository implementations
        // or replicate the DapperRepository pattern exactly.
        // ──────────────────────────────────────────────────────────────────────

        /// <inheritdoc />
        public override async Task AddAsync(TAggregate entity, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    EventTracker.AddEntity(entity);
                    MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
                    await db.InsertAsync(entity, cancellationToken: token).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task AddRangeAsync(IEnumerable<TAggregate> entities, CancellationToken token = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    foreach (var entity in entities)
                    {
                        EventTracker.AddEntity(entity);
                        MultiTenantHelper.SetTenantIdIfApplicable(entity, _tenantIdAccessor.GetTenantId());
                        await db.InsertAsync(entity, cancellationToken: token).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the aggregate. If <typeparamref name="TAggregate"/> implements <see cref="ISoftDelete"/>,
        /// a soft delete is performed automatically (sets <c>IsDeleted = true</c> and issues an UPDATE).
        /// Otherwise a physical DELETE is executed.
        /// </summary>
        public override async Task DeleteAsync(TAggregate entity, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TAggregate>())
            {
                SoftDeleteHelper.MarkAsDeleted(entity);
                await UpdateAsync(entity, token).ConfigureAwait(false);
                return;
            }

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    EventTracker.AddEntity(entity);
                    await db.DeleteAsync(entity, cancellationToken: token).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the aggregate using the explicitly specified delete mode. When <paramref name="isSoftDelete"/>
        /// is <c>true</c>, the aggregate must implement <see cref="ISoftDelete"/>; its <c>IsDeleted</c> property
        /// is set to <c>true</c> and an UPDATE is issued. When <c>false</c>, a physical DELETE is always
        /// performed — even if the aggregate implements <see cref="ISoftDelete"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TAggregate"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public override async Task DeleteAsync(TAggregate entity, bool isSoftDelete, CancellationToken token = default)
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
                            await db.OpenAsync(token).ConfigureAwait(false);
                        }

                        EventTracker.AddEntity(entity);
                        await db.DeleteAsync(entity, cancellationToken: token).ConfigureAwait(false);
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
                            await db.CloseAsync().ConfigureAwait(false);
                        }
                    }
                }
                return;
            }

            SoftDeleteHelper.EnsureSoftDeletable<TAggregate>();
            SoftDeleteHelper.MarkAsDeleted(entity);
            await UpdateAsync(entity, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes aggregates matching the expression. If <typeparamref name="TAggregate"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically.
        /// </summary>
        public override async Task<int> DeleteManyAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            if (SoftDeleteHelper.IsSoftDeletable<TAggregate>())
            {
                return await DeleteManyAsync(expression, isSoftDelete: true, token).ConfigureAwait(false);
            }

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    return await db.DeleteMultipleAsync(expression, cancellationToken: token).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes aggregates matching the expression. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching aggregate must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <remarks>
        /// The soft-delete path selects matching aggregates, marks each as deleted, then updates them
        /// one by one via Dommel's <c>UpdateAsync</c>. This is consistent with Dapper/Dommel's
        /// per-entity operation model (there is no bulk update-by-expression in Dommel).
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TAggregate"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public override async Task<int> DeleteManyAsync(Expression<Func<TAggregate, bool>> expression, bool isSoftDelete, CancellationToken token = default)
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
                            await db.OpenAsync(token).ConfigureAwait(false);
                        }

                        return await db.DeleteMultipleAsync(expression, cancellationToken: token).ConfigureAwait(false);
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
                            await db.CloseAsync().ConfigureAwait(false);
                        }
                    }
                }
            }

            SoftDeleteHelper.EnsureSoftDeletable<TAggregate>();

            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    var entities = (await db.SelectAsync(expression, cancellationToken: token).ConfigureAwait(false)).ToList();
                    int count = 0;
                    foreach (var entity in entities)
                    {
                        SoftDeleteHelper.MarkAsDeleted(entity);
                        await db.UpdateAsync(entity, cancellationToken: token).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes aggregates matching the specification. If <typeparamref name="TAggregate"/> implements
        /// <see cref="ISoftDelete"/>, a soft delete is performed automatically.
        /// </summary>
        public override async Task<int> DeleteManyAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes aggregates matching the specification. When <paramref name="isSoftDelete"/> is <c>true</c>,
        /// each matching aggregate must implement <see cref="ISoftDelete"/> — its <c>IsDeleted</c> property is
        /// set to <c>true</c> and an UPDATE is issued instead of a DELETE.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="isSoftDelete"/> is <c>true</c> but <typeparamref name="TAggregate"/>
        /// does not implement <see cref="ISoftDelete"/>.
        /// </exception>
        public override async Task<int> DeleteManyAsync(ISpecification<TAggregate> specification, bool isSoftDelete, CancellationToken token = default)
        {
            return await DeleteManyAsync(specification.Predicate, isSoftDelete, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task UpdateAsync(TAggregate entity, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    EventTracker.AddEntity(entity);
                    await db.UpdateAsync(entity, cancellationToken: token).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task<ICollection<TAggregate>> FindAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await FindAsync(specification.Predicate, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task<ICollection<TAggregate>> FindAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    var filteredExpression = SoftDeleteHelper.CombineWithNotDeletedFilter<TAggregate>(expression);
                    filteredExpression = MultiTenantHelper.CombineWithTenantFilter(filteredExpression, _tenantIdAccessor.GetTenantId());
                    var results = await db.SelectAsync(filteredExpression, cancellationToken: token).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task<TAggregate> FindAsync(object primaryKey, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    var result = await db.GetAsync<TAggregate>(primaryKey, cancellationToken: token).ConfigureAwait(false);

                    // Post-fetch soft-delete check: if the entity was soft-deleted, treat it as not found
                    if (result != null && SoftDeleteHelper.IsSoftDeletable<TAggregate>() && ((ISoftDelete)result).IsDeleted)
                    {
                        return default!;
                    }

                    // Post-fetch tenant check: if the entity belongs to a different tenant, treat it as not found
                    var currentTenantId = _tenantIdAccessor.GetTenantId();
                    if (result != null && MultiTenantHelper.IsMultiTenant<TAggregate>()
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task<long> GetCountAsync(ISpecification<TAggregate> selectSpec, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    var filteredPredicate = SoftDeleteHelper.CombineWithNotDeletedFilter<TAggregate>(selectSpec.Predicate);
                    filteredPredicate = MultiTenantHelper.CombineWithTenantFilter(filteredPredicate, _tenantIdAccessor.GetTenantId());
                    var results = await db.CountAsync(filteredPredicate).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task<long> GetCountAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    var filteredExpression = SoftDeleteHelper.CombineWithNotDeletedFilter<TAggregate>(expression);
                    filteredExpression = MultiTenantHelper.CombineWithTenantFilter(filteredExpression, _tenantIdAccessor.GetTenantId());
                    var results = await db.CountAsync(filteredExpression).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task<TAggregate> FindSingleOrDefaultAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            // Dommel lacks a native SingleOrDefault, so we retrieve all matches and apply SingleOrDefault in-memory
            var result = await FindAsync(expression, token).ConfigureAwait(false);
            return result.SingleOrDefault()!;
        }

        /// <inheritdoc />
        public override async Task<TAggregate> FindSingleOrDefaultAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await FindSingleOrDefaultAsync(specification.Predicate, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task<bool> AnyAsync(Expression<Func<TAggregate, bool>> expression, CancellationToken token = default)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(token).ConfigureAwait(false);
                    }

                    var filteredExpression = SoftDeleteHelper.CombineWithNotDeletedFilter<TAggregate>(expression);
                    filteredExpression = MultiTenantHelper.CombineWithTenantFilter(filteredExpression, _tenantIdAccessor.GetTenantId());
                    var results = await db.AnyAsync(filteredExpression).ConfigureAwait(false);
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override async Task<bool> AnyAsync(ISpecification<TAggregate> specification, CancellationToken token = default)
        {
            return await AnyAsync(specification.Predicate, token).ConfigureAwait(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        // Explicit IAggregateRepository<TAggregate, TKey> implementations
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads an aggregate root by its identity key.
        /// </summary>
        async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.GetByIdAsync(TKey id, CancellationToken cancellationToken)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }

                    var result = await db.GetAsync<TAggregate>(id, cancellationToken: cancellationToken).ConfigureAwait(false);

                    // Post-fetch soft-delete check: if the entity was soft-deleted, treat it as not found
                    if (result != null && SoftDeleteHelper.IsSoftDeletable<TAggregate>() && ((ISoftDelete)result).IsDeleted)
                    {
                        return null;
                    }

                    // Post-fetch tenant check: if the entity belongs to a different tenant, treat it as not found
                    var currentTenantId = _tenantIdAccessor.GetTenantId();
                    if (result != null && MultiTenantHelper.IsMultiTenant<TAggregate>()
                        && !string.IsNullOrEmpty(currentTenantId)
                        && ((IMultiTenant)result).TenantId != currentTenantId)
                    {
                        return null;
                    }

                    return result;
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.GetByIdAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Finds a single aggregate matching the given specification.
        /// </summary>
        async Task<TAggregate?> IAggregateRepository<TAggregate, TKey>.FindAsync(ISpecification<TAggregate> specification, CancellationToken cancellationToken)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }

                    var filteredExpression = SoftDeleteHelper.CombineWithNotDeletedFilter<TAggregate>(specification.Predicate);
                    filteredExpression = MultiTenantHelper.CombineWithTenantFilter(filteredExpression, _tenantIdAccessor.GetTenantId());
                    var results = await db.SelectAsync(filteredExpression, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return results.FirstOrDefault();
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
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether an aggregate with the given identity key exists.
        /// </summary>
        async Task<bool> IAggregateRepository<TAggregate, TKey>.ExistsAsync(TKey id, CancellationToken cancellationToken)
        {
            await using (var db = DataStore.GetDbConnection())
            {
                try
                {
                    if (db.State == ConnectionState.Closed)
                    {
                        await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                    }

                    var result = await db.GetAsync<TAggregate>(id, cancellationToken: cancellationToken).ConfigureAwait(false);
                    return result != null;
                }
                catch (ApplicationException exception)
                {
                    Logger.LogError(exception, "Error in {0}.ExistsAsync while executing on the DbConnection.", GetType().FullName);
                    throw;
                }
                finally
                {
                    if (db.State == ConnectionState.Open)
                    {
                        await db.CloseAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new aggregate root to the repository and persists it.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.AddAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            await AddAsync(aggregate, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing aggregate root and persists the changes.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.UpdateAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            await UpdateAsync(aggregate, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes an aggregate root. If the aggregate implements <see cref="ISoftDelete"/>,
        /// a soft delete is performed automatically.
        /// </summary>
        async Task IAggregateRepository<TAggregate, TKey>.DeleteAsync(TAggregate aggregate, CancellationToken cancellationToken)
        {
            await DeleteAsync(aggregate, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// No-op: Dapper does not support eager loading. Returns this repository for fluent chaining.
        /// </summary>
        IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.Include<TProperty>(Expression<Func<TAggregate, TProperty>> path)
        {
            // Dapper has no eager loading support — this is intentionally a no-op.
            return this;
        }

        /// <summary>
        /// No-op: Dapper does not support eager loading. Returns this repository for fluent chaining.
        /// </summary>
        IAggregateRepository<TAggregate, TKey> IAggregateRepository<TAggregate, TKey>.ThenInclude<TPreviousProperty, TProperty>(Expression<Func<TPreviousProperty, TProperty>> path)
        {
            // Dapper has no eager loading support — this is intentionally a no-op.
            return this;
        }
    }
}
