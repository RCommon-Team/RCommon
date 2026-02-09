using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.SystemTextJson
{
    /// <summary>
    /// Builder interface for configuring JSON serialization using the System.Text.Json library.
    /// </summary>
    /// <seealso cref="IJsonBuilder"/>
    /// <seealso cref="TextJsonBuilder"/>
    public interface ITextJsonBuilder : IJsonBuilder
    {
    }
}
