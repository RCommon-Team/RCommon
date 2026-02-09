using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.JsonNet
{
    /// <summary>
    /// Implements <see cref="IJsonSerializer"/> using the Newtonsoft.Json (Json.NET) library.
    /// Supports per-call overrides for camel-case naming and indented formatting through
    /// <see cref="JsonSerializeOptions"/> and <see cref="JsonDeserializeOptions"/>.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="JsonSerializerSettings"/> are injected via the options pattern
    /// and may be mutated per-call when options are provided. This means per-call options
    /// modify the shared settings instance.
    /// </remarks>
    public class JsonNetSerializer : IJsonSerializer
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Initializes a new instance of <see cref="JsonNetSerializer"/> with the configured
        /// <see cref="JsonSerializerSettings"/>.
        /// </summary>
        /// <param name="options">The injected Newtonsoft.Json serializer settings.</param>
        public JsonNetSerializer(IOptions<JsonSerializerSettings> options)
        {
            _settings = options.Value;
        }

        /// <inheritdoc/>
        public T? Deserialize<T>(string json, JsonDeserializeOptions? options = null)
        {
            // Apply camelCase contract resolver when requested
            if (options != null && options.CamelCase)
            {
                _settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        /// <inheritdoc/>
        public object? Deserialize(string json, Type type, JsonDeserializeOptions? options = null)
        {
            // Apply camelCase contract resolver when requested
            if (options != null && options.CamelCase)
            {
                _settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }
            return JsonConvert.DeserializeObject(json, type, _settings);
        }

        /// <inheritdoc/>
        public string Serialize(object obj, JsonSerializeOptions? options = null)
        {
            // Apply camelCase contract resolver when requested
            if (options != null && options.CamelCase)
            {
                _settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }

            // Enable indented formatting when requested
            if (options != null && options.Indented)
            {
                _settings.Formatting = Formatting.Indented;
            }

            return JsonConvert.SerializeObject(obj, _settings);
        }

        /// <inheritdoc/>
        public string Serialize(object obj, Type type, JsonSerializeOptions? options = null)
        {
            // Apply camelCase contract resolver when requested
            if (options != null && options.CamelCase)
            {
                _settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            }

            // Enable indented formatting when requested
            if (options != null && options.Indented)
            {
                _settings.Formatting = Formatting.Indented;
            }

            return JsonConvert.SerializeObject(obj, type, _settings);
        }
    }
}
