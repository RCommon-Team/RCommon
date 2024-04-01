using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Authorization
{
    /// <summary>
    /// This exception is thrown on an unauthorized request.
    /// </summary>
    [Serializable]
    public class AuthorizationException : ApplicationException
    {
        /// <summary>
        /// Severity of the exception.
        /// Default: Warn.
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Error code.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Creates a new <see cref="AuthorizationException"/> object.
        /// </summary>
        public AuthorizationException()
        {
            LogLevel = LogLevel.Warning;
        }

        /// <summary>
        /// Creates a new <see cref="AuthorizationException"/> object.
        /// </summary>
        /// <param name="message">Exception message</param>
        public AuthorizationException(string message)
            : base(message)
        {
            LogLevel = LogLevel.Warning;
        }

        /// <summary>
        /// Creates a new <see cref="AuthorizationException"/> object.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="innerException">Inner exception</param>
        public AuthorizationException(string message, Exception innerException)
            : base(message, innerException)
        {
            LogLevel = LogLevel.Warning;
        }

        /// <summary>
        /// Creates a new <see cref="AuthorizationException"/> object.
        /// </summary>
        /// <param name="message">Exception message</param>
        /// <param name="code">Exception code</param>
        /// <param name="innerException">Inner exception</param>
        public AuthorizationException(string message = null, string code = null, Exception innerException = null)
            : base(message, innerException)
        {
            Code = code;
            LogLevel = LogLevel.Warning;
        }

        public AuthorizationException WithData(string name, object value)
        {
            Data[name] = value;
            return this;
        }
    }
}
