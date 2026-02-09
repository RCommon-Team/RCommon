using Microsoft.Extensions.DependencyInjection;
using RCommon.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.SystemTextJson
{
    /// <summary>
    /// Default implementation of <see cref="ITextJsonBuilder"/> that registers
    /// the System.Text.Json-based <see cref="TextJsonSerializer"/> into the DI container.
    /// </summary>
    /// <seealso cref="ITextJsonBuilder"/>
    /// <seealso cref="TextJsonSerializer"/>
    public class TextJsonBuilder : ITextJsonBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="TextJsonBuilder"/> and registers JSON serialization services.
        /// </summary>
        /// <param name="builder">The RCommon builder providing access to the <see cref="IServiceCollection"/>.</param>
        public TextJsonBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        /// <summary>
        /// Registers the <see cref="TextJsonSerializer"/> as the <see cref="IJsonSerializer"/> implementation
        /// with a transient lifetime.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        protected void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<IJsonSerializer, TextJsonSerializer>();
        }

        /// <inheritdoc/>
        public IServiceCollection Services { get; }
    }
}
