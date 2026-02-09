using MediatR;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling.Subscribers;
using RCommon.Mediator;
using RCommon.MediatR;
using RCommon.MediatR.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator.MediatR
{
    /// <summary>
    /// Default implementation of <see cref="IMediatRBuilder"/> that registers MediatR services
    /// and the <see cref="MediatRAdapter"/> as the <see cref="IMediatorAdapter"/> within the DI container.
    /// </summary>
    public class MediatRBuilder : IMediatRBuilder
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MediatRBuilder"/> and registers core MediatR services.
        /// </summary>
        /// <param name="builder">The <see cref="IRCommonBuilder"/> whose service collection is used for dependency registration.</param>
        public MediatRBuilder(IRCommonBuilder builder)
        {


            this.RegisterServices(builder.Services);
            Services = builder.Services;

        }

        /// <summary>
        /// Registers the <see cref="MediatRAdapter"/> and MediatR services from this assembly.
        /// </summary>
        /// <param name="services">The service collection to register services into.</param>
        protected void RegisterServices(IServiceCollection services)
        {
            services.AddScoped<IMediatorAdapter, MediatRAdapter>();

            services.AddMediatR(options =>
            {
                options.RegisterServicesFromAssemblies((typeof(MediatRBuilder).GetTypeInfo().Assembly));
            });
        }

        /// <inheritdoc />
        public IMediatRBuilder Configure(Action<MediatRServiceConfiguration> options)
        {
            Services.AddMediatR(options);
            return this;
        }

        /// <inheritdoc />
        public IMediatRBuilder Configure(MediatRServiceConfiguration options)
        {
            Services.AddMediatR(options);
            return this;
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
