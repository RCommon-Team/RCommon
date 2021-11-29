using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RCommon.Serialization.Json
{
    public class JsonIntEnumConverter<T> : JsonConverter<T> where T : Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert,
            JsonSerializerOptions options)
        {
            T value = (T)(Enum.Parse(typeof(T), reader.GetString()));
            return value;
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            Enum test = (Enum)Enum.Parse(typeof(T), value.ToString());
            writer.WriteNumberValue(Convert.ToInt32(test));
        }
    }
}
