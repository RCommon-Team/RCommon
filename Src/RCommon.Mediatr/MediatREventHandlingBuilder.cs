using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.Mediator;
using RCommon.Mediator.MediatR;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MediatR
{
    /// <summary>
    /// Default implementation of <see cref="IMediatREventHandlingBuilder"/> that registers MediatR event handling
    /// services including the <see cref="MediatRAdapter"/> as the <see cref="IMediatorAdapter"/>.
    /// </summary>
    public class MediatREventHandlingBuilder : IMediatREventHandlingBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MediatREventHandlingBuilder"/> and registers core services.
        /// </summary>
        /// <param name="builder">The <see cref="IRCommonBuilder"/> whose service collection is used for dependency registration.</param>
        public MediatREventHandlingBuilder(IRCommonBuilder builder)
        {
            this.RegisterServices(builder.Services);
            Services = builder.Services;
        }

        /// <summary>
        /// Registers the <see cref="MediatRAdapter"/> as the scoped <see cref="IMediatorAdapter"/> implementation.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        protected void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IMediatorAdapter, MediatRAdapter>();
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
