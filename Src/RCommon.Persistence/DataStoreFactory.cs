using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class DataStoreFactory : IDataStoreFactory
    {
        private readonly IServiceProvider _provider;
        private readonly ConcurrentBag<DataStoreValue> _values;

        public DataStoreFactory(IServiceProvider provider, IOptions<DataStoreFactoryOptions> options)
        {
            _provider = provider;
            _values = options.Value.Values;
        }

        public C Resolve<B, C>(string name)
            where B : IDataStore
            where C : B, IDataStore
        {
            DataStoreValue value = new DataStoreValue(name, typeof(B), typeof(C));
            if (_values.TryPeek(out value))
            {
                return (C)_provider.GetRequiredService(value.ConcreteType);
            }

            throw new DataStoreNotFoundException($"DataStore with name of {name} not found");
        }

        public B Resolve<B>(string name)
            where B : IDataStore
        {
            if (_values.Any(x => x.Name == name && x.BaseType == typeof(B)))
            {
                return (B)_provider.GetRequiredService(_values.First(x => x.Name == name && x.BaseType == typeof(B)).ConcreteType);
            }

            throw new DataStoreNotFoundException($"DataStore with name of {name} and base type of {typeof(B).GetGenericTypeName()} not found");
        }
    }
}
