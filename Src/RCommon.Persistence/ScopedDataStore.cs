using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class ScopedDataStore : IScopedDataStore
    {

        public ScopedDataStore()
        {
            DataStores = new ConcurrentDictionary<string, Type>();
        }

        public ConcurrentDictionary<string, Type> DataStores { get; set; }
    }
}
