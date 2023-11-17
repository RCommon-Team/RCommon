using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public class ScopedDataStoreEnlistmentProvider : IDataStoreEnlistmentProvider
    {
        public ScopedDataStoreEnlistmentProvider()
        {
            DataStores = new ConcurrentDictionary<Guid, IDataStore>();
        }

        public bool EnlistDataStore(Guid transactionId, IDataStore dataStore)
        {
            var dataStoreValue = DataStores.GetOrAdd(transactionId, dataStore);

            if (dataStoreValue == null)
            {
                return false;
            }
            return true;

        }

        public IList<IDataStore> GetEnlistedDataStores(Guid transactionId)
        {
            var dataStores = DataStores.Where(x => x.Key == transactionId)
                .Select(x => x.Value).ToList();
            return dataStores;
        }

        public bool RemoveEnlistedDataStores(Guid transactionId)
        {
            var dataStores = DataStores.Where(x => x.Key == transactionId);
            foreach (var item in dataStores)
            {
                if (!DataStores.TryRemove(item))
                {
                    return false;
                }

            }
            return true;
        }

        public ConcurrentDictionary<Guid, IDataStore> DataStores { get; }
    }
}
