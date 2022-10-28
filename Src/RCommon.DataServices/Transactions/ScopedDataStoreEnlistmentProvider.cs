using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices.Transactions
{
    public class ScopedDataStoreEnlistmentProvider : IDataStoreEnlistmentProvider
    {
        public ScopedDataStoreEnlistmentProvider()
        {
            this.DataStores = new ConcurrentDictionary<Guid, IDataStore>();
        }

        public ConcurrentDictionary<Guid, IDataStore> DataStores { get; }

        public bool EnlistDataStore(Guid transactionId, IDataStore dataStore)
        {
            var dataStoreValue = this.DataStores.GetOrAdd(transactionId, dataStore);

            if (dataStoreValue == null)
            {
                return false;
            }
            return true;

        }

        public IList<IDataStore> GetEnlistedDataStores(Guid transactionId)
        {
            var dataStores = this.DataStores.Where(x => x.Key == transactionId)
                .Select(x => x.Value).ToList();
            return dataStores;
        }

        public bool RemoveEnlistedDataStores(Guid transactionId)
        {
            var dataStores = this.DataStores.Where(x => x.Key == transactionId);
            foreach (var item in dataStores)
            {
                if (!this.DataStores.TryRemove(item))
                {
                    return false;
                }

            }
            return true;
        }
    }
}
