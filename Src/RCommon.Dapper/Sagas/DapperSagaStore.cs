using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dommel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RCommon.Persistence.Sagas;
using RCommon.Persistence.Sql;

namespace RCommon.Persistence.Dapper.Sagas;

/// <summary>
/// A Dapper/Dommel implementation of <see cref="ISagaStore{TState,TKey}"/> that persists saga state
/// using a <see cref="RDbConnection"/> resolved through the <see cref="IDataStoreFactory"/>.
/// </summary>
/// <typeparam name="TState">The saga state type. Must derive from <see cref="SagaState{TKey}"/>.</typeparam>
/// <typeparam name="TKey">The primary key type. Must implement <see cref="IEquatable{TKey}"/>.</typeparam>
public class DapperSagaStore<TState, TKey> : ISagaStore<TState, TKey>
    where TState : SagaState<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly IDataStoreFactory _dataStoreFactory;
    private readonly ILogger _logger;
    private string _dataStoreName;

    /// <summary>
    /// Initializes a new instance of <see cref="DapperSagaStore{TState,TKey}"/>.
    /// </summary>
    /// <param name="dataStoreFactory">Factory used to resolve the <see cref="RDbConnection"/> for the configured data store.</param>
    /// <param name="loggerFactory">Factory for creating loggers scoped to this store type.</param>
    /// <param name="defaultDataStoreOptions">Options specifying which data store to use when none is explicitly set.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required parameter is <c>null</c>.</exception>
    public DapperSagaStore(
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
    /// Gets the resolved <see cref="RDbConnection"/> for this store using the current <see cref="_dataStoreName"/>.
    /// </summary>
    private RDbConnection DataStore
        => _dataStoreFactory.Resolve<RDbConnection>(_dataStoreName);

    /// <inheritdoc />
    public async Task<TState?> GetByIdAsync(TKey id, CancellationToken ct = default)
    {
        await using var db = DataStore.GetDbConnection();
        try
        {
            if (db.State == ConnectionState.Closed)
                await db.OpenAsync(ct).ConfigureAwait(false);

            return await db.GetAsync<TState>(id, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.GetByIdAsync while executing on the DbConnection.", GetType().FullName);
            throw;
        }
        finally
        {
            if (db.State == ConnectionState.Open)
                await db.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TState?> FindByCorrelationIdAsync(string correlationId, CancellationToken ct = default)
    {
        await using var db = DataStore.GetDbConnection();
        try
        {
            if (db.State == ConnectionState.Closed)
                await db.OpenAsync(ct).ConfigureAwait(false);

            var results = await db.SelectAsync<TState>(
                s => s.CorrelationId == correlationId,
                cancellationToken: ct).ConfigureAwait(false);

            return results.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.FindByCorrelationIdAsync while executing on the DbConnection.", GetType().FullName);
            throw;
        }
        finally
        {
            if (db.State == ConnectionState.Open)
                await db.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(TState state, CancellationToken ct = default)
    {
        await using var db = DataStore.GetDbConnection();
        try
        {
            if (db.State == ConnectionState.Closed)
                await db.OpenAsync(ct).ConfigureAwait(false);

            var updated = await db.UpdateAsync(state, cancellationToken: ct).ConfigureAwait(false);
            if (!updated)
            {
                await db.InsertAsync(state, cancellationToken: ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.SaveAsync while executing on the DbConnection.", GetType().FullName);
            throw;
        }
        finally
        {
            if (db.State == ConnectionState.Open)
                await db.CloseAsync().ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(TState state, CancellationToken ct = default)
    {
        await using var db = DataStore.GetDbConnection();
        try
        {
            if (db.State == ConnectionState.Closed)
                await db.OpenAsync(ct).ConfigureAwait(false);

            await db.DeleteAsync(state, cancellationToken: ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {StoreType}.DeleteAsync while executing on the DbConnection.", GetType().FullName);
            throw;
        }
        finally
        {
            if (db.State == ConnectionState.Open)
                await db.CloseAsync().ConfigureAwait(false);
        }
    }
}
