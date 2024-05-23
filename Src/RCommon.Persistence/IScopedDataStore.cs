using System;
using System.Collections.Concurrent;

namespace RCommon.Persistence
{
    public interface IScopedDataStore
    {
        ConcurrentDictionary<string, Type> DataStores { get; set; }
    }
}