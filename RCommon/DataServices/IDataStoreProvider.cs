using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public interface IDataStoreProvider
    {
        TDataStore GetDataStore<TDataStore>() where TDataStore : IDataStore;
        TDataStore GetDataStore<TDataStore>(Guid transactionId, string dataStoreName = null) where TDataStore : IDataStore;

        TDataStore GetDataStore<TDataStore>(string dataStoreName = null)
            where TDataStore : IDataStore;

        void RegisterDataStore<TDataStore>(Guid transactionId, TDataStore dataStore) where TDataStore : IDataStore;


        IEnumerable<StoredDataSource> GetRegisteredDataStores(Func<StoredDataSource, bool> criteria);

        Task RemoveRegisterdDataStoresAsync(Guid transactionId);
    }
}