using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    /// <summary>
    /// Exception thrown when a named data store cannot be found in the <see cref="IDataStoreFactory"/> registry.
    /// </summary>
    public class DataStoreNotFoundException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStoreNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing which data store could not be found.</param>
        public DataStoreNotFoundException(string message) : base(message)
        {

        }
    }
}
