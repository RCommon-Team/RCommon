using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Sagas;

namespace RCommon.Persistence.Linq2Db.Sagas;

/// <summary>
/// A Linq2Db implementation of <see cref="ISagaStore{TState,TKey}"/> that persists saga state
/// using a <see cref="RCommonDataConnection"/> resolved through the <see cref="IDataStoreFactory"/>.
/// </summary>
/// <typeparam name="TState">The saga state type. Must derive from <see cref="SagaState{TKey}"/>.</typeparam>
/// <typeparam name="TKey">The primary key type. Must implement <see cref="IEquatable{TKey}"/>.</typeparam>
public class Linq2DbSagaStore<TState, TKey> : ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly ILogger _logger;
    private string _dataStoreName;

    /// <summary>
    /// Initializes a new instance of <see cref="Linq2DbSagaStore{TState,TKey}"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RCommonDataConnection"/> for the configured data store.</param>
    /// <param name="loggerFactory">Factory for creating loggers scoped to this store type.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public Linq2DbSagaStore(
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

    /// <inheritdoc />
    public async Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        try
        {
            return await DataConnection.GetTable<TState>()
                .FirstOrDefaultAsync(s => s.Id.Equals(id), token: ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.GetByIdAsync while executing on the DataConnection.", GetType().FullName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        try
        {
            return await DataConnection.GetTable<TState>()
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, token: ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.FindByCorrelationIdAsync while executing on the DataConnection.", GetType().FullName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(TState state, CancellationToken ct = default)
    {
        try
        {
            await DataConnection.InsertOrReplaceAsync(state, token: ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.SaveAsync while executing on the DataConnection.", GetType().FullName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TState state, CancellationToken ct = default)
    {
        try
        {
            await DataConnection.GetTable<TState>()
                .Where(s => s.Id.Equals(state.Id))
                .DeleteAsync(ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.DeleteAsync while executing on the DataConnection.", GetType().FullName);
            throw;
        }
    }
}
