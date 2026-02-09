using Microsoft.Extensions.DependencyInjection;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RCommon.SystemTextJson
{
    /// <summary>
    /// Provides extension methods for <see cref="ITextJsonBuilder"/> to configure System.Text.Json settings.
    /// </summary>
    public static class ITextJsonBuilderExtensions
    {
        /// <summary>
        /// Configures the underlying <see cref="JsonSerializerOptions"/> used by the System.Text.Json serializer.
        /// </summary>
        /// <param name="builder">The System.Text.Json builder instance.</param>
        /// <param name="options">An action to configure <see cref="JsonSerializerOptions"/>.</param>
        /// <returns>The <see cref="ITextJsonBuilder"/> for further chaining.</returns>
        public static ITextJsonBuilder Configure(this ITextJsonBuilder builder, Action<JsonSerializerOptions> options)
        {
            builder.Services.Configure<JsonSerializerOptions>(options);
            return builder;
        }
    }
}
