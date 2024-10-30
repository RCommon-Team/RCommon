using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    public class JsonDeserializeOptions
    {
        public JsonDeserializeOptions()
        {
            this.CamelCase = true;   
        }

        public bool CamelCase { get; set; }
    }
}
