using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Persistence
{
    /// <summary>
    /// Exception thrown when a general persistence operation fails.
    /// </summary>
    /// <remarks>
    /// This wraps provider-specific exceptions with a persistence-layer abstraction.
    /// See also <see cref="Crud.RepositoryException"/> for repository-specific errors.
    /// </remarks>
    public class PersistenceException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersistenceException"/> class.
        /// </summary>
        /// <param name="message">A message describing the persistence failure.</param>
        /// <param name="exception">The inner exception that caused this persistence error.</param>
        public PersistenceException(string message, Exception exception) : base(message, exception)
        {

        }
    }
}
