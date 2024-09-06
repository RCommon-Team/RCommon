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
    public static class ITextJsonBuilderExtensions
    {
        public static ITextJsonBuilder Configure(this ITextJsonBuilder builder, Action<JsonSerializerOptions> options)
        {
            builder.Services.Configure<JsonSerializerOptions>(options);
            return builder;
        }
    }
}
