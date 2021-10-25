using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace RCommon
{
    public enum SeverityOptions : int
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    [Serializable]
    public class GeneralException : BaseApplicationException
    {
        private SeverityOptions _severity = SeverityOptions.High;
        private string _debugMessage = string.Empty;
        private object[] _messageParameters = null;

        public GeneralException():base()
        {
        }

        public GeneralException(string keyMessage)
            : base(keyMessage)
        {
            _debugMessage = keyMessage;
        }

        public GeneralException(SeverityOptions severity, string keyMessage)
            : base(keyMessage)
        {
            _severity = severity;
            _debugMessage = keyMessage;
        }

        public GeneralException(string keyMessage, params object[] messageParameters)
            : base(keyMessage)
        {
            _debugMessage = keyMessage;
            _messageParameters = messageParameters;
        }

        public GeneralException(SeverityOptions severity, string keyMessage, params object[] messageParameters)
            : base(keyMessage)
        {
            _severity = severity;
            _debugMessage = keyMessage;
            _messageParameters = messageParameters;
        }

        public GeneralException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            _debugMessage = keyMessage;
        }

        public GeneralException(string keyMessage, System.Exception innerException, params object[] messageParameters)
            : base(keyMessage, innerException)
        {
            _debugMessage = keyMessage;
            _messageParameters = messageParameters;
        }

        protected GeneralException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _severity = (SeverityOptions)info.GetInt32("severity");
            _debugMessage = info.GetString("message");
        }

        public override string Message
        {
            get
            {
                return FormatMessage();
            }
        }

        public string DebugMessage
        {
            get
            {
                return (_messageParameters == null) ? base.Message : string.Format(base.Message, _messageParameters);
            }
        }

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

        private string FormatMessage()
        {
            return DebugMessage;
        }
    }
}
