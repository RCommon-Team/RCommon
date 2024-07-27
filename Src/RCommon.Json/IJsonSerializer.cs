using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    public interface IJsonSerializer
    {
        public string Serialize(object obj, JsonSerializeOptions options);

        public T Deserialize<T>(string json, JsonDeserializeOptions options);

        public object Deserialize(Type type, string json, JsonDeserializeOptions options);
    }
}
