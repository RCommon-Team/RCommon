using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ExceptionHandling
{
    public class FriendlySecurityException : GeneralException
    {
        public FriendlySecurityException()
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlySecurityException(string keyMessage)
            : base(keyMessage)
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlySecurityException(string keyMessage, params object[] messageParameters)
            : base(keyMessage, messageParameters)
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlySecurityException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            base.Severity = SeverityOptions.Low;
        }
    }
}
