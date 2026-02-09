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
    /// <summary>
    /// Implements <see cref="IJsonSerializer"/> using the System.Text.Json library.
    /// Supports per-call overrides for camel-case naming and indented formatting through
    /// <see cref="JsonSerializeOptions"/> and <see cref="JsonDeserializeOptions"/>.
    /// </summary>
    /// <remarks>
    /// The underlying <see cref="JsonSerializerOptions"/> are injected via the options pattern
    /// and may be mutated per-call when options are provided. This means per-call options
    /// modify the shared options instance.
    /// </remarks>
    public class TextJsonSerializer : IJsonSerializer
    {
        private JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="TextJsonSerializer"/> with the configured
        /// <see cref="JsonSerializerOptions"/>.
        /// </summary>
        /// <param name="options">The injected System.Text.Json serializer options.</param>
        public TextJsonSerializer(IOptions<JsonSerializerOptions> options)
        {
            _jsonOptions = options.Value;
        }

        /// <inheritdoc/>
        public T? Deserialize<T>(string json, JsonDeserializeOptions? options = null)
        {
            if (options != null)
            {
                // Apply camelCase naming policy when requested
                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        /// <inheritdoc/>
        public object? Deserialize(string json, Type type, JsonDeserializeOptions? options = null)
        {
            if (options != null)
            {
                // Apply camelCase naming policy when requested
                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }

            return JsonSerializer.Deserialize(json, type, _jsonOptions);
        }

        /// <inheritdoc/>
        public string Serialize(object obj, JsonSerializeOptions? options = null)
        {
            if (options != null)
            {
                _jsonOptions.WriteIndented = options.Indented;

                // Apply camelCase naming policy when requested
                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }

            return JsonSerializer.Serialize(obj, _jsonOptions);
        }

        /// <inheritdoc/>
        public string Serialize(object obj, Type type, JsonSerializeOptions? options = null)
        {
            if (options != null)
            {
                _jsonOptions.WriteIndented = options.Indented;

                // Apply camelCase naming policy when requested
                if (options.CamelCase)
                {
                    _jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                }
            }

            return JsonSerializer.Serialize(obj, type, _jsonOptions);
        }
    }
}
