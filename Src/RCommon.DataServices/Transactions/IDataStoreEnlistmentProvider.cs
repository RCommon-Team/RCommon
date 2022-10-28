using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RCommon.DataServices.Transactions
{
    public interface IDataStoreEnlistmentProvider
    {
        ConcurrentDictionary<Guid, IDataStore> DataStores { get; }

        bool EnlistDataStore(Guid transactionId, IDataStore dataStore);
        IList<IDataStore> GetEnlistedDataStores(Guid transactionId);

        bool RemoveEnlistedDataStores(Guid transactionId);
    }
}
