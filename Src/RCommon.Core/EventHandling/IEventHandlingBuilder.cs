using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.EventHandling
{
    /// <summary>
    /// Defines the base builder interface for configuring event handling infrastructure.
    /// Provides access to the <see cref="IServiceCollection"/> for service registration.
    /// </summary>
    public interface IEventHandlingBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register event handling services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
