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
    /// <summary>
    /// Default implementation of <see cref="IDataStoreFactory"/> that resolves named data stores
    /// from the DI container based on registered <see cref="DataStoreValue"/> entries.
    /// </summary>
    public class DataStoreFactory : IDataStoreFactory
    {
        private readonly IServiceProvider _provider;
        private readonly ConcurrentBag<DataStoreValue> _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreFactory"/> class.
        /// </summary>
        /// <param name="provider">The service provider used to resolve data store instances.</param>
        /// <param name="options">The configured factory options containing registered <see cref="DataStoreValue"/> entries.</param>
        public DataStoreFactory(IServiceProvider provider, IOptions<DataStoreFactoryOptions> options)
        {
            _provider = provider;
            _values = options.Value.Values;
        }

        /// <inheritdoc />
        public C Resolve<B, C>(string name)
            where B : IDataStore
            where C : B, IDataStore
        {
            // Attempt to peek at the first value in the bag as a quick existence check
            DataStoreValue? value = new DataStoreValue(name, typeof(B), typeof(C));
            if (_values.TryPeek(out value))
            {
                return (C)_provider.GetRequiredService(value.ConcreteType);
            }

            throw new DataStoreNotFoundException($"DataStore with name of {name} not found");
        }

        /// <inheritdoc />
        public B Resolve<B>(string name)
            where B : IDataStore
        {
            // Search the registered values by both name and base type to find the matching concrete type
            if (_values.Any(x => x.Name == name && x.BaseType == typeof(B)))
            {
                return (B)_provider.GetRequiredService(_values.First(x => x.Name == name && x.BaseType == typeof(B)).ConcreteType);
            }

            throw new DataStoreNotFoundException($"DataStore with name of {name} and base type of {typeof(B).GetGenericTypeName()} not found");
        }
    }
}
