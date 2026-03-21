using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Dommel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Models;
using RCommon.Persistence;
using RCommon.Persistence.Crud;
using RCommon.Persistence.Sql;

namespace RCommon.Persistence.Dapper.Crud;

/// <summary>
/// A read-model repository implementation using Dapper and the Dommel extension library for query operations.
/// </summary>
/// <typeparam name="TReadModel">
/// The read-model/projection type. Must implement <see cref="IReadModel"/> and be a class.
/// </typeparam>
/// <remarks>
/// Each operation acquires a <see cref="System.Data.Common.DbConnection"/> from the configured
/// <see cref="IDataStore"/>, ensures it is open before executing, and closes it in a
/// <c>finally</c> block. This repository uses Dommel's extension methods for SQL generation.
///
/// Read models do not participate in domain event tracking or soft-delete filtering.
/// <see cref="Include{TProperty}"/> is a no-op because Dapper does not support eager loading.
/// </remarks>
public class DapperReadModelRepository<TReadModel> : IReadModelRepository<TReadModel>
    where TReadModel : class, IReadModel
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly ILogger _logger;
    private string _dataStoreName;

    /// <summary>
    /// Initializes a new instance of <see cref="DapperReadModelRepository{TReadModel}"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RDbConnection"/> for the configured data store.</param>
    /// <param name="loggerFactory">Factory for creating loggers scoped to this repository type.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public DapperReadModelRepository(
        IDataStoreFactory dataStoreFactory,
        ILoggerFactory loggerFactory,
        IOptions<DefaultDataStoreOptions> defaultDataStoreOptions)
    {
        _dataStoreFactory = dataStoreFactory ?? throw new ArgumentNullException(nameof(dataStoreFactory));
        _logger = loggerFactory?.CreateLogger(GetType().Name) ?? throw new ArgumentNullException(nameof(loggerFactory));

        if (defaultDataStoreOptions is null)
            throw new ArgumentNullException(nameof(defaultDataStoreOptions));

        _dataStoreName = defaultDataStoreOptions.Value?.DefaultDataStoreName ?? string.Empty;
    }

    /// <summary>
    /// Gets the resolved <see cref="RDbConnection"/> for this repository using the current <see cref="DataStoreName"/>.
    /// </summary>
    private RDbConnection DataStore
        => _dataStoreFactory.Resolve<RDbConnection>(_dataStoreName);

    /// <inheritdoc />
    public string DataStoreName
    {
        get => _dataStoreName;
        set => _dataStoreName = value;
    }

    /// <inheritdoc />
    public async Task<TReadModel?> FindAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        await using (var db = DataStore.GetDbConnection())
        {
            try
            {
                if (db.State == ConnectionState.Closed)
                {
                    await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                var results = await db.SelectAsync<TReadModel>(
                    specification.Predicate,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return results.FirstOrDefault();
            }
            catch (ApplicationException exception)
            {
                _logger.LogError(exception,
                    "Error in {RepositoryType}.FindAsync while executing on the DbConnection.",
                    GetType().FullName);
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
    public async Task<IReadOnlyList<TReadModel>> FindAllAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        await using (var db = DataStore.GetDbConnection())
        {
            try
            {
                if (db.State == ConnectionState.Closed)
                {
                    await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                var results = await db.SelectAsync<TReadModel>(
                    specification.Predicate,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return results.ToList();
            }
            catch (ApplicationException exception)
            {
                _logger.LogError(exception,
                    "Error in {RepositoryType}.FindAllAsync while executing on the DbConnection.",
                    GetType().FullName);
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
    public async Task<IPagedResult<TReadModel>> GetPagedAsync(
        IPagedSpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        await using (var db = DataStore.GetDbConnection())
        {
            try
            {
                if (db.State == ConnectionState.Closed)
                {
                    await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                // Dommel does not support server-side paging with Skip/Take, so we fetch
                // the full filtered result set and apply paging in-memory.
                var allResults = (await db.SelectAsync<TReadModel>(
                    specification.Predicate,
                    cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();

                var totalCount = (long)allResults.Count;

                var items = allResults
                    .Skip((specification.PageNumber - 1) * specification.PageSize)
                    .Take(specification.PageSize)
                    .ToList();

                return new PagedResult<TReadModel>(items, totalCount, specification.PageNumber, specification.PageSize);
            }
            catch (ApplicationException exception)
            {
                _logger.LogError(exception,
                    "Error in {RepositoryType}.GetPagedAsync while executing on the DbConnection.",
                    GetType().FullName);
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
    public async Task<long> GetCountAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        await using (var db = DataStore.GetDbConnection())
        {
            try
            {
                if (db.State == ConnectionState.Closed)
                {
                    await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                var count = await db.CountAsync<TReadModel>(specification.Predicate).ConfigureAwait(false);
                return count;
            }
            catch (ApplicationException exception)
            {
                _logger.LogError(exception,
                    "Error in {RepositoryType}.GetCountAsync while executing on the DbConnection.",
                    GetType().FullName);
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
    public async Task<bool> AnyAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        await using (var db = DataStore.GetDbConnection())
        {
            try
            {
                if (db.State == ConnectionState.Closed)
                {
                    await db.OpenAsync(cancellationToken).ConfigureAwait(false);
                }

                var result = await db.AnyAsync<TReadModel>(specification.Predicate).ConfigureAwait(false);
                return result;
            }
            catch (ApplicationException exception)
            {
                _logger.LogError(exception,
                    "Error in {RepositoryType}.AnyAsync while executing on the DbConnection.",
                    GetType().FullName);
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
    /// No-op: Dapper does not support eager loading. Returns this repository for fluent chaining.
    /// </summary>
    /// <param name="path">The navigation property expression (ignored).</param>
    /// <returns>This repository instance.</returns>
    public IReadModelRepository<TReadModel> Include<TProperty>(
        Expression<Func<TReadModel, TProperty>> path)
    {
        // Dapper has no eager loading support — this is intentionally a no-op.
        return this;
    }
}
