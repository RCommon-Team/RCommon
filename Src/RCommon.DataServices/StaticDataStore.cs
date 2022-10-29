using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public static class StaticDataStore
    {
        public static ConcurrentDictionary<string, Type> DataStores { get; set; }
    }
}
