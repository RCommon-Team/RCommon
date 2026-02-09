using RCommon.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Clients
{
    /// <summary>
    /// Default implementation of <see cref="ICurrentClient"/> that resolves the client identity
    /// from the current <see cref="ClaimsPrincipal"/> via <see cref="ICurrentPrincipalAccessor"/>.
    /// </summary>
    public class CurrentClient : ICurrentClient
    {
        /// <inheritdoc />
        public virtual string? Id => _principalAccessor.Principal?.FindClientId();

        /// <inheritdoc />
        public virtual bool IsAuthenticated => Id != null;

        private readonly ICurrentPrincipalAccessor _principalAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentClient"/> class.
        /// </summary>
        /// <param name="principalAccessor">The accessor used to retrieve the current claims principal.</param>
        public CurrentClient(ICurrentPrincipalAccessor principalAccessor)
        {
            _principalAccessor = principalAccessor;
        }
    }
}
