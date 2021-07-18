using System;
using System.Collections.Generic;

namespace RCommon.DataServices
{
    public interface IDataStoreProvider
    {
        IDataStore GetDataStore(Guid transactionId, string dataStoreName);
        IDataStore GetDataStore(string dataStoreName);
        IEnumerable<StoredDataSource> GetRegisteredDataStores(Func<StoredDataSource, bool> criteria);
        void RegisterDataStore(Guid transactionId, Type dataStore, string dataStoreName);
        void RemoveRegisterdDataStores(Guid transactionId);
    }
}