using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RCommon.Persistence
{
    /// <summary>
    /// Exception thrown when a data store registration or resolution is not supported,
    /// such as registering a duplicate data store name or an invalid type hierarchy.
    /// </summary>
    /// <seealso cref="DataStoreFactoryOptions.Register{B, C}(string)"/>
    /// <seealso cref="DataStoreValue"/>
    public class UnsupportedDataStoreException : GeneralException
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedDataStoreException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the unsupported data store operation.</param>
        public UnsupportedDataStoreException(string message) :base(message)
        {

        }

    }
}
