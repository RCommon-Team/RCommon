using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Sql
{
    /// <summary>
    /// Exception thrown when an <see cref="RDbConnection"/> encounters a configuration or connectivity error.
    /// </summary>
    public class RDbConnectionException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RDbConnectionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">A message describing the connection error.</param>
        public RDbConnectionException(string message)
            : base(message)
        {

        }
    }
}
