using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Persistence.Crud
{
    /// <summary>
    /// Exception thrown when a repository operation fails.
    /// </summary>
    /// <remarks>
    /// This wraps provider-specific exceptions (e.g., database constraint violations) at the repository layer.
    /// See also <see cref="PersistenceException"/> for general persistence-level errors.
    /// </remarks>
    public class RepositoryException : ApplicationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryException"/> class.
        /// </summary>
        /// <param name="message">A message describing the repository operation failure.</param>
        /// <param name="innerException">The inner exception that caused this error.</param>
        public RepositoryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
