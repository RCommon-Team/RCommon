using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Abstract base class for accessing and temporarily replacing the current <see cref="ClaimsPrincipal"/>.
    /// Uses <see cref="AsyncLocal{T}"/> to maintain an override principal that flows across async contexts.
    /// </summary>
    /// <remarks>
    /// Derived classes must implement <see cref="GetClaimsPrincipal"/> to provide the default principal
    /// when no override has been set (e.g., from <see cref="Thread.CurrentPrincipal"/> or an HTTP context).
    /// </remarks>
    public abstract class CurrentPrincipalAccessorBase : ICurrentPrincipalAccessor
    {
        /// <inheritdoc />
        public ClaimsPrincipal? Principal => _currentPrincipal.Value ?? GetClaimsPrincipal();

        /// <summary>
        /// Async-local storage that holds the overridden principal for the current execution context.
        /// </summary>
        private readonly AsyncLocal<ClaimsPrincipal?> _currentPrincipal = new AsyncLocal<ClaimsPrincipal?>();

        /// <summary>
        /// When implemented in a derived class, returns the default <see cref="ClaimsPrincipal"/> for the current context.
        /// </summary>
        /// <returns>The current <see cref="ClaimsPrincipal"/>, or <c>null</c> if none is available.</returns>
        protected abstract ClaimsPrincipal? GetClaimsPrincipal();

        /// <inheritdoc />
        public virtual IDisposable Change(ClaimsPrincipal principal)
        {
            return SetCurrent(principal);
        }

        /// <summary>
        /// Replaces the current principal with <paramref name="principal"/> and returns an <see cref="IDisposable"/>
        /// that restores the previous principal when disposed.
        /// </summary>
        /// <param name="principal">The new principal to set.</param>
        /// <returns>An <see cref="IDisposable"/> that restores the previous principal on disposal.</returns>
        private IDisposable SetCurrent(ClaimsPrincipal principal)
        {
            // Capture the current principal so it can be restored when the scope ends.
            var parent = Principal;
            _currentPrincipal.Value = principal;
            return new DisposeAction(() =>
            {
                _currentPrincipal.Value = parent;
            });
        }

        
    }
}
