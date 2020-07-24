using System;
using System.Collections.Generic;

namespace RCommon.DataServices
{
    public interface IDataStoreProvider
    {
        TDataStore GetDataStore<TDataStore>() where TDataStore : IDataStore;
        TDataStore GetDataStore<TDataStore>(Guid transactionId) where TDataStore : IDataStore;
        void RegisterDataSource<TDataStore>(Guid transactionId, TDataStore dataStore) where TDataStore : IDataStore;

        IEnumerable<StoredDataSource> GetRegisteredDataStores(Func<StoredDataSource, bool> criteria);

        void RemoveRegisterdDataStores(Guid transactionId);
    }
}