using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.JsonNet
{
    /// <summary>
    /// Builder interface for configuring JSON serialization using the Newtonsoft.Json (Json.NET) library.
    /// </summary>
    /// <seealso cref="IJsonBuilder"/>
    /// <seealso cref="JsonNetBuilder"/>
    public interface IJsonNetBuilder : IJsonBuilder
    {
    }
}
