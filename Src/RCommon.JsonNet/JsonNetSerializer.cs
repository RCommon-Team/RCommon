using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.JsonNet
{
    public class JsonNetSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _settings;

        public JsonNetSerializer(IOptions<JsonSerializerSettings> options)
        {
            _settings = options.Value;
        }

        public T Deserialize<T>(string json, JsonDeserializeOptions? options = null)
        {
            if (options != null && options.CamelCase)
            {
                _settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        public object Deserialize(string json, Type type, JsonDeserializeOptions? options = null)
        {
            if (options != null && options.CamelCase)
            {
                _settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
            return JsonConvert.DeserializeObject(json, type, _settings);
        }

        public string Serialize(object obj, JsonSerializeOptions? options = null)
        {
            if (options != null && options.CamelCase)
            {
                _settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }

            if (options != null && options.Indented)
            {
                _settings.Formatting = Formatting.Indented;
            }
            
            return JsonConvert.SerializeObject(obj, _settings);
        }
    }
}
