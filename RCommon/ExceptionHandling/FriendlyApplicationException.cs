using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ExceptionHandling
{
    public class FriendlyApplicationException : GeneralException
    {
        public FriendlyApplicationException()
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlyApplicationException(string keyMessage)
            : base(keyMessage)
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlyApplicationException(string keyMessage, params object[] messageParameters)
            : base(keyMessage, messageParameters)
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlyApplicationException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            base.Severity = SeverityOptions.Low;
        }
    }
}
