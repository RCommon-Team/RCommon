using Microsoft.Extensions.DependencyInjection;
using RCommon.ApplicationServices.Commands;
using RCommon.ApplicationServices.Queries;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Default implementation of <see cref="ICqrsBuilder"/> that registers the core CQRS services
    /// (<see cref="ICommandBus"/> and <see cref="IQueryBus"/>) into the dependency injection container.
    /// </summary>
    public class CqrsBuilder : ICqrsBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CqrsBuilder"/> and registers the default CQRS services.
        /// </summary>
        /// <param name="builder">The RCommon builder providing access to the service collection.</param>
        public CqrsBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
            this.RegisterServices(Services);
        }

        /// <summary>
        /// Registers the default <see cref="CommandBus"/> and <see cref="QueryBus"/> as transient services.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        protected void RegisterServices(IServiceCollection services)
        {
            services.AddTransient<ICommandBus, CommandBus>();
            services.AddTransient<IQueryBus, QueryBus>();

        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
