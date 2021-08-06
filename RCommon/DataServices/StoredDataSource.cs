using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RCommon.DataServices
{
    public class StoredDataSource : IStoredDataSource
    {
        public StoredDataSource(Guid transactionId, IDataStore dataStore)
        {
            TransactionId = transactionId;
            DataStore = dataStore;
        }

        public Guid TransactionId { get; }
        public IDataStore DataStore { get; }
    }
}
