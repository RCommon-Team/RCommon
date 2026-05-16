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
    /// Configuration options for <see cref="DataStoreFactory"/> that holds the collection of registered
    /// <see cref="DataStoreValue"/> entries used to resolve data stores by name and type.
    /// </summary>
    public class DataStoreFactoryOptions
    {
        /// <summary>
        /// Gets the thread-safe collection of registered <see cref="DataStoreValue"/> entries.
        /// </summary>
        public ConcurrentBag<DataStoreValue> Values { get; } = new ConcurrentBag<DataStoreValue>();

        /// <summary>
        /// Registers a data store mapping with the specified name, base type, and concrete type.
        /// </summary>
        /// <typeparam name="B">The base data store type (e.g., a provider-specific DbContext base).</typeparam>
        /// <typeparam name="C">The concrete data store type that implements <typeparamref name="B"/>.</typeparam>
        /// <param name="name">A unique name identifying the data store registration.</param>
        /// <exception cref="UnsupportedDataStoreException">
        /// Thrown when a data store with the same <paramref name="name"/> and base type <typeparamref name="B"/> is already registered.
        /// </exception>
        public void Register<B, C>(string name)
            where B : IDataStore
            where C : IDataStore
        {
            var existing = Values.FirstOrDefault(x => x.Name == name && x.BaseType == typeof(B));
            if (existing is null)
            {
                Values.Add(new DataStoreValue(name, typeof(B), typeof(C)));
                return;
            }

            if (existing.ConcreteType == typeof(C))
            {
                return;
            }

            throw new UnsupportedDataStoreException(
                $"Data store '{name}' for base type '{typeof(B).GetGenericTypeName()}' is already registered with concrete type " +
                $"'{existing.ConcreteType.GetGenericTypeName()}'; cannot reconfigure as '{typeof(C).GetGenericTypeName()}'.");
        }
    }
}
