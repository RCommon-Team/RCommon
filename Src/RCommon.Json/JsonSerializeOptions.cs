using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    public class JsonSerializeOptions
    {
        public JsonSerializeOptions()
        {
            this.CamelCase = true;
            this.Indented = false;
        }

        public bool CamelCase { get; set; }
        public bool Indented { get; set; }
    }
}
