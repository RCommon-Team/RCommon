using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    public class CachingOptions
    {
        public CachingOptions()
        {
            this.CachingEnabled = false;
            this.CacheDynamicallyCompiledExpressions = false;
        }

        public bool CachingEnabled { get; set; }
        public bool CacheDynamicallyCompiledExpressions { get; set; }
    }
}
