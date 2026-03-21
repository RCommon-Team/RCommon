using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Models;
using RCommon.Persistence;
using RCommon.Persistence.Crud;

namespace RCommon.Persistence.EFCore.Crud;

public class EFCoreReadModelRepository<TReadModel> : IReadModelRepository<TReadModel>
    where TReadModel : class, IReadModel
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly ILogger _logger;
    private string _dataStoreName;
    private IQueryable<TReadModel>? _repositoryQuery;

    public EFCoreReadModelRepository(
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

    private RCommonDbContext ObjectContext
        => _dataStoreFactory.Resolve<RCommonDbContext>(_dataStoreName);

    private DbSet<TReadModel> ObjectSet
        => ObjectContext.Set<TReadModel>();

    /// <summary>
    /// Gets the base queryable used for all read operations. Defaults to no-tracking since
    /// read models do not participate in change tracking or domain events.
    /// </summary>
    private IQueryable<TReadModel> RepositoryQuery
    {
        get
        {
            if (_repositoryQuery == null)
            {
                _repositoryQuery = ObjectSet.AsNoTracking();
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
            .FirstOrDefaultAsync(cancellationToken)
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
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IReadModelRepository<TReadModel> Include<TProperty>(
        Expression<Func<TReadModel, TProperty>> path)
    {
        _repositoryQuery = RepositoryQuery.Include(path);
        return this;
    }
}
