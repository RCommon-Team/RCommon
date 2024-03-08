using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RCommon.Security.Claims
{
    public class ThreadCurrentPrincipalAccessor : CurrentPrincipalAccessorBase
    {
        protected override ClaimsPrincipal GetClaimsPrincipal()
        {
            return Thread.CurrentPrincipal as ClaimsPrincipal;
        }
    }
}
