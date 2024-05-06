using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Validation
{
    /// <summary>
    /// Specifies the severity of a rule.
    /// </summary>
    public enum Severity
    {
        /// <summary>
        /// Error
        /// </summary>
        Error,
        /// <summary>
        /// Warning
        /// </summary>
        Warning,
        /// <summary>
        /// Info
        /// </summary>
        Info
    }
}
