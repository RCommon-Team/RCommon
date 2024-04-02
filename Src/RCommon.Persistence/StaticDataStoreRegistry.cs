using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
            var type = StaticDataStore.DataStores.Where(x => x.Key == dataStoreName).FirstOrDefault().Value;
            Guard.Against<DataStoreNotFoundException>(type == null,
                this.GetGenericTypeName() + " could not find a DataStore with the key of: " + dataStoreName);
            return (TDataStore)this._serviceProvider.GetService(type);
        }

        public IDataStore GetDataStore(string dataStoreName)
        {
            var type = StaticDataStore.DataStores.Where(x => x.Key == dataStoreName).FirstOrDefault().Value;
            Guard.Against<DataStoreNotFoundException>(type == null, 
                this.GetGenericTypeName() + " could not find a DataStore with the key of: " + dataStoreName);
            return (IDataStore) this._serviceProvider.GetService(type);
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
