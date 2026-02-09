using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Json
{
    /// <summary>
    /// Defines the contract for configuring JSON serialization within the RCommon framework.
    /// Implementations register a specific JSON serialization library (e.g., Newtonsoft.Json or System.Text.Json)
    /// into the dependency injection container.
    /// </summary>
    /// <seealso cref="IJsonSerializer"/>
    public interface IJsonBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register JSON serialization services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
