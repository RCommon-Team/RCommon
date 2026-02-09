using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    /// <summary>
    /// Represents a named data store registration that maps a base type to a concrete type,
    /// used by <see cref="DataStoreFactory"/> to resolve data stores from the DI container.
    /// </summary>
    public class DataStoreValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreValue"/> class.
        /// </summary>
        /// <param name="name">The unique name identifying this data store registration.</param>
        /// <param name="baseType">The base type (e.g., a provider-specific DbContext base class).</param>
        /// <param name="concreteType">The concrete type that directly inherits from <paramref name="baseType"/>.</param>
        /// <exception cref="UnsupportedDataStoreException">
        /// Thrown when <paramref name="concreteType"/> does not directly inherit from <paramref name="baseType"/>.
        /// </exception>
        public DataStoreValue(string name, Type baseType, Type concreteType)
        {
            Name = name;
            BaseType = baseType;
            ConcreteType = concreteType;

            // Validate that the concrete type directly inherits from the base type
            if (concreteType.BaseType != baseType)
            {
                throw new UnsupportedDataStoreException($"Concrete type must implement base type.");
            }
        }

        /// <summary>
        /// Gets the unique name identifying this data store registration.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the base type used for resolving this data store.
        /// </summary>
        public Type BaseType { get; }

        /// <summary>
        /// Gets the concrete type that will be resolved from the DI container.
        /// </summary>
        public Type ConcreteType { get; }
    }
}
