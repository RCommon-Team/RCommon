using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Provides configurable constants for standard claim type URIs used throughout the security subsystem.
    /// Each property defaults to the corresponding <see cref="ClaimTypes"/> value and can be overridden at startup.
    /// </summary>
    public static class ClaimTypesConst
    {
        /// <summary>
        /// Default: <see cref="ClaimTypes.Name"/>
        /// </summary>
        public static string UserName { get; set; } = ClaimTypes.Name;

        /// <summary>
        /// Default: <see cref="ClaimTypes.GivenName"/>
        /// </summary>
        public static string Name { get; set; } = ClaimTypes.GivenName;

        /// <summary>
        /// Default: <see cref="ClaimTypes.Surname"/>
        /// </summary>
        public static string SurName { get; set; } = ClaimTypes.Surname;

        /// <summary>
        /// Default: <see cref="ClaimTypes.NameIdentifier"/>
        /// </summary>
        public static string UserId { get; set; } = ClaimTypes.NameIdentifier;

        /// <summary>
        /// Default: <see cref="ClaimTypes.Role"/>
        /// </summary>
        public static string Role { get; set; } = ClaimTypes.Role;

        /// <summary>
        /// Default: <see cref="ClaimTypes.Email"/>
        /// </summary>
        public static string Email { get; set; } = ClaimTypes.Email;

        /// <summary>
        /// Default: "tenantid".
        /// </summary>
        public static string TenantId { get; set; } = "tenantid";

        /// <summary>
        /// Default: "client_id".
        /// </summary>
        public static string ClientId { get; set; } = "client_id";
    }
}
