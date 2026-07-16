using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace RCommon.Persistence
{
    /// <summary>
    /// Infers <see cref="DefaultDataStoreOptions.DefaultDataStoreName"/> when exactly one data store
    /// is registered and no explicit default was set via <c>SetDefaultDataStore(...)</c>. Runs as a
    /// standard <see cref="IPostConfigureOptions{TOptions}"/> so it always executes after any explicit
    /// <c>Configure&lt;DefaultDataStoreOptions&gt;</c> call, regardless of registration order.
    /// </summary>
    internal sealed class DefaultDataStoreOptionsPostConfigure : IPostConfigureOptions<DefaultDataStoreOptions>
    {
        private readonly IOptions<DataStoreFactoryOptions> _dataStoreFactoryOptions;
        private readonly ILogger<DefaultDataStoreOptions>? _logger;

        public DefaultDataStoreOptionsPostConfigure(
            IOptions<DataStoreFactoryOptions> dataStoreFactoryOptions,
            ILogger<DefaultDataStoreOptions>? logger = null)
        {
            _dataStoreFactoryOptions = dataStoreFactoryOptions;
            _logger = logger;
        }

        public void PostConfigure(string? name, DefaultDataStoreOptions options)
        {
            if (!string.IsNullOrEmpty(options.DefaultDataStoreName))
            {
                return; // consumer explicitly set a default -- always respected, never overridden
            }

            var registered = _dataStoreFactoryOptions.Value.Values
                .Select(v => v.Name).Distinct().ToList();

            if (registered.Count == 1)
            {
                options.DefaultDataStoreName = registered[0];
                _logger?.LogInformation(
                    "RCommon inferred '{DataStoreName}' as the default data store because it is the only one " +
                    "registered. Call SetDefaultDataStore(...) explicitly to set this yourself and silence this message.",
                    registered[0]);
            }
            else if (registered.Count > 1)
            {
                _logger?.LogInformation(
                    "RCommon found {Count} registered data stores ({Names}) and no default was set via " +
                    "SetDefaultDataStore(...). This is expected if every repository sets DataStoreName explicitly; " +
                    "otherwise, repository calls that don't specify DataStoreName will throw DataStoreNotFoundException. " +
                    "Call SetDefaultDataStore(...) to resolve.",
                    registered.Count, string.Join(", ", registered));
            }
        }
    }
}
