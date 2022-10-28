using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public class CachedDataStoreRegistry : IDataStoreRegistry
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CachedDataStoreRegistry> _logger;
        private readonly IServiceProvider _serviceProvider;

        public CachedDataStoreRegistry(IMemoryCache memoryCache, ILogger<CachedDataStoreRegistry> logger, IServiceProvider serviceProvider)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void RegisterDataStore<TDataStore>(TDataStore dataStore, string dataStoreName)
            where TDataStore : IDataStore
        {
            var newTypeName = dataStore.GetType().AssemblyQualifiedName;
            this._memoryCache.Set<string>(dataStoreName, newTypeName);
            this._logger.LogInformation("Registered new Data Store: {0}", newTypeName);
        }

        public TDataStore GetDataStore<TDataStore>(string dataStoreName)
            where TDataStore : IDataStore
        {
            string dataStoreTypeName;
            if (this._memoryCache.TryGetValue(dataStoreName, out dataStoreTypeName))
            {

                TDataStore dataStore = (TDataStore) this._serviceProvider.GetService(Type.GetType(dataStoreTypeName));
                return dataStore;
            }
            else
            {
                throw new UnsupportedDataStoreException($"A Data Store with the name of: {dataStoreName} and type of: {dataStoreTypeName} was not registered or found");
            }
        }

        public IDataStore GetDataStore(string dataStoreName)
        {
            string dataStoreTypeName;
            if (this._memoryCache.TryGetValue(dataStoreName, out dataStoreTypeName))
            {

                var dataStore = (IDataStore)this._serviceProvider.GetService(Type.GetType(dataStoreTypeName));
                return dataStore;
            }
            else
            {
                throw new UnsupportedDataStoreException($"A Data Store with the name of: {dataStoreName} and type of: {dataStoreTypeName} was not registered or found");
            }
        }

        public void RemoveRegisteredDataStore(string dataStoreName)
        {
            this._memoryCache.Remove(dataStoreName);
        }
    }
}
