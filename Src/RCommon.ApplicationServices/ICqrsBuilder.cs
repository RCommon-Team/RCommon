using Microsoft.Extensions.DependencyInjection;

namespace RCommon.ApplicationServices
{
    /// <summary>
    /// Defines the contract for a builder that configures CQRS (Command Query Responsibility Segregation) services.
    /// </summary>
    /// <remarks>
    /// Implementations register command and query bus infrastructure into the DI container.
    /// Use extension methods on this interface (e.g., <c>AddCommandHandler</c>, <c>AddQueryHandler</c>) to register handlers.
    /// </remarks>
    public interface ICqrsBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> used to register CQRS-related services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}