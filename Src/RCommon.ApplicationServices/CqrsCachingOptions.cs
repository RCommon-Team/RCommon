using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    public class CqrsCachingOptions
    {
        public CqrsCachingOptions()
        {
            this.UseCacheForHandlers = false;
        }

        public bool UseCacheForHandlers { get; set; }
    }
}
