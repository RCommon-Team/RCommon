using Microsoft.Extensions.Options;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RCommon.SystemTextJson
{
    public class TextJsonSerializer
    {
        private JsonSerializerOptions _jsonOptions;

        public TextJsonSerializer(IOptions<JsonSerializerOptions> options)
        {
            _jsonOptions = options.Value;
        }

        public string Serialize(object obj, JsonSerializeOptions options)
        { 
            _jsonOptions.WriteIndented = options.Indented;

            if (options.CamelCase)
            {
                _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            }
            return JsonSerializer.Serialize(obj, _jsonOptions);
        }
    }
}
