using Microsoft.Extensions.DependencyInjection;
using RCommon.EventHandling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Wolverine
{
    /// <summary>
    /// Default implementation of <see cref="IWolverineEventHandlingBuilder"/> that configures
    /// Wolverine event handling services through the RCommon builder pipeline.
    /// </summary>
    public class WolverineEventHandlingBuilder : IWolverineEventHandlingBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="WolverineEventHandlingBuilder"/> using the provided RCommon builder.
        /// </summary>
        /// <param name="builder">The <see cref="IRCommonBuilder"/> whose service collection is used for dependency registration.</param>
        public WolverineEventHandlingBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}
