using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    public interface IJsonSerializer
    {
        public string Serialize(object obj, JsonSerializeOptions? options = null);

        public T Deserialize<T>(string json, JsonDeserializeOptions? options = null);

        public object Deserialize(string json, Type type, JsonDeserializeOptions? options = null);
    }
}
