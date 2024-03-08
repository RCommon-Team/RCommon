using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class StaticDataStoreRegistry : IDataStoreRegistry
    {
        private readonly IServiceProvider _serviceProvider;

        public StaticDataStoreRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }


        public TDataStore GetDataStore<TDataStore>(string dataStoreName)
            where TDataStore : IDataStore
        {
            var dataStore = this._serviceProvider.GetService(StaticDataStore.DataStores.Where(x => x.Key == dataStoreName).FirstOrDefault().Value);
            return (TDataStore) dataStore; 
        }

        public IDataStore GetDataStore(string dataStoreName)
        {
            return (IDataStore)this._serviceProvider.GetService(StaticDataStore.DataStores.Where(x => x.Key == dataStoreName).FirstOrDefault().Value);
        }

        public void RegisterDataStore<TDataStore>(TDataStore dataStore, string dataStoreName) 
            where TDataStore : IDataStore
        {
            if (!StaticDataStore.DataStores.TryAdd(dataStoreName, typeof(TDataStore)))
            {
                throw new UnsupportedDataStoreException($"The StaticDataStore refused to add the new DataStore name: {dataStoreName} of type: {dataStore.GetType().AssemblyQualifiedName}");
            }
        }

        public void RemoveRegisteredDataStore(string dataStoreName)
        {
            if (!StaticDataStore.DataStores.TryRemove(dataStoreName, out _))
            {
                throw new UnsupportedDataStoreException($"The StaticDataStore refused to remove the DataStore name: {dataStoreName}");
            }
        }
    }
}
