using System;
using System.Security.Claims;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Provides claim type URI constants used throughout the security subsystem.
    /// Call <see cref="Configure"/> once at startup to override defaults.
    /// After configuration (or first property access), values are frozen.
    /// </summary>
    public static class ClaimTypesConst
    {
        private static ClaimTypesOptions? _options;
        private static bool _frozen;
        private static readonly object _lock = new();

        public static string UserName => GetOptions().UserName;
        public static string Name => GetOptions().Name;
        public static string SurName => GetOptions().SurName;
        public static string UserId => GetOptions().UserId;
        public static string Role => GetOptions().Role;
        public static string Email => GetOptions().Email;
        public static string TenantId => GetOptions().TenantId;
        public static string ClientId => GetOptions().ClientId;

        /// <summary>
        /// Configures claim type mappings. May only be called once, before any property is accessed.
        /// </summary>
        public static void Configure(Action<ClaimTypesOptions> configure)
        {
            Guard.IsNotNull(configure, nameof(configure));

            lock (_lock)
            {
                if (_frozen)
                {
                    throw new InvalidOperationException(
                        "ClaimTypesConst has already been configured or accessed. Configure may only be called once, before any property is read.");
                }

                var options = new ClaimTypesOptions();
                configure(options);
                _options = options;
                _frozen = true;
            }
        }

        private static ClaimTypesOptions GetOptions()
        {
            if (_options != null)
                return _options;

            lock (_lock)
            {
                if (_options != null)
                    return _options;

                _options = new ClaimTypesOptions();
                _frozen = true;
                return _options;
            }
        }

        /// <summary>
        /// Resets configuration to allow reconfiguration. Internal — for test isolation only.
        /// </summary>
        internal static void Reset()
        {
            lock (_lock)
            {
                _options = null;
                _frozen = false;
            }
        }
    }
}
