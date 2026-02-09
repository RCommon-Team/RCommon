using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace RCommon
{
    /// <summary>
    /// Defines severity levels for exceptions, used to classify the impact of an error.
    /// </summary>
    public enum SeverityOptions : int
    {
        /// <summary>Low severity; typically informational or easily recoverable.</summary>
        Low = 1,
        /// <summary>Medium severity; may require attention but is not critical.</summary>
        Medium = 2,
        /// <summary>High severity; default level indicating a significant error.</summary>
        High = 3,
        /// <summary>Critical severity; indicates a fatal or system-level failure.</summary>
        Critical = 4
    }

    /// <summary>
    /// A general-purpose exception that supports severity classification and parameterized message formatting.
    /// Extends <see cref="BaseApplicationException"/> to include environment diagnostic information.
    /// </summary>
    [Serializable]
    public class GeneralException : BaseApplicationException
    {
        private SeverityOptions _severity = SeverityOptions.High;
        private string _debugMessage = string.Empty;
        private object[]? _messageParameters = null;

        /// <summary>
        /// Initializes a new instance of <see cref="GeneralException"/> with default values.
        /// </summary>
        public GeneralException():base()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GeneralException"/> with the specified message.
        /// </summary>
        /// <param name="keyMessage">The exception message or format string key.</param>
        public GeneralException(string keyMessage)
            : base(keyMessage)
        {
            _debugMessage = keyMessage;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GeneralException"/> with the specified severity and message.
        /// </summary>
        /// <param name="severity">The <see cref="SeverityOptions"/> level of the exception.</param>
        /// <param name="keyMessage">The exception message or format string key.</param>
        public GeneralException(SeverityOptions severity, string keyMessage)
            : base(keyMessage)
        {
            _severity = severity;
            _debugMessage = keyMessage;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GeneralException"/> with a parameterized message.
        /// </summary>
        /// <param name="keyMessage">The message format string.</param>
        /// <param name="messageParameters">Parameters to format into the message string.</param>
        public GeneralException(string keyMessage, params object[] messageParameters)
            : base(keyMessage)
        {
            _debugMessage = keyMessage;
            _messageParameters = messageParameters;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GeneralException"/> with severity and a parameterized message.
        /// </summary>
        /// <param name="severity">The <see cref="SeverityOptions"/> level of the exception.</param>
        /// <param name="keyMessage">The message format string.</param>
        /// <param name="messageParameters">Parameters to format into the message string.</param>
        public GeneralException(SeverityOptions severity, string keyMessage, params object[] messageParameters)
            : base(keyMessage)
        {
            _severity = severity;
            _debugMessage = keyMessage;
            _messageParameters = messageParameters;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GeneralException"/> with a message and inner exception.
        /// </summary>
        /// <param name="keyMessage">The exception message or format string key.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        public GeneralException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            _debugMessage = keyMessage;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="GeneralException"/> with a parameterized message and inner exception.
        /// </summary>
        /// <param name="keyMessage">The message format string.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        /// <param name="messageParameters">Parameters to format into the message string.</param>
        public GeneralException(string keyMessage, System.Exception innerException, params object[] messageParameters)
            : base(keyMessage, innerException)
        {
            _debugMessage = keyMessage;
            _messageParameters = messageParameters;
        }

        /// <summary>
        /// Gets the formatted exception message, applying any message parameters if provided.
        /// </summary>
        public override string Message
        {
            get
            {
                return FormatMessage();
            }
        }

        /// <summary>
        /// Gets the debug-friendly message with parameters applied via <see cref="string.Format(string, object[])"/>.
        /// If no parameters were provided, returns the base message unmodified.
        /// </summary>
        public string DebugMessage
        {
            get
            {
                return (_messageParameters == null) ? base.Message : string.Format(base.Message, _messageParameters);
            }
        }

        /// <summary>
        /// Gets or sets the severity level of this exception.
        /// </summary>
        /// <seealso cref="SeverityOptions"/>
        public SeverityOptions Severity
        {
            get
            {
                return _severity;
            }
            set
            {
                _severity = value;
            }
        }

        /// <summary>
        /// Formats the exception message by delegating to <see cref="DebugMessage"/>.
        /// </summary>
        /// <returns>The formatted message string.</returns>
        private string FormatMessage()
        {
            return DebugMessage;
        }
    }
}
