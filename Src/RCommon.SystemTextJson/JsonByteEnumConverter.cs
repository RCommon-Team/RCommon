using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RCommon.SystemTextJson
{
    /// <summary>
    /// A custom <see cref="JsonConverter{T}"/> that serializes enum values as their underlying
    /// <see cref="byte"/> numeric representation and deserializes from string names.
    /// </summary>
    /// <typeparam name="T">The enum type to convert.</typeparam>
    /// <remarks>
    /// During reading, the converter expects the JSON token to be a string containing the enum member name.
    /// During writing, the converter outputs the numeric <see cref="byte"/> value of the enum member.
    /// </remarks>
    /// <seealso cref="JsonIntEnumConverter{T}"/>
    public class JsonByteEnumConverter<T> : JsonConverter<T> where T : Enum
    {
        /// <summary>
        /// Reads a JSON string token and parses it into the corresponding <typeparamref name="T"/> enum value.
        /// </summary>
        /// <param name="reader">The UTF-8 JSON reader.</param>
        /// <param name="typeToConvert">The target enum type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The parsed enum value of type <typeparamref name="T"/>.</returns>
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            // Parse the string token as the enum member name
            T value = (T)(Enum.Parse(typeof(T), reader.GetString()!));
            return value;
        }

        /// <summary>
        /// Writes the enum value as its <see cref="byte"/> numeric representation.
        /// </summary>
        /// <param name="writer">The UTF-8 JSON writer.</param>
        /// <param name="value">The enum value to serialize.</param>
        /// <param name="options">The serializer options.</param>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Re-parse the enum to its base Enum type and convert to byte for numeric output
            Enum test = (Enum)Enum.Parse(typeof(T), value.ToString());
            writer.WriteNumberValue(Convert.ToByte(test));
        }
    }
}
