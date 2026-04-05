using System.Security.Claims;

namespace RCommon.Security.Claims
{
    /// <summary>
    /// Configurable options for claim type URI mappings.
    /// Set properties to match the claim types issued by your identity provider.
    /// </summary>
    public class ClaimTypesOptions
    {
        public string UserName { get; set; } = ClaimTypes.Name;
        public string Name { get; set; } = ClaimTypes.GivenName;
        public string SurName { get; set; } = ClaimTypes.Surname;
        public string UserId { get; set; } = ClaimTypes.NameIdentifier;
        public string Role { get; set; } = ClaimTypes.Role;
        public string Email { get; set; } = ClaimTypes.Email;
        public string TenantId { get; set; } = "tenantid";
        public string ClientId { get; set; } = "client_id";
    }
}
