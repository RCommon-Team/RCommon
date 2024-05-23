using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class ScopedDataStoreRegistry : IDataStoreRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IScopedDataStore _scopedDataStore;

        public ScopedDataStoreRegistry(IServiceProvider serviceProvider, IScopedDataStore scopedDataStore)
        {
            _serviceProvider = serviceProvider;
            _scopedDataStore = scopedDataStore;
        }

        public TDataStore GetDataStore<TDataStore>(string dataStoreName) where TDataStore : IDataStore
        {
            var type = _scopedDataStore.DataStores.Where(x => x.Key == dataStoreName).FirstOrDefault().Value;
            Guard.Against<DataStoreNotFoundException>(type == null,
                this.GetGenericTypeName() + " could not find a DataStore with the key of: " + dataStoreName);
            return (TDataStore)this._serviceProvider.GetService(type);
        }

        public IDataStore GetDataStore(string dataStoreName)
        {
            var type = _scopedDataStore.DataStores.Where(x => x.Key == dataStoreName).FirstOrDefault().Value;
            Guard.Against<DataStoreNotFoundException>(type == null,
                this.GetGenericTypeName() + " could not find a DataStore with the key of: " + dataStoreName);
            return (IDataStore)this._serviceProvider.GetService(type);
        }

        public void RegisterDataStore<TDataStore>(TDataStore dataStore, string dataStoreName) where TDataStore : IDataStore
        {
            if (!_scopedDataStore.DataStores.TryAdd(dataStoreName, typeof(TDataStore)))
            {
                throw new UnsupportedDataStoreException($"The ScopedDataStore refused to add the new DataStore name: {dataStoreName} of type: {dataStore.GetType().AssemblyQualifiedName}");
            }
        }

        public void RemoveRegisteredDataStore(string dataStoreName)
        {
            if (!_scopedDataStore.DataStores.TryRemove(dataStoreName, out _))
            {
                throw new UnsupportedDataStoreException($"The ScopedDataStore refused to remove the DataStore name: {dataStoreName}");
            }
        }
    }
}
