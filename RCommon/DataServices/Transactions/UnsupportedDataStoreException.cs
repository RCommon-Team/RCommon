using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.DataServices.Transactions
{
    public class UnsupportedDataStoreException : ApplicationException
    {

        public UnsupportedDataStoreException(Type type) : base("Type of: " + type.FullName + " implements IDataStore but is not registered with the "
            + "Dependency Injection container.")
        {

        }
    }
}
