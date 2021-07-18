using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RCommon.DataServices
{
    public class StoredDataSource : IStoredDataSource
    {
        public StoredDataSource(Guid transactionId, Type dataStore, string dataStoreName)
        {
            TransactionId = transactionId;
            Type = dataStore;
            DataStoreName = dataStoreName;
        }

        public Guid TransactionId { get; }
        public Type Type { get; }

        public string DataStoreName { get; set; }
    }
}
