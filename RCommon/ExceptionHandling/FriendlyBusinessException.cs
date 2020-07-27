using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace RCommon.ExceptionHandling
{
	[Serializable]
	public class FriendlyBusinessException : GeneralException
    {
        public FriendlyBusinessException()
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlyBusinessException(string keyMessage)
            : base(keyMessage)
        {
            base.Severity = SeverityOptions.Low;
        }

        public FriendlyBusinessException(string keyMessage, params object[] messageParameters)
            : base(keyMessage, messageParameters)
        {
            base.Severity = SeverityOptions.Low;
        }

		public FriendlyBusinessException(string keyMessage, System.Exception innerException)
            : base(keyMessage, innerException)
        {
            base.Severity = SeverityOptions.Low;
        }
    }
}
