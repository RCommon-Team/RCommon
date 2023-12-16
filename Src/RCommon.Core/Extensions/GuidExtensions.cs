using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RCommon
{
    public static class GuidExtension
    {

        [DebuggerStepThrough]
        public static bool IsEmpty(this Guid target)
        {
            return target == Guid.Empty;
        }
    }
}
