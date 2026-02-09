using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    /// <summary>
    /// Default implementation of <see cref="IInMemoryEventBusBuilder"/> that configures
    /// in-memory event handling by exposing the <see cref="IServiceCollection"/> from the parent
    /// <see cref="IRCommonBuilder"/>.
    /// </summary>
    public class InMemoryEventBusBuilder : IInMemoryEventBusBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryEventBusBuilder"/> using the parent builder's service collection.
        /// </summary>
        /// <param name="builder">The parent <see cref="IRCommonBuilder"/> whose <see cref="IRCommonBuilder.Services"/> will be used.</param>
        public InMemoryEventBusBuilder(IRCommonBuilder builder)
        {
            Services = builder.Services;

        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}

