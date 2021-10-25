using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.ExceptionHandling
{
    [Serializable]
    public class BusinessException : GeneralException
    {
        public BusinessException():base()
        {
            base.Severity = SeverityOptions.Medium;
        }

        public BusinessException(string keyMessage)
            : base(keyMessage)
        {
            base.Severity = SeverityOptions.Medium;
        }

        public BusinessException(string keyMessage, params object[] messageParameters)
            : base(keyMessage, messageParameters)
        {
            base.Severity = SeverityOptions.Medium;
        }

        public BusinessException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            base.Severity = SeverityOptions.Medium;
        }
    }
}
