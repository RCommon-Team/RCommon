namespace RCommon.Security.Clients
{
    /// <summary>
    /// Represents the currently authenticated client (e.g., an OAuth client application) in a multi-client environment.
    /// </summary>
    public interface ICurrentClient
    {
        /// <summary>
        /// Gets the unique identifier of the current client, derived from the <see cref="Claims.ClaimTypesConst.ClientId"/> claim.
        /// Returns <c>null</c> if no client identity is present.
        /// </summary>
        string? Id { get; }

        /// <summary>
        /// Gets a value indicating whether the current request has an authenticated client identity.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}