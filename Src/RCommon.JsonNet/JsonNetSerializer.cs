using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.JsonNet
{
    public class JsonNetSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonNetSerializer(IOptions<JsonSerializerSettings> options)
        {
            _settings = options.Value;
        }

        public string Serialize(object obj, Jsonser)
        {
            return JsonConvert.SerializeObject(obj, _settings); ;
        }
    }
}
