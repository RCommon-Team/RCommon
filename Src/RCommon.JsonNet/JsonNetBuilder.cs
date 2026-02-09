using Microsoft.Extensions.DependencyInjection;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.JsonNet
{
    /// <summary>
    /// Default implementation of <see cref="IJsonNetBuilder"/> that registers
    /// the Newtonsoft.Json-based <see cref="JsonNetSerializer"/> into the DI container.
    /// </summary>
    /// <seealso cref="IJsonNetBuilder"/>
    /// <seealso cref="JsonNetSerializer"/>
    public class JsonNetBuilder : IJsonNetBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonNetBuilder"/> and registers JSON serialization services.
        /// </summary>
        /// <param name="builder">The RCommon builder providing access to the <see cref="IServiceCollection"/>.</param>
        public JsonNetBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        /// <summary>
        /// Registers the <see cref="JsonNetSerializer"/> as the <see cref="IJsonSerializer"/> implementation
        /// with a transient lifetime.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        protected void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<IJsonSerializer, JsonNetSerializer>();
        }

        /// <inheritdoc/>
        public IServiceCollection Services { get; }
    }
}
