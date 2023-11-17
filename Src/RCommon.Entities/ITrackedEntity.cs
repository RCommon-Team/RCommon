using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Entities
{
    public interface ITrackedEntity
    {
        bool AllowEventTracking { get; set; }
    }
}
