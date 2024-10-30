using Microsoft.Extensions.DependencyInjection;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.SystemTextJson
{
    public class TextJsonBuilder : ITextJsonBuilder
    {
        public TextJsonBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        protected void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<IJsonSerializer, TextJsonSerializer>();
        }

        public IServiceCollection Services { get; }
    }
}
