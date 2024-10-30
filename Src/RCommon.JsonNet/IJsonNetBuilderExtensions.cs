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
    public static class IJsonNetBuilderExtensions
    {
        public static IJsonNetBuilder Configure(this IJsonNetBuilder builder, Action<JsonSerializerSettings> options)
        {
            builder.Services.Configure<JsonSerializerSettings>(options);
            return builder;
        }
    }
}
