using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    public class DataStoreValue
    {
        public DataStoreValue(string name, Type baseType, Type concreteType)
        {
            Name = name;
            BaseType = baseType;
            ConcreteType = concreteType;

            if (concreteType.BaseType != baseType)
            {
                throw new UnsupportedDataStoreException($"Concrete type must implement base type.");
            }
        }

        public string Name { get; }
        public Type BaseType { get; }
        public Type ConcreteType { get; }
    }
}
