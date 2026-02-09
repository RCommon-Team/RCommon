using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    /// <summary>
    /// Provides an abstraction for JSON serialization and deserialization operations.
    /// Implementations wrap a specific JSON library (e.g., Newtonsoft.Json or System.Text.Json).
    /// </summary>
    /// <seealso cref="JsonSerializeOptions"/>
    /// <seealso cref="JsonDeserializeOptions"/>
    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes the specified object to a JSON string.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">Optional serialization options such as camel casing and indentation.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public string Serialize(object obj, JsonSerializeOptions? options = null);

        /// <summary>
        /// Serializes the specified object to a JSON string using the provided type information.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="type">The <see cref="Type"/> to use during serialization, which may differ from the runtime type.</param>
        /// <param name="options">Optional serialization options such as camel casing and indentation.</param>
        /// <returns>A JSON string representation of the object.</returns>
        public string Serialize(object obj, Type type, JsonSerializeOptions? options = null);

        /// <summary>
        /// Deserializes a JSON string to an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The target type to deserialize to.</typeparam>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="options">Optional deserialization options such as camel casing.</param>
        /// <returns>The deserialized object of type <typeparamref name="T"/>, or <c>null</c> if the JSON represents a null value.</returns>
        public T? Deserialize<T>(string json, JsonDeserializeOptions? options = null);

        /// <summary>
        /// Deserializes a JSON string to an object of the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="type">The <see cref="Type"/> to deserialize the JSON into.</param>
        /// <param name="options">Optional deserialization options such as camel casing.</param>
        /// <returns>The deserialized object, or <c>null</c> if the JSON represents a null value.</returns>
        public object? Deserialize(string json, Type type, JsonDeserializeOptions? options = null);
    }
}
