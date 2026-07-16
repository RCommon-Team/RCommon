using System;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Decorates an <see cref="ITenantIdAccessor"/> so that <see cref="TenantScope.Bypass"/>
    /// suspends its resolution for the scope's lifetime. RCommon wraps its own
    /// <see cref="ClaimsTenantIdAccessor"/> and Finbuckle's tenant accessor with this
    /// automatically; wrap a custom <see cref="ITenantIdAccessor"/> implementation with this
    /// type at your own registration site to opt into the same bypass support.
    /// </summary>
    public sealed class TenantScopeAwareTenantIdAccessor : ITenantIdAccessor
    {
        private readonly ITenantIdAccessor _inner;

        /// <summary>
        /// Initializes a new instance of <see cref="TenantScopeAwareTenantIdAccessor"/> wrapping
        /// <paramref name="inner"/>.
        /// </summary>
        /// <param name="inner">The accessor to delegate to when no bypass scope is active.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="inner"/> is <c>null</c>.</exception>
        public TenantScopeAwareTenantIdAccessor(ITenantIdAccessor inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <inheritdoc />
        public string? GetTenantId() => TenantScope.IsBypassed ? null : _inner.GetTenantId();
    }
}
