using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models
{
    /// <summary>
    /// Specifies the direction in which a collection should be sorted.
    /// </summary>
    [DataContract]
    public enum  SortDirectionEnum : byte
    {
        /// <summary>
        /// Sort in ascending order (A-Z, 0-9).
        /// </summary>
        Ascending = 1,

        /// <summary>
        /// Sort in descending order (Z-A, 9-0).
        /// </summary>
        Descending = 2,

        /// <summary>
        /// No sorting is applied; the default order is preserved.
        /// </summary>
        None = 3,

    }
}
