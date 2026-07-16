using System;
using System.Threading;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Ambient, scoped bypass for tenant-based repository filtering and stamping. While a
    /// scope returned by <see cref="Bypass"/> is active, any <see cref="ITenantIdAccessor"/>
    /// wrapped with <see cref="TenantScopeAwareTenantIdAccessor"/> resolves to <c>null</c>,
    /// which every repository already treats as "skip tenant filtering / skip stamping" per
    /// <see cref="ITenantIdAccessor.GetTenantId"/>'s existing contract.
    /// </summary>
    public static class TenantScope
    {
        private static readonly AsyncLocal<bool> _bypassed = new();

        /// <summary>
        /// Gets whether a bypass scope is currently active for this logical call context.
        /// </summary>
        public static bool IsBypassed => _bypassed.Value;

        /// <summary>
        /// Suspends tenant scoping for the returned scope's lifetime, including across async
        /// continuations. Always dispose the returned handle (a <c>using</c> block is
        /// recommended) -- if it is never disposed, the bypass remains active for the rest of
        /// the current logical call context.
        /// </summary>
        /// <returns>An <see cref="IDisposable"/> that restores the previous bypass state when disposed.</returns>
        public static IDisposable Bypass()
        {
            var previous = _bypassed.Value;
            _bypassed.Value = true;
            return new BypassHandle(previous);
        }

        private sealed class BypassHandle : IDisposable
        {
            private readonly bool _previous;
            private bool _disposed;

            public BypassHandle(bool previous) => _previous = previous;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _bypassed.Value = _previous;
            }
        }
    }
}
