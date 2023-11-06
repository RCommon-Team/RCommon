using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.DataServices
{
    public sealed class StaticDataStore
    {

        private static readonly Lazy<StaticDataStore> lazy =
        new Lazy<StaticDataStore>(() => new StaticDataStore());

        public static StaticDataStore Instance { get { return lazy.Value; } }

        private StaticDataStore()
        {
        }

        public static ConcurrentDictionary<string, Type> DataStores { get; set; }
    }
}
