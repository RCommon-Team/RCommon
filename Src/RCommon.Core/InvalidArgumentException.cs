using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon
{
    /// <summary>
    /// Thrown if invalid argument supplied. 
    /// This is a low severity exception that's  meant to be handled 
    /// by the client determining related argument.
    /// </summary>
    [Serializable]
    public class InvalidArgumentException : GeneralException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InvalidArgumentException"/> with <see cref="SeverityOptions.Low"/> severity.
        /// </summary>
        public InvalidArgumentException()
        {
            base.Severity = SeverityOptions.Low;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="InvalidArgumentException"/> with the specified message.
        /// </summary>
        /// <param name="keyMessage">The exception message or format string key.</param>
        public InvalidArgumentException(string keyMessage)
            : base(keyMessage)
        {
            base.Severity = SeverityOptions.Low;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="InvalidArgumentException"/> with a parameterized message.
        /// </summary>
        /// <param name="keyMessage">The message format string.</param>
        /// <param name="messageParameters">Parameters to format into the message string.</param>
        public InvalidArgumentException(string keyMessage, params object[] messageParameters)
            : base(keyMessage, messageParameters)
        {
            base.Severity = SeverityOptions.Low;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="InvalidArgumentException"/> with a message and inner exception.
        /// </summary>
        /// <param name="keyMessage">The exception message or format string key.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        public InvalidArgumentException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            base.Severity = SeverityOptions.Low;
        }
    }
}
