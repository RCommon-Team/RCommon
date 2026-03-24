using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Models;
using RCommon.Persistence;
using RCommon.Persistence.Crud;

namespace RCommon.Persistence.Linq2Db.Crud;

/// <summary>
/// A read-model repository implementation using Linq2Db for query operations.
/// </summary>
/// <typeparam name="TReadModel">
/// The read-model/projection type. Must implement <see cref="IReadModel"/> and be a class.
/// </typeparam>
/// <remarks>
/// Queries are built against <see cref="ITable{TReadModel}"/> from the underlying
/// <see cref="RCommonDataConnection"/>. Read models do not participate in domain event tracking,
/// change tracking, soft-delete filtering, or multi-tenancy filtering.
///
/// Eager loading via <see cref="Include{TProperty}"/> is supported through Linq2Db's
/// <c>LoadWith</c> API.
/// </remarks>
public class Linq2DbReadModelRepository<TReadModel> : IReadModelRepository<TReadModel>
    where TReadModel : class, IReadModel
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly ILogger _logger;
    private string _dataStoreName;
    private IQueryable<TReadModel>? _repositoryQuery;
    private ILoadWithQueryable<TReadModel, object>? _includableQueryable;

    /// <summary>
    /// Initializes a new instance of <see cref="Linq2DbReadModelRepository{TReadModel}"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDataConnection"/> for the configured data store.</param>
    /// <param name="loggerFactory">Factory for creating loggers scoped to this repository type.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public Linq2DbReadModelRepository(
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
    /// Gets the <see cref="RCommonDataConnection"/> for the configured data store, resolved through the <see cref="IDataStoreFactory"/>.
    /// </summary>
    private RCommonDataConnection DataConnection
        => _dataStoreFactory.Resolve<RCommonDataConnection>(_dataStoreName);

    /// <summary>
    /// Gets the Linq2Db <see cref="ITable{TReadModel}"/> from the current <see cref="DataConnection"/> for direct table operations.
    /// </summary>
    private ITable<TReadModel> ObjectSet
        => DataConnection.GetTable<TReadModel>();

    /// <summary>
    /// Gets the base <see cref="IQueryable{TReadModel}"/> used for all query operations.
    /// Applies eager-loading expressions if any have been configured via <see cref="Include{TProperty}"/>.
    /// </summary>
    private IQueryable<TReadModel> RepositoryQuery
    {
        get
        {
            if (_repositoryQuery == null)
            {
                _repositoryQuery = ObjectSet.AsQueryable();
            }

            // Override the base query with the eager-loaded queryable if includes have been configured
            if (_includableQueryable != null)
            {
                _repositoryQuery = _includableQueryable;
            }

            return _repositoryQuery;
        }
    }

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
        return await RepositoryQuery
            .Where(specification.Predicate)
            .FirstOrDefaultAsync(token: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TReadModel>> FindAllAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        return await RepositoryQuery
            .Where(specification.Predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IPagedResult<TReadModel>> GetPagedAsync(
        IPagedSpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        var query = RepositoryQuery.Where(specification.Predicate);
        var totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query
            .Skip((specification.PageNumber - 1) * specification.PageSize)
            .Take(specification.PageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<TReadModel>(items, totalCount, specification.PageNumber, specification.PageSize);
    }

    /// <inheritdoc />
    public async Task<long> GetCountAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        return await RepositoryQuery
            .Where(specification.Predicate)
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> AnyAsync(
        ISpecification<TReadModel> specification,
        CancellationToken cancellationToken = default)
    {
        return await RepositoryQuery
            .Where(specification.Predicate)
            .AnyAsync(token: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Adds an eager-loading path for the specified navigation property using Linq2Db's <c>LoadWith</c> API.
    /// </summary>
    /// <typeparam name="TProperty">The type of the navigation property to include.</typeparam>
    /// <param name="path">An expression selecting the navigation property to include.</param>
    /// <returns>This repository instance for fluent chaining of additional includes.</returns>
    public IReadModelRepository<TReadModel> Include<TProperty>(
        Expression<Func<TReadModel, TProperty>> path)
    {
        // Convert to Expression<Func<TReadModel, object>> so it is compatible with
        // the ILoadWithQueryable<TReadModel, object> field used by LoadWith.
        var converted = Expression.Lambda<Func<TReadModel, object>>(
            Expression.Convert(path.Body, typeof(object)),
            path.Parameters);

        _includableQueryable = RepositoryQuery.LoadWith(converted!);
        return this;
    }
}
