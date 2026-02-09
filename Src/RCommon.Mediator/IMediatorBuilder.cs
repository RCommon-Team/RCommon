using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Mediator
{
    /// <summary>
    /// Defines the contract for configuring a mediator implementation within the RCommon framework.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface are responsible for registering mediator-specific services
    /// (e.g., MediatR or Wolverine) into the dependency injection container. Used in conjunction
    /// with <see cref="MediatorBuilderExtensions.WithMediator{T}(IRCommonBuilder, Action{T})"/>.
    /// </remarks>
    public interface IMediatorBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register mediator-related services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
