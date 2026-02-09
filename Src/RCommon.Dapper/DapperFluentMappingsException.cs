using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence.Dapper
{
    /// <summary>
    /// Exception thrown when Dapper fluent mapping configuration encounters an error.
    /// </summary>
    /// <remarks>
    /// This exception is raised with <see cref="SeverityOptions.Critical"/> severity,
    /// indicating a fatal configuration problem that prevents Dapper from operating correctly.
    /// </remarks>
    public class DapperFluentMappingsException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="DapperFluentMappingsException"/> with the specified error message.
        /// </summary>
        /// <param name="message">A message describing the fluent mapping error.</param>
        public DapperFluentMappingsException(string message)
            :base(SeverityOptions.Critical, message)
        {

        }
    }
}
