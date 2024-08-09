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
    public class TextJsonSerializer : IJsonSerializer
    {
        private JsonSerializerOptions _jsonOptions;

        public TextJsonSerializer(IOptions<JsonSerializerOptions> options)
        {
            _jsonOptions = options.Value;
        }

        public T Deserialize<T>(string json, JsonDeserializeOptions? options = null)
        {
            if (options != null)
            {
                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }
                
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public object Deserialize(string json, Type type, JsonDeserializeOptions? options = null)
        {
            if (options != null)
            {
                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }
            
            return JsonSerializer.Deserialize(json, type, _jsonOptions);
        }

        public string Serialize(object obj, JsonSerializeOptions? options = null)
        {
            if (options != null)
            {
                _jsonOptions.WriteIndented = options.Indented;

                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }
            
            return JsonSerializer.Serialize(obj, _jsonOptions);
        }

        public string Serialize(object obj, Type type, JsonSerializeOptions? options = null)
        {
            if (options != null)
            {
                _jsonOptions.WriteIndented = options.Indented;

                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }

            return JsonSerializer.Serialize(obj, type, _jsonOptions);
        }
    }
}
