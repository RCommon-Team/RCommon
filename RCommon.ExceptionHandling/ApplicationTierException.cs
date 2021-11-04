using System;
using System.Collections.Generic;
using System.Text;

namespace RCommon.ExceptionHandling
{
    public class ApplicationTierException : GeneralException
    {
        public ApplicationTierException() : base()
        {
            base.Severity = SeverityOptions.Medium;
        }

        public ApplicationTierException(string keyMessage)
            : base(keyMessage)
        {
            base.Severity = SeverityOptions.Medium;
        }

        public ApplicationTierException(string keyMessage, params object[] messageParameters)
            : base(keyMessage, messageParameters)
        {
            base.Severity = SeverityOptions.Medium;
        }

        public ApplicationTierException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            base.Severity = SeverityOptions.Medium;
        }
    }
}
