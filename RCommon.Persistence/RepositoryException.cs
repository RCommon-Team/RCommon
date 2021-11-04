using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.Persistence
{
    public class RepositoryException : ApplicationException
    {
        public RepositoryException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
