using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Validation
{
    /// <summary>
    /// Defines the contract for a validation service that orchestrates object validation.
    /// </summary>
    /// <remarks>
    /// The validation service acts as a facade over <see cref="IValidationProvider"/>, creating a DI scope
    /// and delegating to the registered provider. It is used by <see cref="Commands.CommandBus"/>
    /// and <see cref="Queries.QueryBus"/> when CQRS validation is enabled.
    /// </remarks>
    public interface IValidationService
    {
        /// <summary>
        /// Validates the specified target object asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="target">The object to validate.</param>
        /// <param name="throwOnFaults">If <c>true</c>, a <see cref="ValidationException"/> is thrown when validation fails. Defaults to <c>false</c>.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="ValidationOutcome"/> containing any validation faults.</returns>
        Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults = false, CancellationToken cancellationToken = default) where T : class;
    }
}