using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class DataStoreFactory : IDataStoreFactory
    {
        private readonly IServiceProvider _provider;
        private readonly IDictionary<string, Type> _types;

        public DataStoreFactory(IServiceProvider provider, IOptions<DataStoreFactoryOptions> options)
        {
            _provider = provider;
            _types = options.Value.Types;
        }

        public IDataStore Resolve(string name)
        {
            if (_types.TryGetValue(name, out var type))
            {
                return (IDataStore)_provider.GetRequiredService(type);
            }

            throw new DataStoreNotFoundException($"DataStore with name of {name} not found");
        }

        public TDataStore Resolve<TDataStore>(string name)
            where TDataStore : IDataStore
        {
            if (_types.TryGetValue(name, out var type))
            {
                return (TDataStore)_provider.GetRequiredService(type);
            }

            throw new DataStoreNotFoundException($"DataStore with name of {name} not found");
        }
    }
}
