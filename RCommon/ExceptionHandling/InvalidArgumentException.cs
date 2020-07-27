using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.ExceptionHandling
{
    /// <summary>
    /// Thrown if invalid argument supplied. 
    /// This is a low severity exception that's  meant to be handled 
    /// by the client determining related argument.
    /// </summary>
    [Serializable]
    public class InvalidArgumentException : GeneralException
    {
        public InvalidArgumentException()
        {
            base.Severity = SeverityOptions.Low;
        }

        public InvalidArgumentException(string keyMessage)
            : base(keyMessage)
        {
            base.Severity = SeverityOptions.Low;
        }

        public InvalidArgumentException(string keyMessage, params object[] messageParameters)
            : base(keyMessage, messageParameters)
        {
            base.Severity = SeverityOptions.Low;
        }

        public InvalidArgumentException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            base.Severity = SeverityOptions.Low;
        }
    }
}
