using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Sagas;

namespace RCommon.Persistence.EFCore.Sagas;

/// <summary>
/// An EF Core implementation of <see cref="ISagaStore{TState,TKey}"/> that persists saga state
/// using a <see cref="RCommonDbContext"/> resolved through the <see cref="IDataStoreFactory"/>.
/// </summary>
/// <typeparam name="TState">The saga state type. Must derive from <see cref="SagaState{TKey}"/>.</typeparam>
/// <typeparam name="TKey">The primary key type. Must implement <see cref="IEquatable{TKey}"/>.</typeparam>
public class EFCoreSagaStore<TState, TKey> : ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly ILogger _logger;
    private string _dataStoreName;

    /// <summary>
    /// Initializes a new instance of <see cref="EFCoreSagaStore{TState,TKey}"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDbContext"/> for the configured data store.</param>
    /// <param name="loggerFactory">Factory for creating loggers scoped to this store type.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public EFCoreSagaStore(
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
    /// Gets the <see cref="RCommonDbContext"/> for the configured data store, resolved through the <see cref="IDataStoreFactory"/>.
    /// </summary>
    private RCommonDbContext ObjectContext
        => _dataStoreFactory.Resolve<RCommonDbContext>(_dataStoreName);

    /// <summary>
    /// Gets the <see cref="DbSet{TState}"/> from the current <see cref="ObjectContext"/> for direct entity set operations.
    /// </summary>
    private DbSet<TState> ObjectSet
        => ObjectContext.Set<TState>();

    /// <inheritdoc />
    public async Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        try
        {
            return await ObjectSet.FindAsync(new object[] { id }, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.GetByIdAsync while executing on the DbContext.", GetType().FullName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        try
        {
            return await ObjectSet
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.FindByCorrelationIdAsync while executing on the DbContext.", GetType().FullName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(TState state, CancellationToken ct = default)
    {
        try
        {
            var context = ObjectContext;
            var existing = await context.Set<TState>().FindAsync(new object[] { state.Id }, ct).ConfigureAwait(false);

            if (existing == null)
            {
                await context.Set<TState>().AddAsync(state, ct).ConfigureAwait(false);
            }
            else
            {
                context.Entry(existing).CurrentValues.SetValues(state);
            }

            await context.SaveChangesAsync(true, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.SaveAsync while executing on the DbContext.", GetType().FullName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TState state, CancellationToken ct = default)
    {
        try
        {
            var context = ObjectContext;
            context.Set<TState>().Remove(state);
            await context.SaveChangesAsync(true, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.DeleteAsync while executing on the DbContext.", GetType().FullName);
            throw;
        }
    }
}
