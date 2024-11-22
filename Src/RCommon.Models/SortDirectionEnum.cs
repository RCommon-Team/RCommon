using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Models
{
    [DataContract]
    public enum  SortDirectionEnum : byte
    {
        Ascending = 1,
        Descending = 2,
        None = 3,
      
    }
}
