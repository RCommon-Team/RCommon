using MassTransit;
using MassTransit.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using RCommon.MassTransit.Subscribers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.MassTransit
{
    /// <summary>
    /// Default implementation of <see cref="IMassTransitEventHandlingBuilder"/> that configures MassTransit
    /// consumers and event handling services through the RCommon builder pipeline.
    /// Inherits from <see cref="ServiceCollectionBusConfigurator"/> to provide full MassTransit bus registration capabilities.
    /// </summary>
    public class MassTransitEventHandlingBuilder : ServiceCollectionBusConfigurator, IMassTransitEventHandlingBuilder
    {

        /// <summary>
        /// Initializes a new instance of <see cref="MassTransitEventHandlingBuilder"/> using the provided RCommon builder.
        /// </summary>
        /// <param name="builder">The <see cref="IRCommonBuilder"/> whose service collection is used for dependency registration.</param>
        public MassTransitEventHandlingBuilder(IRCommonBuilder builder)
            :base(builder.Services)
        {
            Services = builder.Services;

        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
