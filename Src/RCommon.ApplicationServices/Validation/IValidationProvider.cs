using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.ApplicationServices.Validation
{
    /// <summary>
    /// Defines the contract for a validation provider that performs validation against a target object.
    /// </summary>
    /// <remarks>
    /// Implementations bridge to a specific validation library (e.g., FluentValidation).
    /// The <see cref="ValidationService"/> resolves an <see cref="IValidationProvider"/> from the DI container
    /// to perform the actual validation logic.
    /// </remarks>
    public interface IValidationProvider
    {
        /// <summary>
        /// Validates the specified target object asynchronously.
        /// </summary>
        /// <typeparam name="T">The type of the object to validate.</typeparam>
        /// <param name="target">The object to validate.</param>
        /// <param name="throwOnFaults">If <c>true</c>, a <see cref="ValidationException"/> is thrown when validation fails.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A <see cref="ValidationOutcome"/> containing any validation faults.</returns>
        Task<ValidationOutcome> ValidateAsync<T>(T target, bool throwOnFaults, CancellationToken cancellationToken = default)
        where T : class;

    }
}
