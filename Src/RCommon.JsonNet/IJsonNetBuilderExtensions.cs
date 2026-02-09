using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RCommon.JsonNet
{
    /// <summary>
    /// Provides extension methods for <see cref="IJsonNetBuilder"/> to configure Newtonsoft.Json settings.
    /// </summary>
    public static class IJsonNetBuilderExtensions
    {
        /// <summary>
        /// Configures the underlying <see cref="JsonSerializerSettings"/> used by the Newtonsoft.Json serializer.
        /// </summary>
        /// <param name="builder">The Json.NET builder instance.</param>
        /// <param name="options">An action to configure <see cref="JsonSerializerSettings"/>.</param>
        /// <returns>The <see cref="IJsonNetBuilder"/> for further chaining.</returns>
        public static IJsonNetBuilder Configure(this IJsonNetBuilder builder, Action<JsonSerializerSettings> options)
        {
            builder.Services.Configure<JsonSerializerSettings>(options);
            return builder;
        }
    }
}
