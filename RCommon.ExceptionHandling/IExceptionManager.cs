using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RCommon.ExceptionHandling
{
    public interface IExceptionManager
    {
        void HandleException(Exception ex, string policyName);

    }
}
