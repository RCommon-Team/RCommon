using Microsoft.Extensions.DependencyInjection;

namespace RCommon.Mediator.MediatR
{
    /// <summary>
    /// Builder interface for configuring the MediatR mediator implementation within RCommon.
    /// Extends <see cref="IMediatorBuilder"/> with MediatR-specific configuration methods.
    /// </summary>
    public interface IMediatRBuilder : IMediatorBuilder
    {
        /// <summary>
        /// Configures MediatR services using a configuration action delegate.
        /// </summary>
        /// <param name="options">An action to configure <see cref="MediatRServiceConfiguration"/>.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        IMediatRBuilder Configure(Action<MediatRServiceConfiguration> options);

        /// <summary>
        /// Configures MediatR services using a pre-built configuration instance.
        /// </summary>
        /// <param name="options">The <see cref="MediatRServiceConfiguration"/> to apply.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        IMediatRBuilder Configure(MediatRServiceConfiguration options);
    }
}
