using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class DataStoreFactoryOptions
    {
        public ConcurrentBag<DataStoreValue> Values { get; } = new ConcurrentBag<DataStoreValue>();

        public void Register<B, C>(string name)
            where B : IDataStore
            where C : IDataStore
        {
            if (!Values.Any(x => x.Name == name && x.BaseType == typeof(B)))
            {
                Values.Add(new DataStoreValue(name, typeof(B), typeof(C)));
            }
            else
            {
                throw new UnsupportedDataStoreException($"You cannot register a data store with the same name of {name} as an existing one with the same base type of {typeof(B).GetGenericTypeName()}");
            }
        }
    }
}
