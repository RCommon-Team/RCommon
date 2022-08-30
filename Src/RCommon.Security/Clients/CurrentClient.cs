using RCommon.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon.Security.Clients
{
    public class CurrentClient : ICurrentClient
    {
        public virtual string Id => _principalAccessor.Principal?.FindClientId();

        public virtual bool IsAuthenticated => Id != null;

        private readonly ICurrentPrincipalAccessor _principalAccessor;

        public CurrentClient(ICurrentPrincipalAccessor principalAccessor)
        {
            _principalAccessor = principalAccessor;
        }
    }
}
