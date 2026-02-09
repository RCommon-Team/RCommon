using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RCommon
{
    /// <summary>
    /// Provides extension methods for <see cref="Guid"/> operations.
    /// </summary>
    public static class GuidExtension
    {

        /// <summary>
        /// Determines whether the <see cref="Guid"/> is equal to <see cref="Guid.Empty"/>.
        /// </summary>
        /// <param name="target">The <see cref="Guid"/> to check.</param>
        /// <returns><c>true</c> if the GUID is empty; otherwise, <c>false</c>.</returns>
        [DebuggerStepThrough]
        public static bool IsEmpty(this Guid target)
        {
            return target == Guid.Empty;
        }
    }
}
